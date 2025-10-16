// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Prowl.Runtime.Rendering;
using Prowl.Vector;

namespace Prowl.Runtime;

public class PointLight : Light
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

    public override void Update()
    {
        GameObject.Scene.PushLight(this);
    }

    public override void DrawGizmos()
    {
        Debug.DrawWireSphere(Transform.Position, Range, Color);
    }

    public override LightType GetLightType() => LightType.Point;

    public override void GetShadowMatrix(out Double4x4 view, out Double4x4 projection)
    {
        // Default implementation - will be overridden by GetShadowMatrixForFace
        projection = Double4x4.CreatePerspectiveFov(90f * Maths.Deg2Rad, 1.0f, 0.1f, Range);
        view = Double4x4.CreateLookTo(Transform.Position, Transform.Forward, Transform.Up);
    }

    // Get shadow matrix for a specific cubemap face
    public void GetShadowMatrixForFace(int faceIndex, out Double4x4 view, out Double4x4 projection, out Double3 forward, out Double3 up)
    {
        // 90 degree FOV perspective projection for cubemap faces
        projection = Double4x4.CreatePerspectiveFov(90f * Maths.Deg2Rad, 1.0f, 0.1f, Range);

        Double3 position = Transform.Position;

        // Define view matrices for each cubemap face
        // 0: +X, 1: -X, 2: +Y, 3: -Y, 4: +Z, 5: -Z
        switch (faceIndex)
        {
            case 0: // Positive X
                forward = Double3.UnitX;
                up = -Double3.UnitY;
                break;
            case 1: // Negative X
                forward = -Double3.UnitX;
                up = -Double3.UnitY;
                break;
            case 2: // Positive Y
                forward = Double3.UnitY;
                up = Double3.UnitZ;
                break;
            case 3: // Negative Y
                forward = -Double3.UnitY;
                up = -Double3.UnitZ;
                break;
            case 4: // Positive Z
                forward = Double3.UnitZ;
                up = -Double3.UnitY;
                break;
            case 5: // Negative Z
                forward = -Double3.UnitZ;
                up = -Double3.UnitY;
                break;
            default:
                throw new ArgumentException($"Invalid face index: {faceIndex}. Must be 0-5.");
        }

        view = Double4x4.CreateLookTo(position, forward, up);
    }

    public void UploadToGPU(bool cameraRelative, Double3 cameraPosition, int atlasX, int atlasY, int atlasWidth, int lightIndex)
    {
        Double3 position = cameraRelative ? Transform.Position - cameraPosition : Transform.Position;
        Double3 colorVec = new(Color.R, Color.G, Color.B);

        // Use GlobalUniforms to set packed point light data
        if (CastShadows && atlasX >= 0)
        {
            GlobalUniforms.SetPointLightData(
                lightIndex,
                position,
                colorVec,
                Intensity,
                Range,
                ShadowBias,
                ShadowNormalBias,
                ShadowStrength,
                (double)ShadowQuality,
                atlasX,
                atlasY,
                atlasWidth
            );
        }
        else
        {
            GlobalUniforms.SetPointLightData(
                lightIndex,
                position,
                colorVec,
                Intensity,
                Range,
                ShadowBias,
                ShadowNormalBias,
                0, // shadowStrength = 0
                (double)ShadowQuality,
                -1, // atlasX = -1
                -1, // atlasY = -1
                0   // atlasWidth = 0
            );
        }
    }
}
