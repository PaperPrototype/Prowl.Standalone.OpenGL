#ifndef SHADER_SHADERVARIABLES
#define SHADER_SHADERVARIABLES

// Global Uniform Buffer containing per-frame shared rendering data
// Uses std140 layout for compatibility across all GPUs
// This data is uploaded once per frame and is constant across all draw calls
// Note: binding qualifier requires GLSL 420+, otherwise binding is set via glUniformBlockBinding in C# code
#if __VERSION__ >= 420
layout(std140, binding = 0) uniform GlobalUniforms
#else
layout(std140) uniform GlobalUniforms
#endif
{
    // Camera matrices
    mat4 prowl_MatV;
    mat4 prowl_MatIV;
    mat4 prowl_MatP;
    mat4 prowl_MatVP;
    mat4 prowl_PrevViewProj;

    // Camera parameters
    vec3 _WorldSpaceCameraPos;
    float _padding0;

    vec4 _ProjectionParams;
    vec4 _ScreenParams;
    vec2 _CameraJitter;
    vec2 _CameraPreviousJitter;

    // Time parameters
    vec4 _Time;
    vec4 _SinTime;
    vec4 _CosTime;
    vec4 prowl_DeltaTime;

    // Fog parameters
    vec4 prowl_FogColor;  // RGB color of fog
    vec4 prowl_FogParams; // Packed parameters:
                         // x: density/sqrt(ln(2)) - for Exp2 fog
                         // y: density/ln(2) - for Exp fog
                         // z: -1/(end-start) - for Linear fog
                         // w: end/(end-start) - for Linear fog
    vec3 prowl_FogStates; // x: 1 if linear is enabled, 0 otherwise
                         // y: 1 if exp fog, 0 otherwise
                         // z: 1 if exp2 fog, 0 otherwise
    float _padding1;

    // Ambient light parameters
    vec2 prowl_AmbientMode;    // x: uniform, y: hemisphere
    vec2 _padding2;
    vec4 prowl_AmbientColor;
    vec4 prowl_AmbientSkyColor;
    vec4 prowl_AmbientGroundColor;

    // Shadow parameters
    vec2 prowl_ShadowAtlasSize;
    vec2 _padding3;

    // Directional Light (Sun)
    vec3 prowl_SunDirection;
    float prowl_SunIntensity;
    vec3 prowl_SunColor;
    float prowl_SunShadowBias;
    mat4 prowl_SunShadowMatrix;
    vec4 prowl_SunShadowParams;      // x: shadowNormalBias, y: shadowStrength, z: shadowDistance, w: shadowQuality
    vec4 prowl_SunAtlasParams;       // x: atlasX, y: atlasY, z: atlasWidth, w: unused

    // Point Lights - 4 lights packed
    vec4 prowl_4PointLightPosX;
    vec4 prowl_4PointLightPosY;
    vec4 prowl_4PointLightPosZ;
    vec4 prowl_4PointLightColorR;
    vec4 prowl_4PointLightColorG;
    vec4 prowl_4PointLightColorB;
    vec4 prowl_4PointLightIntensity;
    vec4 prowl_4PointLightRange;
    vec4 prowl_4PointLightShadowBias;
    vec4 prowl_4PointLightShadowNormalBias;
    vec4 prowl_4PointLightShadowStrength;
    vec4 prowl_4PointLightShadowQuality;
    vec4 prowl_4PointLightAtlasX;
    vec4 prowl_4PointLightAtlasY;
    vec4 prowl_4PointLightAtlasWidth;

    // Spot Lights - 4 lights packed
    vec4 prowl_4SpotLightPosX;
    vec4 prowl_4SpotLightPosY;
    vec4 prowl_4SpotLightPosZ;
    vec4 prowl_4SpotLightDirX;
    vec4 prowl_4SpotLightDirY;
    vec4 prowl_4SpotLightDirZ;
    vec4 prowl_4SpotLightColorR;
    vec4 prowl_4SpotLightColorG;
    vec4 prowl_4SpotLightColorB;
    vec4 prowl_4SpotLightIntensity;
    vec4 prowl_4SpotLightRange;
    vec4 prowl_4SpotLightInnerAngle;
    vec4 prowl_4SpotLightOuterAngle;
    vec4 prowl_4SpotLightShadowBias;
    vec4 prowl_4SpotLightShadowNormalBias;
    vec4 prowl_4SpotLightShadowStrength;
    vec4 prowl_4SpotLightShadowQuality;
    vec4 prowl_4SpotLightAtlasX;
    vec4 prowl_4SpotLightAtlasY;
    vec4 prowl_4SpotLightAtlasWidth;
    mat4 prowl_SpotLightShadowMatrix0;
    mat4 prowl_SpotLightShadowMatrix1;
    mat4 prowl_SpotLightShadowMatrix2;
    mat4 prowl_SpotLightShadowMatrix3;

    // Light counts
    int prowl_PointLightCount;
    int prowl_SpotLightCount;
    vec2 _padding4;
};

// Per-object uniforms (set per draw call)
uniform mat4 prowl_ObjectToWorld;
uniform mat4 prowl_WorldToObject;
uniform mat4 prowl_PrevObjectToWorld;
uniform int _ObjectID;

#define PROWL_MATRIX_V prowl_MatV
#define PROWL_MATRIX_VP_PREVIOUS prowl_PrevViewProj
#define PROWL_MATRIX_I_V prowl_MatIV
#define PROWL_MATRIX_P prowl_MatP
#define PROWL_MATRIX_VP prowl_MatVP
#define PROWL_MATRIX_M prowl_ObjectToWorld
#define PROWL_MATRIX_M_PREVIOUS prowl_PrevObjectToWorld

// Derived matrices
mat4 prowl_MatMV = prowl_MatV * prowl_ObjectToWorld;
mat4 prowl_MatMVP = prowl_MatVP * prowl_ObjectToWorld;
mat4 prowl_MatTMV = transpose(prowl_MatV * prowl_ObjectToWorld);
mat4 prowl_MatITMV = transpose(prowl_WorldToObject * prowl_MatIV);

#define PROWL_MATRIX_MV prowl_MatMV
#define PROWL_MATRIX_MVP prowl_MatMVP
#define PROWL_MATRIX_T_MV prowl_MatTMV
#define PROWL_MATRIX_IT_MV prowl_MatITMV

#endif