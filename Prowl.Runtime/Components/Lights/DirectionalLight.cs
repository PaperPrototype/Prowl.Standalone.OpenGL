// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Vector;
using Prowl.Runtime.Rendering;

namespace Prowl.Runtime;

public class DirectionalLight : Light
{
    public enum Resolution : int
    {
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
    }

    public Resolution shadowResolution = Resolution._1024;

    public float shadowDistance = 50f;

    public override void Update()
    {
        GameObject.Scene.PushLight(this);
        Debug.DrawArrow(Transform.position, -Transform.forward, Color.Yellow);
        Debug.DrawWireCircle(Transform.position, Transform.forward, 0.5f, Color.Yellow);
    }


    public override LightType GetLightType() => LightType.Directional;
    public override void GetShadowMatrix(out Double4x4 view, out Double4x4 projection)
    {
        Double3 forward = -Transform.forward;
        projection = Double4x4.CreateOrtho(shadowDistance, shadowDistance, 0.1f, shadowDistance);
        view = Double4x4.CreateLookTo(Transform.position - (forward * shadowDistance * 0.5), forward, Transform.up);
    }

    public void UploadToGPU(bool cameraRelative, Double3 cameraPosition, int atlasX, int atlasY, int atlasWidth)
    {
        GetShadowMatrix(out var view, out var proj);

        if (cameraRelative)
            view.Translation -= new Double4(cameraPosition.X, cameraPosition.Y, cameraPosition.Z, 0.0f);

        // Use GlobalUniforms to set directional light data
        GlobalUniforms.SetSunDirection(Transform.forward);
        GlobalUniforms.SetSunColor(new Double3(color.R, color.G, color.B));
        GlobalUniforms.SetSunIntensity(intensity);
        GlobalUniforms.SetSunShadowBias(shadowBias);
        GlobalUniforms.SetSunShadowMatrix(proj * view);
        GlobalUniforms.SetSunShadowParams(new Double4(shadowNormalBias, shadowStrength, shadowDistance, (float)shadowQuality));
        GlobalUniforms.SetSunAtlasParams(new Double4(atlasX, atlasY, atlasWidth, 0));
    }
}
