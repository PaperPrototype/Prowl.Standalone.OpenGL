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
    }

    public override void DrawGizmos()
    {
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

    public void GetShadowMatrix(Double3 cameraPosition, int shadowResolution, out Double4x4 view, out Double4x4 projection)
    {
        Double3 forward = -Transform.forward;
        projection = Double4x4.CreateOrtho(shadowDistance, shadowDistance, 0.1f, shadowDistance);

        // Calculate texel size in world units
        double texelSize = (shadowDistance * 2.0) / shadowResolution;

        // Build orthonormal basis for light space
        Double3 lightUp = Double3.Normalize(Transform.up);
        Double3 lightRight = Double3.Normalize(Double3.Cross(lightUp, forward));
        lightUp = Double3.Normalize(Double3.Cross(forward, lightRight)); // Recompute to ensure orthogonality

        // Project camera position onto the light's perpendicular plane (X and Y in light space)
        double x = Double3.Dot(cameraPosition, lightRight);
        double y = Double3.Dot(cameraPosition, lightUp);

        // Snap to texel grid in light space
        x = Maths.Round(x / texelSize) * texelSize;
        y = Maths.Round(y / texelSize) * texelSize;

        // Reconstruct the snapped position (only X and Y are snapped, keep camera's position along light direction)
        Double3 snappedPosition = (lightRight * x) + (lightUp * y);

        // Position the shadow map at the snapped position, offset back by half the shadow distance
        view = Double4x4.CreateLookTo(snappedPosition - (forward * shadowDistance * 0.5), forward, Transform.up);
    }

    public void UploadToGPU(bool cameraRelative, Double3 cameraPosition, int atlasX, int atlasY, int atlasWidth)
    {
        // Use camera-following shadow matrix when atlas width is available (meaning we have shadow resolution info)
        Double4x4 view, proj;
        if (atlasWidth > 0)
            GetShadowMatrix(cameraPosition, atlasWidth, out view, out proj);
        else
            GetShadowMatrix(out view, out proj);

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
