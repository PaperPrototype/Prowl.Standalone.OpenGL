// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime.Audio;
using Prowl.Vector;

namespace Prowl.Runtime;

public sealed class AudioSource : MonoBehaviour
{
    public AudioClip Clip;
    public bool PlayOnStart = true;
    public bool Looping = false;
    public double Volume = 1f;
    public double Pitch = 1f;
    public double MaxDistance = 32f;
    public double ReferenceDistance = 1f;

    private ActiveAudio _source;
    private AudioBuffer _buffer;
    private uint _lastVersion;
    private bool _looping = false;
    private double _gain = 1f;
    private double _pitch = 1f;
    private double _maxDistance = 32f;
    private double _refDistance = 1f;

    public void Play()
    {
        if (Clip.IsValid())
            _source.Play(_buffer);
    }

    public void Stop()
    {
        if (Clip.IsValid())
            _source?.Stop();
    }

    public override void OnEnable()
    {
        _source = AudioSystem.Engine.CreateAudioSource();
        _source.PositionKind = AudioPositionKind.AbsoluteWorld;
        _source.Position = GameObject.Transform.Position;
        _source.Direction = GameObject.Transform.Forward;
        _source.Gain = Volume;
        _source.Pitch = Pitch;
        _source.Looping = Looping;
        _source.MaxDistance = MaxDistance;
        _source.ReferenceDistance = ReferenceDistance;

        Debug.Log($"AudioSource {GameObject.Name} created: Pos={_source.Position}, MaxDist={_source.MaxDistance}, RefDist={_source.ReferenceDistance}, PositionKind={_source.PositionKind}");

        if (Clip.IsValid())
        {
            _buffer = AudioSystem.GetAudioBuffer(Clip);

            // Warn if using stereo audio with 3D AudioSource
            if (Clip.Channels == 2)
            {
                Debug.LogWarning($"AudioSource '{GameObject.Name}' is using stereo audio clip '{Clip.Name}'. " +
                    "Stereo audio cannot be spatialized in 3D - distance attenuation and panning will not work. " +
                    "Use AudioClip.LoadFromFile(path, enforceMono: true) to automatically convert to mono.");
            }
        }
    }

    public override void Start()
    {
        if (PlayOnStart) // OnEnable should always be called Before start() So _source should not ever be null here.
            Play();
    }

    public override void Update()
    {
        // Update position in world space
        _source.Position = GameObject.Transform.Position;
        _source.Direction = GameObject.Transform.Forward;

        if (Clip.IsValid())
            _buffer = AudioSystem.GetAudioBuffer(Clip);

        if (_looping != Looping)
        {
            _source.Looping = Looping;
            _looping = Looping;
        }

        if (_gain != Volume)
        {
            _source.Gain = Volume;
            _gain = Volume;
        }

        if (_pitch != Pitch)
        {
            _source.Pitch = Pitch;
            _pitch = Pitch;
        }

        if (_maxDistance != MaxDistance)
        {
            _source.MaxDistance = MaxDistance;
            _maxDistance = MaxDistance;
        }

        if (_refDistance != ReferenceDistance)
        {
            _source.ReferenceDistance = ReferenceDistance;
            _refDistance = ReferenceDistance;
        }
    }

    public override void DrawGizmos()
    {
        Debug.DrawWireSphere(Transform.Position, MaxDistance, Vector.Color.Yellow);
    }

    public override void OnDisable()
    {
        _source.Stop();
        _source.Dispose();
        _source = null;
    }
}
