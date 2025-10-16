﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime.Rendering;
using Prowl.Vector;

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

    public Resolution ShadowResolution = Resolution._1024;

    public double ShadowDistance = 50f;

    public override void Update()
    {
        GameObject.Scene.PushLight(this);
    }

    public override void DrawGizmos()
    {
        Debug.DrawArrow(Transform.Position, -Transform.Forward, Color.Yellow);
        Debug.DrawWireCircle(Transform.Position, Transform.Forward, 0.5f, Color.Yellow);
    }


    public override LightType GetLightType() => LightType.Directional;
    public override void GetShadowMatrix(out Double4x4 view, out Double4x4 projection)
    {
        Double3 forward = -Transform.Forward;
        projection = Double4x4.CreateOrtho(ShadowDistance, ShadowDistance, 0.1f, ShadowDistance);
        view = Double4x4.CreateLookTo(Transform.Position - (forward * ShadowDistance * 0.5), forward, Transform.Up);
    }

    public void GetShadowMatrix(Double3 cameraPosition, int shadowResolution, out Double4x4 view, out Double4x4 projection)
    {
        Double3 forward = -Transform.Forward;
        projection = Double4x4.CreateOrtho(ShadowDistance, ShadowDistance, 0.1f, ShadowDistance);

        // Calculate texel size in world units
        double texelSize = (ShadowDistance * 2.0) / shadowResolution;

        // Build orthonormal basis for light space
        Double3 lightUp = Double3.Normalize(Transform.Up);
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
        view = Double4x4.CreateLookTo(snappedPosition - (forward * ShadowDistance * 0.5), forward, Transform.Up);
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
        GlobalUniforms.SetSunDirection(Transform.Forward);
        GlobalUniforms.SetSunColor(new Double3(Color.R, Color.G, Color.B));
        GlobalUniforms.SetSunIntensity(Intensity);
        GlobalUniforms.SetSunShadowBias(ShadowBias);
        GlobalUniforms.SetSunShadowMatrix(proj * view);
        GlobalUniforms.SetSunShadowParams(new Double4(ShadowNormalBias, ShadowStrength, ShadowDistance, (double)ShadowQuality));
        GlobalUniforms.SetSunAtlasParams(new Double4(atlasX, atlasY, atlasWidth, 0));
    }
}
