// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Prowl.Vector;

using Silk.NET.OpenAL;

namespace Prowl.Runtime.Audio.OpenAL;

public class OpenALActiveAudio : ActiveAudio
{
    public OpenALActiveAudio()
    {
        ID = OpenALEngine.al.GenSource();
        if (ID == 0)
            throw new InvalidOperationException("Too many OpenALAudioSources.");
    }

    public uint ID { get; }

    public override double Gain
    {
        get
        {
            OpenALEngine.al.GetSourceProperty(ID, SourceFloat.Gain, out float gain);
            return gain;
        }
        set
        {
            OpenALEngine.al.SetSourceProperty(ID, SourceFloat.Gain, (float)value);
        }
    }

    public override double Pitch
    {
        get
        {
            OpenALEngine.al.GetSourceProperty(ID, SourceFloat.Pitch, out float pitch);
            return pitch;
        }
        set
        {
            if (value < 0.5 || value > 2.0f)
                throw new ArgumentOutOfRangeException(null, "Pitch must be between 0.5 and 2.0.");

            OpenALEngine.al.SetSourceProperty(ID, SourceFloat.Pitch, (float)value);
        }
    }

    public override double MaxDistance
    {
        get
        {
            OpenALEngine.al.GetSourceProperty(ID, SourceFloat.ReferenceDistance, out float maxDistance);
            return maxDistance;
        }
        set
        {
            OpenALEngine.al.SetSourceProperty(ID, SourceFloat.MaxDistance, (float)value);
        }
    }

    public override bool Looping
    {
        get
        {
            OpenALEngine.al.GetSourceProperty(ID, SourceBoolean.Looping, out bool looping);
            return looping;
        }
        set
        {
            OpenALEngine.al.SetSourceProperty(ID, SourceBoolean.Looping, value);
        }
    }

    public override Double3 Position
    {
        get
        {
            OpenALEngine.al.GetSourceProperty(ID, SourceVector3.Position, out System.Numerics.Vector3 vec3);
            return new Double3(vec3.X, vec3.Y, vec3.Z);
        }
        set
        {
            System.Numerics.Vector3 vec3 = (Float3)value;
            OpenALEngine.al.SetSourceProperty(ID, SourceVector3.Position, in vec3);
        }
    }

    public override Double3 Direction
    {
        get
        {
            OpenALEngine.al.GetSourceProperty(ID, SourceVector3.Direction, out System.Numerics.Vector3 vec3);
            return new Double3(vec3.X, vec3.Y, vec3.Z);
        }
        set
        {
            System.Numerics.Vector3 vec3 = (Float3)value;
            OpenALEngine.al.SetSourceProperty(ID, SourceVector3.Direction, in vec3);
        }
    }

    public override AudioPositionKind PositionKind
    {
        get
        {
            OpenALEngine.al.GetSourceProperty(ID, SourceBoolean.SourceRelative, out bool sourceRelative);
            return sourceRelative ? AudioPositionKind.ListenerRelative : AudioPositionKind.AbsoluteWorld;
        }
        set
        {
            OpenALEngine.al.SetSourceProperty(ID, SourceBoolean.SourceRelative, value == AudioPositionKind.ListenerRelative ? true : false);
        }
    }

    /// <summary>
    /// Gets or sets the playback position, as a value between 0.0f (beginning of clip), and 1.0f (end of clip).
    /// </summary>
    public override double PlaybackPosition
    {
        get
        {
            OpenALEngine.al.GetSourceProperty(ID, GetSourceInteger.ByteOffset, out int playbackBytes);
            OpenALEngine.al.GetSourceProperty(ID, GetSourceInteger.Buffer, out int bufferID);
            OpenALEngine.al.GetBufferProperty((uint)bufferID, GetBufferInteger.Size, out int totalBufferBytes);
            return playbackBytes / totalBufferBytes;
        }
        set
        {
            OpenALEngine.al.GetSourceProperty(ID, GetSourceInteger.Buffer, out int bufferID);
            OpenALEngine.al.GetBufferProperty((uint)bufferID, GetBufferInteger.Size, out int totalBufferBytes);
            int newByteOffset = (int)(totalBufferBytes * value);
            OpenALEngine.al.SetSourceProperty(ID, SourceInteger.ByteOffset, newByteOffset);
        }
    }

    public override bool IsPlaying
    {
        get
        {
            OpenALEngine.al.GetSourceProperty(ID, GetSourceInteger.SourceState, out int state);
            return state == (int)SourceState.Playing;
        }
    }

    public override void Play(AudioBuffer buffer)
    {
        OpenALAudioBuffer alBuffer = (OpenALAudioBuffer)buffer;
        OpenALEngine.al.SetSourceProperty(ID, SourceInteger.Buffer, alBuffer.ID);
        OpenALEngine.al.SourcePlay(ID);
    }

    public override void Stop() => OpenALEngine.al.SourceStop(ID);

    public override void Dispose() => OpenALEngine.al.DeleteSource(ID);
}
