// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Prowl.Vector;

using Silk.NET.OpenAL;

namespace Prowl.Runtime.Audio.OpenAL;

public class OpenALEngine : AudioEngine, IDisposable
{
    public static ALContext alContext { get; private set; }
    public static AL al { get; private set; }
    public static unsafe Context* context { get; private set; }
    public static unsafe Device* device { get; private set; }

    public OpenALEngine()
    {
        alContext = ALContext.GetApi();
        al = AL.GetApi();
        unsafe
        {
            device = alContext.OpenDevice("");
            if (device == null)
            {
                Console.WriteLine("Could not create device");
                return;
            }

            context = alContext.CreateContext(device, null);
            alContext.MakeContextCurrent(context);

            // Set distance model for 3D audio attenuation
            // LinearDistanceClamped provides linear falloff and silence at MaxDistance
            al.DistanceModel(DistanceModel.LinearDistanceClamped);
        }

        AudioError err = al.GetError();
        if (err != AudioError.NoError)
        {
            Console.WriteLine("OpenAL error: " + err);
        }
    }

    public override void SetListenerPosition(Double3 position)
    {
        // Negate X to convert from left-handed to right-handed coordinate system
        al.SetListenerProperty(ListenerVector3.Position, -(float)position.X, (float)position.Y, (float)position.Z);
    }

    public override Double3 GetListenerPosition()
    {
        al.GetListenerProperty(ListenerVector3.Position, out float x, out float y, out float z);
        // Negate X when converting back from right-handed to left-handed
        return new Double3(-x, y, z);
    }

    public override void SetListenerVelocity(Double3 velocity)
    {
        // Negate X to convert from left-handed to right-handed coordinate system
        al.SetListenerProperty(ListenerVector3.Velocity, -(float)velocity.X, (float)velocity.Y, (float)velocity.Z);
    }

    public override void SetListenerOrientation(Double3 forward, Double3 up)
    {
        unsafe
        {
            float* orientationPtr = stackalloc float[6];

            // Negate X components to convert from left-handed to right-handed coordinate system
            orientationPtr[0] = -(float)forward.X;
            orientationPtr[1] = (float)forward.Y;
            orientationPtr[2] = (float)forward.Z;
            orientationPtr[3] = -(float)up.X;
            orientationPtr[4] = (float)up.Y;
            orientationPtr[5] = (float)up.Z;

            al.SetListenerProperty(ListenerFloatArray.Orientation, orientationPtr);
        }
    }

    public override void SetDopplerFactor(float dopplerFactor)
    {
        al.DopplerFactor(dopplerFactor);
    }

    public override void SetSpeedOfSound(float speedOfSound)
    {
        al.SpeedOfSound(speedOfSound);
    }

    public override AudioBuffer CreateAudioBuffer()
    {
        return new OpenALAudioBuffer();
    }

    public override ActiveAudio CreateAudioSource()
    {
        return new OpenALActiveAudio();
    }

    public void Dispose()
    {
        unsafe
        {
            alContext.DestroyContext(context);
            alContext.CloseDevice(device);
            alContext.Dispose();
        }
    }
}
