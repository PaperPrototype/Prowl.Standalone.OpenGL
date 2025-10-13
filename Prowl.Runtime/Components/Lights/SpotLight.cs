// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Vector;
using Prowl.Runtime.Rendering;

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

    public Resolution shadowResolution = Resolution._512;

    public float range = 10f;
    public float innerAngle = 30f; // Inner cone angle in degrees
    public float outerAngle = 45f; // Outer cone angle in degrees

    public override void Update()
    {
        GameObject.Scene.PushLight(this);
        Debug.DrawArrow(Transform.position, Transform.forward * range, color);
        Debug.DrawWireCircle(Transform.position + Transform.forward * range, -Transform.forward, range * Maths.Tan(outerAngle * 0.5f * Maths.Deg2Rad), color);
    }

    public override LightType GetLightType() => LightType.Spot;

    public override void GetShadowMatrix(out Double4x4 view, out Double4x4 projection)
    {
        Double3 forward = Transform.forward;
        // Use perspective projection for spot light shadows
        float fov = outerAngle * 2.0f; // Full cone angle
        projection = Double4x4.CreatePerspectiveFov(fov * Maths.Deg2Rad, 1.0f, 0.1f, range);
        view = Double4x4.CreateLookTo(Transform.position, forward, Transform.up);
    }

    public void UploadToGPU(bool cameraRelative, Double3 cameraPosition, int atlasX, int atlasY, int atlasWidth, int lightIndex)
    {
        Double3 position = cameraRelative ? Transform.position - cameraPosition : Transform.position;
        Double3 colorVec = new Double3(color.R, color.G, color.B);
        float innerAngleCos = (float)Maths.Cos(innerAngle * 0.5f * Maths.Deg2Rad);
        float outerAngleCos = (float)Maths.Cos(outerAngle * 0.5f * Maths.Deg2Rad);

        GetShadowMatrix(out var view, out var proj);

        if (cameraRelative)
            view.Translation -= new Double4(cameraPosition.X, cameraPosition.Y, cameraPosition.Z, 0.0f);

        Double4x4 shadowMatrix = proj * view;

        // Use GlobalUniforms to set packed spot light data
        if (castShadows)
        {
            GlobalUniforms.SetSpotLightData(
                lightIndex,
                position,
                Transform.forward,
                colorVec,
                intensity,
                range,
                innerAngleCos,
                outerAngleCos,
                shadowBias,
                shadowNormalBias,
                shadowStrength,
                (float)shadowQuality,
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
                Transform.forward,
                colorVec,
                intensity,
                range,
                innerAngleCos,
                outerAngleCos,
                shadowBias,
                shadowNormalBias,
                0, // shadowStrength = 0
                (float)shadowQuality,
                -1, // atlasX = -1
                -1, // atlasY = -1
                0,  // atlasWidth = 0
                shadowMatrix
            );
        }
    }
}
