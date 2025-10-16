// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime.Rendering;
using Prowl.Vector;

namespace Prowl.Runtime;

public class SpotLight : Light
{
    public enum Resolution : int
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
    }

    public Resolution ShadowResolution = Resolution._512;

    public double Range = 10f;
    public double InnerAngle = 30f; // Inner cone angle in degrees
    public double OuterAngle = 45f; // Outer cone angle in degrees

    public override void Update()
    {
        GameObject.Scene.PushLight(this);
    }

    public override void DrawGizmos()
    {
        Debug.DrawArrow(Transform.Position, Transform.Forward * Range, Color);
        Debug.DrawWireCircle(Transform.Position + Transform.Forward * Range, -Transform.Forward, Range * Maths.Tan(OuterAngle * 0.5f * Maths.Deg2Rad), Color);
    }

    public override LightType GetLightType() => LightType.Spot;

    public override void GetShadowMatrix(out Double4x4 view, out Double4x4 projection)
    {
        Double3 forward = Transform.Forward;
        // Use perspective projection for spot light shadows
        double fov = OuterAngle * 2.0f; // Full cone angle
        projection = Double4x4.CreatePerspectiveFov(fov * Maths.Deg2Rad, 1.0f, 0.1f, Range);
        view = Double4x4.CreateLookTo(Transform.Position, forward, Transform.Up);
    }

    public void UploadToGPU(bool cameraRelative, Double3 cameraPosition, int atlasX, int atlasY, int atlasWidth, int lightIndex)
    {
        Double3 position = cameraRelative ? Transform.Position - cameraPosition : Transform.Position;
        Double3 colorVec = new(Color.R, Color.G, Color.B);
        float innerAngleCos = (float)Maths.Cos(InnerAngle * 0.5f * Maths.Deg2Rad);
        float outerAngleCos = (float)Maths.Cos(OuterAngle * 0.5f * Maths.Deg2Rad);

        GetShadowMatrix(out Double4x4 view, out Double4x4 proj);

        if (cameraRelative)
            view.Translation -= new Double4(cameraPosition.X, cameraPosition.Y, cameraPosition.Z, 0.0f);

        Double4x4 shadowMatrix = proj * view;

        // Use GlobalUniforms to set packed spot light data
        if (CastShadows)
        {
            GlobalUniforms.SetSpotLightData(
                lightIndex,
                position,
                Transform.Forward,
                colorVec,
                Intensity,
                Range,
                innerAngleCos,
                outerAngleCos,
                ShadowBias,
                ShadowNormalBias,
                ShadowStrength,
                (float)ShadowQuality,
                atlasX,
                atlasY,
                atlasWidth,
                shadowMatrix
            );
        }
        else
        {
            GlobalUniforms.SetSpotLightData(
                lightIndex,
                position,
                Transform.Forward,
                colorVec,
                Intensity,
                Range,
                innerAngleCos,
                outerAngleCos,
                ShadowBias,
                ShadowNormalBias,
                0, // shadowStrength = 0
                (float)ShadowQuality,
                -1, // atlasX = -1
                -1, // atlasY = -1
                0,  // atlasWidth = 0
                shadowMatrix
            );
        }
    }
}
