// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Buffers.Binary;
using System.IO;

using Prowl.Runtime.Audio;

namespace Prowl.Runtime;

public sealed class AudioClip : EngineObject
{
    public byte[] Data;
    public BufferAudioFormat Format;
    public int SizeInBytes;
    public int SampleRate;

    public int Channels => GetChannelCount(Format);
    public int BitsPerSample => GetBitsPerSample(Format);
    public double Duration => (double)SampleCount / SampleRate;
    public int SampleCount => Data.Length / Channels;

    public static AudioClip Create(string name, byte[] data, short numChannels, short bitsPerSample, int sampleRate)
    {
        if (bitsPerSample == 24)
        {
            data = Convert24BitTo16Bit(data);
            bitsPerSample = 16; // Update bits per sample to 16
        }

        return new AudioClip
        {
            Name = name,
            Data = data,
            Format = MapFormat(numChannels, bitsPerSample),
            SizeInBytes = data.Length,
            SampleRate = sampleRate
        };
    }

    /// <summary>
    /// Loads an audio clip from a file (.wav)
    /// </summary>
    /// <param name="filePath">Path to the audio file</param>
    /// <param name="enforceMono">If true, stereo audio will be automatically converted to mono by averaging the left and right channels.
    /// This is required for 3D positional audio - OpenAL cannot apply distance attenuation or 3D spatialization to stereo sources.
    /// If false and the file is stereo, it will play at full volume regardless of distance when used with AudioSource.</param>
    public static AudioClip LoadFromFile(string filePath, bool enforceMono = false)
    {
        FileInfo file = new FileInfo(filePath);
        if (!file.Exists)
            throw new FileNotFoundException($"Audio file not found: {filePath}");

        using (FileStream stream = file.OpenRead())
        {
            return LoadFromStream(stream, file.Name, enforceMono);
        }
    }

    /// <summary>
    /// Loads an audio clip from a stream (.wav format)
    /// </summary>
    /// <param name="stream">Stream containing audio data</param>
    /// <param name="name">Name for the audio clip</param>
    /// <param name="enforceMono">If true, stereo audio will be automatically converted to mono by averaging the left and right channels.
    /// This is required for 3D positional audio - OpenAL cannot apply distance attenuation or 3D spatialization to stereo sources.
    /// If false and the stream contains stereo audio, it will play at full volume regardless of distance when used with AudioSource.</param>
    public static AudioClip LoadFromStream(System.IO.Stream stream, string name, bool enforceMono = false)
    {
        var buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        return LoadWav(buffer, name, enforceMono);
    }

    private static AudioClip LoadWav(byte[] buffer, string name, bool enforceMono)
    {
        ReadOnlySpan<byte> fileSpan = new ReadOnlySpan<byte>(buffer);

        int index = 0;
        if (fileSpan[index++] != 'R' || fileSpan[index++] != 'I' || fileSpan[index++] != 'F' || fileSpan[index++] != 'F')
        {
            throw new InvalidDataException("Given file is not in RIFF format");
        }

        var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(fileSpan.Slice(index, 4));
        index += 4;

        if (fileSpan[index++] != 'W' || fileSpan[index++] != 'A' || fileSpan[index++] != 'V' || fileSpan[index++] != 'E')
        {
            throw new InvalidDataException("Given file is not in WAVE format");
        }

        short numChannels = -1;
        int sampleRate = -1;
        int byteRate = -1;
        short blockAlign = -1;
        short bitsPerSample = -1;
        byte[] audioData = null;

        while (index + 4 < fileSpan.Length)
        {
            var identifier = "" + (char)fileSpan[index++] + (char)fileSpan[index++] + (char)fileSpan[index++] + (char)fileSpan[index++];
            var size = BinaryPrimitives.ReadInt32LittleEndian(fileSpan.Slice(index, 4));
            index += 4;

            if (identifier == "fmt ")
            {
                if (size != 16)
                {
                    throw new InvalidDataException($"Unknown Audio Format with subchunk1 size {size}");
                }
                else
                {
                    var audioFormat = BinaryPrimitives.ReadInt16LittleEndian(fileSpan.Slice(index, 2));
                    index += 2;
                    if (audioFormat != 1)
                    {
                        throw new InvalidDataException($"Unknown Audio Format with ID {audioFormat}");
                    }
                    else
                    {
                        numChannels = BinaryPrimitives.ReadInt16LittleEndian(fileSpan.Slice(index, 2));
                        index += 2;
                        sampleRate = BinaryPrimitives.ReadInt32LittleEndian(fileSpan.Slice(index, 4));
                        index += 4;
                        byteRate = BinaryPrimitives.ReadInt32LittleEndian(fileSpan.Slice(index, 4));
                        index += 4;
                        blockAlign = BinaryPrimitives.ReadInt16LittleEndian(fileSpan.Slice(index, 2));
                        index += 2;
                        bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(fileSpan.Slice(index, 2));
                        index += 2;
                    }
                }
            }
            else if (identifier == "data")
            {
                audioData = fileSpan.Slice(index, size).ToArray();
                index += size;
            }
            else
            {
                index += size;
            }
        }

        if (audioData == null)
        {
            throw new InvalidDataException("WAV file does not contain a data chunk");
        }

        // Convert stereo to mono if requested
        if (enforceMono && numChannels == 2)
        {
            audioData = ConvertStereoToMono(audioData, bitsPerSample);
            numChannels = 1;
            Debug.Log($"Converted stereo audio '{name}' to mono for 3D spatialization");
        }

        AudioClip audioClip = AudioClip.Create(name, audioData, numChannels, bitsPerSample, sampleRate);
        return audioClip;
    }

    public static BufferAudioFormat MapFormat(int numChannels, int bitsPerSample) => bitsPerSample switch
    {
        8 => numChannels == 1 ? BufferAudioFormat.Mono8 : BufferAudioFormat.Stereo8,
        16 => numChannels == 1 ? BufferAudioFormat.Mono16 : BufferAudioFormat.Stereo16,
        32 => numChannels == 1 ? BufferAudioFormat.MonoF : BufferAudioFormat.StereoF,
        _ => throw new NotSupportedException("The specified sound format is not supported."),
    };

    private static byte[] ConvertStereoToMono(byte[] stereoData, int bitsPerSample)
    {
        int bytesPerSample = bitsPerSample / 8;
        int stereoSampleCount = stereoData.Length / (bytesPerSample * 2); // 2 channels
        byte[] monoData = new byte[stereoSampleCount * bytesPerSample];

        if (bitsPerSample == 8)
        {
            // 8-bit audio (unsigned)
            for (int i = 0; i < stereoSampleCount; i++)
            {
                int left = stereoData[i * 2];
                int right = stereoData[i * 2 + 1];
                monoData[i] = (byte)((left + right) / 2);
            }
        }
        else if (bitsPerSample == 16)
        {
            // 16-bit audio (signed)
            for (int i = 0; i < stereoSampleCount; i++)
            {
                short left = (short)(stereoData[i * 4] | (stereoData[i * 4 + 1] << 8));
                short right = (short)(stereoData[i * 4 + 2] | (stereoData[i * 4 + 3] << 8));
                short mono = (short)((left + right) / 2);
                monoData[i * 2] = (byte)(mono & 0xFF);
                monoData[i * 2 + 1] = (byte)((mono >> 8) & 0xFF);
            }
        }
        else if (bitsPerSample == 32)
        {
            // 32-bit float audio
            for (int i = 0; i < stereoSampleCount; i++)
            {
                float left = BitConverter.ToSingle(stereoData, i * 8);
                float right = BitConverter.ToSingle(stereoData, i * 8 + 4);
                float mono = (left + right) / 2f;
                byte[] monoBytes = BitConverter.GetBytes(mono);
                Array.Copy(monoBytes, 0, monoData, i * 4, 4);
            }
        }

        return monoData;
    }

    private static byte[] Convert24BitTo16Bit(byte[] data)
    {
        int sampleCount = data.Length / 3;
        byte[] result = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            // Read 24-bit sample
            int sample = (data[i * 3] & 0xFF) |
                         ((data[i * 3 + 1] & 0xFF) << 8) |
                         ((data[i * 3 + 2] & 0xFF) << 16);

            // Handle sign extension if the sample is negative
            if ((sample & 0x800000) != 0)
            {
                sample |= unchecked((int)0xFF000000); // Sign extend
            }

            // Convert to 16-bit sample by shifting right and truncating
            short sample16 = (short)(sample >> 8);

            // Write 16-bit sample
            result[i * 2] = (byte)(sample16 & 0xFF);
            result[i * 2 + 1] = (byte)((sample16 >> 8) & 0xFF);
        }

        return result;
    }

    private static int GetChannelCount(BufferAudioFormat format)
    {
        return format == BufferAudioFormat.Mono8 || format == BufferAudioFormat.Mono16 || format == BufferAudioFormat.MonoF ? 1 : 2;
    }

    private static int GetBitsPerSample(BufferAudioFormat format)
    {
        return format == BufferAudioFormat.Mono8 || format == BufferAudioFormat.Stereo8 ? 8 :
            format == BufferAudioFormat.Mono16 || format == BufferAudioFormat.Stereo16 ? 16 : 32;
    }
}
