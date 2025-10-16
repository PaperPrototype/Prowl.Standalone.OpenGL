// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime.Audio;

namespace Prowl.Runtime;

public sealed class AudioSource : MonoBehaviour
{
    public AudioClip Clip;
    public bool PlayOnStart = true;
    public bool Looping = false;
    public double Volume = 1f;
    public double MaxDistance = 32f;

    private ActiveAudio _source;
    private AudioBuffer _buffer;
    private uint _lastVersion;
    private bool _looping = false;
    private double _gain = 1f;
    private double _maxDistance = 32f;

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
        _source.PositionKind = AudioPositionKind.ListenerRelative;
        // position relative to listener
        Vector.Transform listener = AudioSystem.Listener.GameObject.Transform;
        Vector.Double3 thisPos = GameObject.Transform.Position;
        _source.Position = listener.InverseTransformPoint(thisPos);
        _source.Direction = GameObject.Transform.Forward;
        _source.Gain = Volume;
        _source.Looping = Looping;
        _source.MaxDistance = MaxDistance;
        if (Clip.IsValid())
            _buffer = AudioSystem.GetAudioBuffer(Clip);
    }

    public override void Start()
    {
        if (PlayOnStart) // OnEnable should always be called Before start() So _source should not ever be null here.
            Play();
    }

    public override void Update()
    {
        //if (_lastVersion != GameObject.transform.version)
        {
            Vector.Transform listener = AudioSystem.Listener.GameObject.Transform;
            Vector.Double3 thisPos = GameObject.Transform.Position;
            _source.Position = listener.InverseTransformPoint(thisPos);
            _source.Direction = GameObject.Transform.Forward;
            //_lastVersion = GameObject.transform.version;
        }

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

        if (_maxDistance != MaxDistance)
        {
            _source.MaxDistance = MaxDistance;
            _maxDistance = MaxDistance;
        }
    }

    public override void OnDisable()
    {
        _source.Stop();
        _source.Dispose();
        _source = null;
    }
}
