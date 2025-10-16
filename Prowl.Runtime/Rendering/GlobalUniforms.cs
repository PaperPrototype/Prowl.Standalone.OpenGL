// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Runtime.InteropServices;

using Prowl.Runtime.GraphicsBackend;
using Prowl.Runtime.GraphicsBackend.Primitives;
using Prowl.Vector;

namespace Prowl.Runtime.Rendering;

/// <summary>
/// Structure matching the layout of global uniforms in ShaderVariables.glsl
/// Uses std140 layout for uniform buffer compatibility
/// Contains only per-frame data that is constant across all draw calls
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct GlobalUniformsData
{
    // Camera matrices (each mat4 = 64 bytes)
    public Float4x4 prowl_MatV;               // 64 bytes
    public Float4x4 prowl_MatIV;              // 64 bytes
    public Float4x4 prowl_MatP;               // 64 bytes
    public Float4x4 prowl_MatVP;              // 64 bytes
    public Float4x4 prowl_PrevViewProj;       // 64 bytes

    // Camera parameters
    public Float3 _WorldSpaceCameraPos;       // 12 bytes
    public float _padding0;                   // 4 bytes (padding)

    public Float4 _ProjectionParams;          // 16 bytes
    public Float4 _ScreenParams;              // 16 bytes
    public Float2 _CameraJitter;              // 8 bytes
    public Float2 _CameraPreviousJitter;      // 8 bytes

    // Time parameters
    public Float4 _Time;                      // 16 bytes
    public Float4 _SinTime;                   // 16 bytes
    public Float4 _CosTime;                   // 16 bytes
    public Float4 prowl_DeltaTime;            // 16 bytes

    // Fog parameters
    public Float4 prowl_FogColor;             // 16 bytes
    public Float4 prowl_FogParams;            // 16 bytes
    public Float3 prowl_FogStates;            // 12 bytes
    public float _padding1;                   // 4 bytes padding

    // Ambient light parameters
    public Float2 prowl_AmbientMode;          // 8 bytes
    public Float2 _padding2;                  // 8 bytes padding
    public Float4 prowl_AmbientColor;         // 16 bytes
    public Float4 prowl_AmbientSkyColor;      // 16 bytes
    public Float4 prowl_AmbientGroundColor;   // 16 bytes

    // Shadow parameters
    public Float2 prowl_ShadowAtlasSize;      // 8 bytes
    public Float2 _padding3;                  // 8 bytes padding

    // Directional Light (Sun) - 144 bytes total
    public Float3 prowl_SunDirection;         // 12 bytes
    public float prowl_SunIntensity;          // 4 bytes
    public Float3 prowl_SunColor;             // 12 bytes
    public float prowl_SunShadowBias;         // 4 bytes
    public Float4x4 prowl_SunShadowMatrix;    // 64 bytes
    public Float4 prowl_SunShadowParams;      // 16 bytes (x: shadowNormalBias, y: shadowStrength, z: shadowDistance, w: shadowQuality)
    public Float4 prowl_SunAtlasParams;       // 16 bytes (x: atlasX, y: atlasY, z: atlasWidth, w: unused)

    // Point Lights - 4 lights packed
    public Float4 prowl_4PointLightPosX;      // 16 bytes - X positions of 4 point lights
    public Float4 prowl_4PointLightPosY;      // 16 bytes - Y positions of 4 point lights
    public Float4 prowl_4PointLightPosZ;      // 16 bytes - Z positions of 4 point lights
    public Float4 prowl_4PointLightColorR;    // 16 bytes - R color of 4 point lights
    public Float4 prowl_4PointLightColorG;    // 16 bytes - G color of 4 point lights
    public Float4 prowl_4PointLightColorB;    // 16 bytes - B color of 4 point lights
    public Float4 prowl_4PointLightIntensity; // 16 bytes - Intensity of 4 point lights
    public Float4 prowl_4PointLightRange;     // 16 bytes - Range of 4 point lights
    public Float4 prowl_4PointLightShadowBias;        // 16 bytes
    public Float4 prowl_4PointLightShadowNormalBias;  // 16 bytes
    public Float4 prowl_4PointLightShadowStrength;    // 16 bytes
    public Float4 prowl_4PointLightShadowQuality;     // 16 bytes
    public Float4 prowl_4PointLightAtlasX;    // 16 bytes
    public Float4 prowl_4PointLightAtlasY;    // 16 bytes
    public Float4 prowl_4PointLightAtlasWidth;// 16 bytes

    // Spot Lights - 4 lights packed
    public Float4 prowl_4SpotLightPosX;       // 16 bytes - X positions of 4 spot lights
    public Float4 prowl_4SpotLightPosY;       // 16 bytes - Y positions of 4 spot lights
    public Float4 prowl_4SpotLightPosZ;       // 16 bytes - Z positions of 4 spot lights
    public Float4 prowl_4SpotLightDirX;       // 16 bytes - X directions of 4 spot lights
    public Float4 prowl_4SpotLightDirY;       // 16 bytes - Y directions of 4 spot lights
    public Float4 prowl_4SpotLightDirZ;       // 16 bytes - Z directions of 4 spot lights
    public Float4 prowl_4SpotLightColorR;     // 16 bytes - R color of 4 spot lights
    public Float4 prowl_4SpotLightColorG;     // 16 bytes - G color of 4 spot lights
    public Float4 prowl_4SpotLightColorB;     // 16 bytes - B color of 4 spot lights
    public Float4 prowl_4SpotLightIntensity;  // 16 bytes - Intensity of 4 spot lights
    public Float4 prowl_4SpotLightRange;      // 16 bytes - Range of 4 spot lights
    public Float4 prowl_4SpotLightInnerAngle; // 16 bytes - Inner cone angle (cosine) of 4 spot lights
    public Float4 prowl_4SpotLightOuterAngle; // 16 bytes - Outer cone angle (cosine) of 4 spot lights
    public Float4 prowl_4SpotLightShadowBias;       // 16 bytes
    public Float4 prowl_4SpotLightShadowNormalBias; // 16 bytes
    public Float4 prowl_4SpotLightShadowStrength;   // 16 bytes
    public Float4 prowl_4SpotLightShadowQuality;    // 16 bytes
    public Float4 prowl_4SpotLightAtlasX;     // 16 bytes
    public Float4 prowl_4SpotLightAtlasY;     // 16 bytes
    public Float4 prowl_4SpotLightAtlasWidth; // 16 bytes
    public Float4x4 prowl_SpotLightShadowMatrix0; // 64 bytes - Shadow matrix for spot light 0
    public Float4x4 prowl_SpotLightShadowMatrix1; // 64 bytes - Shadow matrix for spot light 1
    public Float4x4 prowl_SpotLightShadowMatrix2; // 64 bytes - Shadow matrix for spot light 2
    public Float4x4 prowl_SpotLightShadowMatrix3; // 64 bytes - Shadow matrix for spot light 3

    // Light counts
    public int prowl_PointLightCount;         // 4 bytes
    public int prowl_SpotLightCount;          // 4 bytes
    public Float2 _padding4;                  // 8 bytes padding
}

/// <summary>
/// Manages the global uniform buffer for efficient shader data upload
/// </summary>
public static class GlobalUniforms
{
    private static GraphicsBuffer? s_uniformBuffer;
    private static GlobalUniformsData s_data;
    private static bool s_isDirty = true;

    /// <summary>
    /// Initializes the global uniform buffer
    /// </summary>
    public static void Initialize()
    {
        if (s_uniformBuffer == null)
        {
            // Create a dynamic uniform buffer
            s_uniformBuffer = Graphics.Device.CreateBuffer<GlobalUniformsData>(
                BufferType.UniformBuffer,
                [s_data],
                true
            );
            s_isDirty = true;
        }
    }

    /// <summary>
    /// Updates the GPU buffer if data has changed
    /// </summary>
    public static void Upload()
    {
        Initialize();

        if (s_isDirty && s_uniformBuffer != null)
        {
            Graphics.Device.UpdateBuffer(s_uniformBuffer, 0, [s_data]);
            s_isDirty = false;
        }
    }

    /// <summary>
    /// Gets the uniform buffer for binding to shaders
    /// </summary>
    public static GraphicsBuffer GetBuffer()
    {
        Initialize();
        return s_uniformBuffer!;
    }

    /// <summary>
    /// Cleans up the global uniform buffer resources
    /// </summary>
    public static void Dispose()
    {
        s_uniformBuffer?.Dispose();
        s_uniformBuffer = null;
    }

    // Camera matrix setters (per-frame data)
    public static void SetMatrixV(Double4x4 value)
    {
        s_data.prowl_MatV = (Float4x4)value;
        s_isDirty = true;
    }

    public static void SetMatrixIV(Double4x4 value)
    {
        s_data.prowl_MatIV = (Float4x4)value;
        s_isDirty = true;
    }

    public static void SetMatrixP(Double4x4 value)
    {
        s_data.prowl_MatP = (Float4x4)value;
        s_isDirty = true;
    }

    public static void SetMatrixVP(Double4x4 value)
    {
        s_data.prowl_MatVP = (Float4x4)value;
        s_isDirty = true;
    }

    public static void SetPrevViewProj(Double4x4 value)
    {
        s_data.prowl_PrevViewProj = (Float4x4)value;
        s_isDirty = true;
    }

    // Camera parameters
    public static void SetWorldSpaceCameraPos(Double3 value)
    {
        s_data._WorldSpaceCameraPos = (Float3)value;
        s_isDirty = true;
    }

    public static void SetProjectionParams(Double4 value)
    {
        s_data._ProjectionParams = (Float4)value;
        s_isDirty = true;
    }

    public static void SetScreenParams(Double4 value)
    {
        s_data._ScreenParams = (Float4)value;
        s_isDirty = true;
    }

    public static void SetCameraJitter(Double2 value)
    {
        s_data._CameraJitter = (Float2)value;
        s_isDirty = true;
    }

    public static void SetCameraPreviousJitter(Double2 value)
    {
        s_data._CameraPreviousJitter = (Float2)value;
        s_isDirty = true;
    }

    // Time parameters
    public static void SetTime(Double4 value)
    {
        s_data._Time = (Float4)value;
        s_isDirty = true;
    }

    public static void SetSinTime(Double4 value)
    {
        s_data._SinTime = (Float4)value;
        s_isDirty = true;
    }

    public static void SetCosTime(Double4 value)
    {
        s_data._CosTime = (Float4)value;
        s_isDirty = true;
    }

    public static void SetDeltaTime(Double4 value)
    {
        s_data.prowl_DeltaTime = (Float4)value;
        s_isDirty = true;
    }

    // Fog parameters
    public static void SetFogColor(Double4 value)
    {
        s_data.prowl_FogColor = (Float4)value;
        s_isDirty = true;
    }

    public static void SetFogParams(Double4 value)
    {
        s_data.prowl_FogParams = (Float4)value;
        s_isDirty = true;
    }

    public static void SetFogStates(Float3 value)
    {
        s_data.prowl_FogStates = value;
        s_isDirty = true;
    }

    // Ambient light parameters
    public static void SetAmbientMode(Double2 value)
    {
        s_data.prowl_AmbientMode = (Float2)value;
        s_isDirty = true;
    }

    public static void SetAmbientColor(Double4 value)
    {
        s_data.prowl_AmbientColor = (Float4)value;
        s_isDirty = true;
    }

    public static void SetAmbientSkyColor(Double4 value)
    {
        s_data.prowl_AmbientSkyColor = (Float4)value;
        s_isDirty = true;
    }

    public static void SetAmbientGroundColor(Double4 value)
    {
        s_data.prowl_AmbientGroundColor = (Float4)value;
        s_isDirty = true;
    }

    // Shadow parameters
    public static void SetShadowAtlasSize(Double2 value)
    {
        s_data.prowl_ShadowAtlasSize = (Float2)value;
        s_isDirty = true;
    }

    // Directional Light (Sun) parameters
    public static void SetSunDirection(Double3 value)
    {
        s_data.prowl_SunDirection = (Float3)value;
        s_isDirty = true;
    }

    public static void SetSunIntensity(double value)
    {
        s_data.prowl_SunIntensity = (float)value;
        s_isDirty = true;
    }

    public static void SetSunColor(Double3 value)
    {
        s_data.prowl_SunColor = (Float3)value;
        s_isDirty = true;
    }

    public static void SetSunShadowBias(double value)
    {
        s_data.prowl_SunShadowBias = (float)value;
        s_isDirty = true;
    }

    public static void SetSunShadowMatrix(Double4x4 value)
    {
        s_data.prowl_SunShadowMatrix = (Float4x4)value;
        s_isDirty = true;
    }

    public static void SetSunShadowParams(Double4 value)
    {
        s_data.prowl_SunShadowParams = (Float4)value;
        s_isDirty = true;
    }

    public static void SetSunAtlasParams(Double4 value)
    {
        s_data.prowl_SunAtlasParams = (Float4)value;
        s_isDirty = true;
    }

    // Point Lights - packed setters
    public static void SetPointLightData(int index, Double3 position, Double3 color, double intensity, double range,
        double shadowBias, double shadowNormalBias, double shadowStrength, double shadowQuality,
        double atlasX, double atlasY, double atlasWidth)
    {
        if (index < 0 || index >= 4) return;

        // Position
        s_data.prowl_4PointLightPosX[index] = (float)position.X;
        s_data.prowl_4PointLightPosY[index] = (float)position.Y;
        s_data.prowl_4PointLightPosZ[index] = (float)position.Z;

        // Color
        s_data.prowl_4PointLightColorR[index] = (float)color.X;
        s_data.prowl_4PointLightColorG[index] = (float)color.Y;
        s_data.prowl_4PointLightColorB[index] = (float)color.Z;

        // Parameters
        s_data.prowl_4PointLightIntensity[index] = (float)intensity;
        s_data.prowl_4PointLightRange[index] = (float)range;

        // Shadow parameters
        s_data.prowl_4PointLightShadowBias[index] = (float)shadowBias;
        s_data.prowl_4PointLightShadowNormalBias[index] = (float)shadowNormalBias;
        s_data.prowl_4PointLightShadowStrength[index] = (float)shadowStrength;
        s_data.prowl_4PointLightShadowQuality[index] = (float)shadowQuality;

        // Atlas parameters
        s_data.prowl_4PointLightAtlasX[index] = (float)atlasX;
        s_data.prowl_4PointLightAtlasY[index] = (float)atlasY;
        s_data.prowl_4PointLightAtlasWidth[index] = (float)atlasWidth;

        s_isDirty = true;
    }

    public static void SetPointLightCount(int count)
    {
        s_data.prowl_PointLightCount = count;
        s_isDirty = true;
    }

    // Spot Lights - packed setters
    public static void SetSpotLightData(int index, Double3 position, Double3 direction, Double3 color,
        double intensity, double range, double innerAngle, double outerAngle,
        double shadowBias, double shadowNormalBias, double shadowStrength, double shadowQuality,
        double atlasX, double atlasY, double atlasWidth, Double4x4 shadowMatrix)
    {
        if (index < 0 || index >= 4) return;

        // Position
        s_data.prowl_4SpotLightPosX[index] = (float)position.X;
        s_data.prowl_4SpotLightPosY[index] = (float)position.Y;
        s_data.prowl_4SpotLightPosZ[index] = (float)position.Z;

        // Direction
        s_data.prowl_4SpotLightDirX[index] = (float)direction.X;
        s_data.prowl_4SpotLightDirY[index] = (float)direction.Y;
        s_data.prowl_4SpotLightDirZ[index] = (float)direction.Z;

        // Color
        s_data.prowl_4SpotLightColorR[index] = (float)color.X;
        s_data.prowl_4SpotLightColorG[index] = (float)color.Y;
        s_data.prowl_4SpotLightColorB[index] = (float)color.Z;

        // Parameters
        s_data.prowl_4SpotLightIntensity[index] = (float)intensity;
        s_data.prowl_4SpotLightRange[index] = (float)range;
        s_data.prowl_4SpotLightInnerAngle[index] = (float)innerAngle;
        s_data.prowl_4SpotLightOuterAngle[index] = (float)outerAngle;

        // Shadow parameters
        s_data.prowl_4SpotLightShadowBias[index] = (float)shadowBias;
        s_data.prowl_4SpotLightShadowNormalBias[index] = (float)shadowNormalBias;
        s_data.prowl_4SpotLightShadowStrength[index] = (float)shadowStrength;
        s_data.prowl_4SpotLightShadowQuality[index] = (float)shadowQuality;

        // Atlas parameters
        s_data.prowl_4SpotLightAtlasX[index] = (float)atlasX;
        s_data.prowl_4SpotLightAtlasY[index] = (float)atlasY;
        s_data.prowl_4SpotLightAtlasWidth[index] = (float)atlasWidth;

        // Shadow matrix
        switch (index)
        {
            case 0: s_data.prowl_SpotLightShadowMatrix0 = (Float4x4)shadowMatrix; break;
            case 1: s_data.prowl_SpotLightShadowMatrix1 = (Float4x4)shadowMatrix; break;
            case 2: s_data.prowl_SpotLightShadowMatrix2 = (Float4x4)shadowMatrix; break;
            case 3: s_data.prowl_SpotLightShadowMatrix3 = (Float4x4)shadowMatrix; break;
        }

        s_isDirty = true;
    }

    public static void SetSpotLightCount(int count)
    {
        s_data.prowl_SpotLightCount = count;
        s_isDirty = true;
    }

    /// <summary>
    /// Resets all data to defaults
    /// </summary>
    public static void Clear()
    {
        s_data = new GlobalUniformsData();
        s_isDirty = true;
    }
}
