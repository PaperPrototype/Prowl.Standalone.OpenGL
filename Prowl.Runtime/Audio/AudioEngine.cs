// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using Prowl.Vector;

namespace Prowl.Runtime.Audio;

public abstract class AudioEngine
{
    // Global audio settings
    private static float _dopplerScale = 1.0f;
    private static float _speedOfSound = 343.5f;
    private static float _distanceScale = 1.0f;

    /// <summary>
    /// Gets or sets the scale of Doppler calculations applied to sounds.
    /// Default is 1.0. Must be >= 0.0.
    /// Higher values more dramatically shift the pitch for the given relative velocity.
    /// </summary>
    public static float DopplerScale
    {
        get => _dopplerScale;
        set
        {
            if (value < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(value), "DopplerScale must be greater than or equal to 0.0");
            _dopplerScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the speed of sound used when calculating the Doppler effect.
    /// Default is 343.5 meters per second. Must be > 0.0.
    /// Has no effect on distance attenuation.
    /// </summary>
    public static float SpeedOfSound
    {
        get => _speedOfSound;
        set
        {
            if (value <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(value), "SpeedOfSound must be greater than 0.0");
            _speedOfSound = value;
        }
    }

    /// <summary>
    /// Gets or sets the scale of distance calculations.
    /// Default is 1.0. Must be > 0.0.
    /// Higher values reduce the rate of falloff between the sound and listener.
    /// </summary>
    public static float DistanceScale
    {
        get => _distanceScale;
        set
        {
            if (value <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(value), "DistanceScale must be greater than 0.0");
            _distanceScale = value;
        }
    }

    public abstract void SetListenerPosition(Double3 position);
    public abstract Double3 GetListenerPosition();
    public abstract void SetListenerVelocity(Double3 velocity);
    public abstract void SetListenerOrientation(Double3 forward, Double3 up);
    public abstract void SetDopplerFactor(float dopplerFactor);
    public abstract void SetSpeedOfSound(float speedOfSound);
    public abstract ActiveAudio CreateAudioSource();
    public abstract AudioBuffer CreateAudioBuffer();
}
