// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime.Rendering;
using Prowl.Vector;

namespace Prowl.Runtime;

public enum ShadowQuality
{
    Hard = 0,
    Soft = 1
}

public abstract class Light : MonoBehaviour, IRenderableLight
{

    public Color color = Color.White;
    public double intensity = 8.0f;
    public double shadowStrength = 1.0f;
    public double shadowBias = 0.05f;
    public double shadowNormalBias = 1f;
    public bool castShadows = true;
    public ShadowQuality shadowQuality = ShadowQuality.Hard;


    public override void Update()
    {
        GameObject.Scene.PushLight(this);
    }

    public virtual int GetLayer() => this.GameObject.layerIndex;
    public virtual int GetLightID() => this.InstanceID;
    public abstract LightType GetLightType();
    public virtual Double3 GetLightPosition() => Transform.position;
    public virtual Double3 GetLightDirection() => Transform.forward;
    public virtual bool DoCastShadows() => castShadows;
    public abstract void GetShadowMatrix(out Double4x4 view, out Double4x4 projection);
}
