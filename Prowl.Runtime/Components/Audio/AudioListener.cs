// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime.Audio;
using Prowl.Vector;

namespace Prowl.Runtime;

public class AudioListener : MonoBehaviour
{
    private uint _lastVersion;
    private Double3 lastPos;

    public override void OnEnable()
    {
        lastPos = GameObject.Transform.Position;
        AudioSystem.RegisterListener(this);
    }
    public override void OnDisable() => AudioSystem.UnregisterListener(this);
    public override void Update()
    {
        if (_lastVersion != GameObject.Transform.Version)
        {
            AudioSystem.ListenerTransformChanged(GameObject.Transform, lastPos);
            lastPos = GameObject.Transform.Position;
            _lastVersion = GameObject.Transform.Version;
        }
    }
}
