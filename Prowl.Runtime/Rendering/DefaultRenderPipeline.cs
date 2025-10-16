﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;

using Prowl.Runtime.GraphicsBackend.Primitives;
using Prowl.Runtime.Rendering.Shaders;
using Prowl.Runtime.Resources;
using Prowl.Vector;
using Prowl.Vector.Geometry;

using Material = Prowl.Runtime.Resources.Material;
using Mesh = Prowl.Runtime.Resources.Mesh;
using Shader = Prowl.Runtime.Resources.Shader;

// TODO:
// 1. Image Effects need a Dispose method to clean up their resources, Camera needs to call it too

namespace Prowl.Runtime.Rendering;

public sealed class FXAAEffect : ImageEffect
{
    public float EdgeThresholdMax = 0.0625f;  // 0.063 - 0.333 (lower = more AA, slower)
    public float EdgeThresholdMin = 0.0312f;  // 0.0312 - 0.0833 (trims dark edges)
    public float SubpixelQuality = 0.75f;     // 0.0 - 1.0 (subpixel AA amount)

    private Material _mat;

    public override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _mat ??= new Material(Shader.LoadDefault(DefaultShader.FXAA));

        // Set shader parameters
        _mat.SetFloat("_EdgeThresholdMax", EdgeThresholdMax);
        _mat.SetFloat("_EdgeThresholdMin", EdgeThresholdMin);
        _mat.SetFloat("_SubpixelQuality", SubpixelQuality);
        _mat.SetVector("_Resolution", new Double2(source.Width, source.Height));

        // Apply FXAA
        Graphics.Blit(source, destination, _mat, 0);
    }
}

public sealed class TonemapperEffect : ImageEffect
{
    public override bool TransformsToLDR => true;

    public float Contrast = 1.1f;
    public float Saturation = 1.1f;

    Material _mat;

    public override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _mat ??= new Material(Shader.LoadDefault(DefaultShader.Tonemapper));
        _mat.SetFloat("Contrast", Contrast);
        _mat.SetFloat("Saturation", Saturation);
        Graphics.Blit(source, destination, _mat, 0);
    }
}

public sealed class KawaseBloomEffect : ImageEffect
{
    public float Intensity = 1.5f;
    public float Threshold = 0.8f;
    public int Iterations = 6;
    public float Spread = 1f;

    private Material _bloomMaterial;
    private RenderTexture[] _pingPongBuffers = new RenderTexture[2];

    public override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Create material if it doesn't exist
        _bloomMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Bloom));

        int width = source.Width / 4;
        int height = source.Height / 4;

        // Create ping-pong buffers if they don't exist
        for (int i = 0; i < 2; i++)
        {
            if (_pingPongBuffers[i].IsNotValid() || _pingPongBuffers[i].Width != width || _pingPongBuffers[i].Height != height)
            {
                _pingPongBuffers[i]?.Dispose();
                _pingPongBuffers[i] = new RenderTexture(width, height, false, [destination.MainTexture.ImageFormat]);
            }
        }

        // 1. Extract bright areas (threshold pass)
        _bloomMaterial.SetFloat("_Threshold", Threshold);
        Graphics.Blit(source, _pingPongBuffers[0], _bloomMaterial, 0);

        // 2. Apply Kawase blur ping-pong (multiple iterations with increasing radius)
        int current = 0;
        int next = 1;

        for (int i = 0; i < Iterations; i++)
        {
            float offset = (i * 0.5f + 0.5f) * Spread;
            _bloomMaterial.SetFloat("_Offset", offset);
            Graphics.Blit(_pingPongBuffers[current], _pingPongBuffers[next], _bloomMaterial, 1);

            // Swap buffers
            (next, current) = (current, next);
        }

        // 3. Composite the bloom with the original image
        _bloomMaterial.SetTexture("_BloomTex", _pingPongBuffers[current].MainTexture);
        _bloomMaterial.SetFloat("_Intensity", Intensity);
        Graphics.Blit(source, destination, _bloomMaterial, 2);
    }

    public override void OnPostRender(Camera camera)
    {
        // Clean up resources if needed
    }
}

public sealed class BokehDepthOfFieldEffect : ImageEffect
{
    public bool UseAutoFocus = true;
    public float ManualFocusPoint = 0.5f;
    public float FocusStrength = 200.0f;

    //[Range(5.0f, 40.0f)]
    public float BlurRadius = 5.0f;

    //[Range(0.1f, 0.9f)]
    public float Quality = 0.9f;

    //[Range(0.25f, 1.0f)]
    public float DownsampleFactor = 0.5f;

    private Material _mat;
    private RenderTexture _downsampledRT;

    public override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _mat ??= new Material(Shader.LoadDefault(DefaultShader.BokehDoF));

        int width = (int)(source.Width * DownsampleFactor);
        int height = (int)(source.Height * DownsampleFactor);

        // Create or update downsampled render texture if needed
        if (_downsampledRT.IsNotValid() || _downsampledRT.Width != width || _downsampledRT.Height != height)
        {
            if (_downsampledRT.IsValid())
                _downsampledRT.Dispose();

            _downsampledRT = new RenderTexture(width, height, false, [source.MainTexture.ImageFormat]);
        }

        // Set shader properties
        _mat.SetFloat("_BlurRadius", BlurRadius);
        _mat.SetFloat("_FocusStrength", FocusStrength);
        _mat.SetFloat("_Quality", Quality);
        _mat.SetFloat("_ManualFocusPoint", ManualFocusPoint);
        _mat.SetKeyword("AUTOFOCUS", UseAutoFocus);
        _mat.SetVector("_Resolution", new Double2(source.Width, source.Height));

        // Two-pass approach:

        // Pass 1: Apply DoF at reduced resolution
        _mat.SetVector("_Resolution", new Double2(width, height));
        Graphics.Blit(source, _downsampledRT, _mat, 0); // DoFDownsample pass

        // Pass 2: Combine original image with blurred result
        _mat.SetTexture("_MainTex", source.MainTexture);
        _mat.SetTexture("_DownsampledDoF", _downsampledRT.MainTexture);
        _mat.SetVector("_Resolution", new Double2(source.Width, source.Height));
        Graphics.Blit(source, destination, _mat, 1); // DoFCombine pass
    }
}

public sealed class ScreenSpaceReflectionEffect : ImageEffect
{
    public int RayStepCount = 16;
    public float ScreenEdgeFade = 0.1f;

    Material _mat;

    public override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _mat ??= new Material(Shader.LoadDefault(DefaultShader.SSR));

        // Set uniforms
        _mat.SetInt("_RayStepCount", RayStepCount);
        _mat.SetFloat("_ScreenEdgeFade", ScreenEdgeFade);

        // Set textures
        _mat.SetTexture("_MainTex", source.MainTexture);

        // Apply effect
        Graphics.Blit(source, destination, _mat, 0);
    }
}

public struct ViewerData
{
    public Double3 Position;
    public Double3 Forward;
    public Double3 Up;
    public Double3 Right;

    public ViewerData(DefaultRenderPipeline.CameraSnapshot css)
    {
        Position = css.CameraPosition;
        Forward = css.CameraForward;
        Up = css.CameraUp;
        Right = css.CameraRight;
    }

    public ViewerData(Double3 position, Double3 forward, Double3 right, Double3 up) : this()
    {
        Position = position;
        Forward = forward;
        Right = right;
        Up = up;
    }
}

/// <summary>
/// Default rendering pipeline implementation that handles standard forward rendering,
/// post-processing effects, shadows, and debug visualization.
/// </summary>
public class DefaultRenderPipeline : RenderPipeline
{
    const bool CAMERA_RELATIVE = false;


    #region Static Resources

    private static Mesh s_quadMesh;
    private static Mesh s_skyDome;
    private static Material s_defaultMaterial;
    private static Material s_skybox;
    private static Material s_gizmo;

    private static RenderTexture? s_shadowMap;

    public static DefaultRenderPipeline Default { get; } = new();
    public static HashSet<int> ActiveObjectIds { get => s_activeObjectIds; set => s_activeObjectIds = value; }

    private static Dictionary<int, Double4x4> s_prevModelMatrices = [];
    private static HashSet<int> s_activeObjectIds = [];
    private const int CLEANUP_INTERVAL_FRAMES = 120; // Clean up every 120 frames
    private static int s_framesSinceLastCleanup = 0;

    #endregion

    #region Resource Management

    private static void ValidateDefaults()
    {
        s_quadMesh ??= Mesh.GetFullscreenQuad();
        s_defaultMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Standard));
        s_skybox ??= new Material(Shader.LoadDefault(DefaultShader.ProceduralSkybox));
        s_gizmo ??= new Material(Shader.LoadDefault(DefaultShader.Gizmos));

        if (s_skyDome.IsNotValid())
        {
            Model skyDomeModel = Model.LoadDefault(DefaultModel.SkyDome) ?? throw new Exception("SkyDome model not found. Please ensure the model is included in the project.");
            s_skyDome = skyDomeModel.Meshes[0].Mesh;
        }
    }

    private static void CleanupUnusedModelMatrices()
    {
        // Increment frame counter
        s_framesSinceLastCleanup++;

        // Only perform cleanup at specified interval
        if (s_framesSinceLastCleanup < CLEANUP_INTERVAL_FRAMES)
            return;

        s_framesSinceLastCleanup = 0;

        // Remove all matrices that weren't used in this frame
        var unusedKeys = s_prevModelMatrices.Keys
            .Where(key => !ActiveObjectIds.Contains(key))
            .ToList();

        foreach (int key in unusedKeys)
            s_prevModelMatrices.Remove(key);

        // Clear the active IDs set for next frame
        ActiveObjectIds.Clear();
    }

    private static void TrackModelMatrix(int objectId, Double4x4 currentModel)
    {
        // Mark this object ID as active this frame
        ActiveObjectIds.Add(objectId);

        // Store current model matrix for next frame
        if (s_prevModelMatrices.TryGetValue(objectId, out Double4x4 prevModel))
            PropertyState.SetGlobalMatrix("prowl_PrevObjectToWorld", prevModel);
        else
            PropertyState.SetGlobalMatrix("prowl_PrevObjectToWorld", currentModel); // First frame, use current matrix

        s_prevModelMatrices[objectId] = currentModel;
    }

    #endregion

    #region Main Rendering

    public override void Render(Camera camera, in RenderingData data)
    {
        ValidateDefaults();

        // Main rendering with correct order of operations
        Internal_Render(camera, data);

        PropertyState.ClearGlobals();

        // Clean up unused matrices after rendering
        CleanupUnusedModelMatrices();
    }

    private static (List<ImageEffect> all, List<ImageEffect> opaque, List<ImageEffect> final) GatherImageEffects(Camera camera)
    {
        var all = new List<ImageEffect>();
        var opaqueEffects = new List<ImageEffect>();
        var finalEffects = new List<ImageEffect>();

        foreach (ImageEffect effect in camera.Effects)
        {
            all.Add(effect);

            if (effect.IsOpaqueEffect)
                opaqueEffects.Add(effect);
            else
                finalEffects.Add(effect);
        }

        return (all, opaqueEffects, finalEffects);
    }

    private static void SetupGlobalUniforms(CameraSnapshot css)
    {
        // Set View Rect
        //buffer.SetViewports((int)(camera.Viewrect.x * target.Width), (int)(camera.Viewrect.y * target.Height), (int)(camera.Viewrect.width * target.Width), (int)(camera.Viewrect.height * target.Height), 0, 1000);

        GlobalUniforms.SetPrevViewProj(css.PreviousViewProj);

        // Setup Default Uniforms for this frame
        // Camera
        GlobalUniforms.SetWorldSpaceCameraPos(CAMERA_RELATIVE ? Double3.Zero : css.CameraPosition);
        GlobalUniforms.SetProjectionParams(new Double4(1.0f, css.NearClipPlane, css.FarClipPlane, 1.0f / css.FarClipPlane));
        GlobalUniforms.SetScreenParams(new Double4(css.PixelWidth, css.PixelHeight, 1.0f + 1.0f / css.PixelWidth, 1.0f + 1.0f / css.PixelHeight));

        // Time
        GlobalUniforms.SetTime(new Double4(Time.TimeSinceStartup / 20, Time.TimeSinceStartup, Time.TimeSinceStartup * 2, Time.FrameCount));
        GlobalUniforms.SetSinTime(new Double4(Math.Sin(Time.TimeSinceStartup / 8), Math.Sin(Time.TimeSinceStartup / 4), Math.Sin(Time.TimeSinceStartup / 2), Math.Sin(Time.TimeSinceStartup)));
        GlobalUniforms.SetCosTime(new Double4(Math.Cos(Time.TimeSinceStartup / 8), Math.Cos(Time.TimeSinceStartup / 4), Math.Cos(Time.TimeSinceStartup / 2), Math.Cos(Time.TimeSinceStartup)));
        GlobalUniforms.SetDeltaTime(new Double4(Time.DeltaTime, 1.0f / Time.DeltaTime, Time.SmoothDeltaTime, 1.0f / Time.SmoothDeltaTime));

        // Fog
        Scene.FogParams fog = css.Scene.Fog;
        Double4 fogParams;
        fogParams.X = fog.Density / Maths.Sqrt(0.693147181); // ln(2)
        fogParams.Y = fog.Density / 0.693147181; // ln(2)
        fogParams.Z = -1.0 / (fog.End - fog.Start);
        fogParams.W = fog.End / (fog.End - fog.Start);
        GlobalUniforms.SetFogColor(fog.Color);
        GlobalUniforms.SetFogParams(fogParams);
        GlobalUniforms.SetFogStates(new Float3(
            fog.Mode == Scene.FogParams.FogMode.Linear ? 1 : 0,
            fog.Mode == Scene.FogParams.FogMode.Exponential ? 1 : 0,
            fog.Mode == Scene.FogParams.FogMode.ExponentialSquared ? 1 : 0
            ));

        // Ambient Lighting
        Scene.AmbientLightParams ambient = css.Scene.Ambient;
        GlobalUniforms.SetAmbientMode(new Double2(
            ambient.Mode == Scene.AmbientLightParams.AmbientMode.Uniform ? 1 : 0,
            ambient.Mode == Scene.AmbientLightParams.AmbientMode.Hemisphere ? 1 : 0
        ));

        GlobalUniforms.SetAmbientColor(ambient.Color);
        GlobalUniforms.SetAmbientSkyColor(ambient.SkyColor);
        GlobalUniforms.SetAmbientGroundColor(ambient.GroundColor);

        // Upload the global uniform buffer
        GlobalUniforms.Upload();
    }

    private static void AssignCameraMatrices(Double4x4 view, Double4x4 projection)
    {
        GlobalUniforms.SetMatrixV(view);
        GlobalUniforms.SetMatrixIV(view.Invert());
        GlobalUniforms.SetMatrixP(projection);
        GlobalUniforms.SetMatrixVP(projection * view);

        // Upload the global uniform buffer
        GlobalUniforms.Upload();
    }

    #endregion

    #region Scene Rendering

    public struct CameraSnapshot(Camera camera)
    {
        public Scene Scene = camera.Scene;

        public Double3 CameraPosition = camera.Transform.Position;
        public Double3 CameraRight = camera.Transform.Right;
        public Double3 CameraUp = camera.Transform.Up;
        public Double3 CameraForward = camera.Transform.Forward;
        public LayerMask CullingMask = camera.CullingMask;
        public CameraClearFlags ClearFlags = camera.ClearFlags;
        public double NearClipPlane = camera.NearClipPlane;
        public double FarClipPlane = camera.FarClipPlane;
        public uint PixelWidth = camera.PixelWidth;
        public uint PixelHeight = camera.PixelHeight;
        public double Aspect = camera.Aspect;
        public Double4x4 OriginView = camera.OriginViewMatrix;
        public Double4x4 View = CAMERA_RELATIVE ? camera.OriginViewMatrix : camera.ViewMatrix;
        public Double4x4 ViewInverse = (CAMERA_RELATIVE ? camera.OriginViewMatrix : camera.ViewMatrix).Invert();
        public Double4x4 Projection = camera.ProjectionMatrix;
        public Double4x4 PreviousViewProj = camera.PreviousViewProjectionMatrix;
        public Frustrum WorldFrustum = Frustrum.FromMatrix(camera.ProjectionMatrix * camera.ViewMatrix);
        public DepthTextureMode DepthTextureMode = camera.DepthTextureMode; // Flags, Can be None, Normals, MotionVectors
    }

    private static void Internal_Render(Camera camera, in RenderingData data)
    {
        // =======================================================
        // 0. Setup variables, and prepare the camera
        bool isHDR = camera.HDR;
        (List<ImageEffect> all, List<ImageEffect> opaqueEffects, List<ImageEffect> finalEffects) = GatherImageEffects(camera);
        IReadOnlyList<IRenderableLight> lights = camera.GameObject.Scene.Lights;
        Double3 sunDirection = GetSunDirection(lights);
        RenderTexture target = camera.UpdateRenderData();

        PropertyState.SetGlobalVector("_SunDir", sunDirection);

        // =======================================================
        // 1. Pre Cull
        foreach (ImageEffect effect in all)
            effect.OnPreCull(camera);

        // =======================================================
        // 2. Take a snapshot of all Camera data
        CameraSnapshot css = new(camera);
        SetupGlobalUniforms(css);

        // =======================================================
        // 3. Cull Renderables based on Snapshot data
        IReadOnlyList<IRenderable> renderables = camera.GameObject.Scene.Renderables;
        HashSet<int> culledRenderableIndices = [];// CullRenderables(renderables, css.worldFrustum);

        // =======================================================
        // 4. Pre Render
        foreach (ImageEffect effect in all)
            effect.OnPreRender(camera);

        // =======================================================
        // 5. Setup Lighting and Shadows
        SetupLightingAndShadows(css, lights, renderables);

        // 5.1 Re-Assign camera matrices (The Lighting can modify these)
        AssignCameraMatrices(css.View, css.Projection);

        // =======================================================
        // 6. Pre-Depth Pass
        // We draw objects to get the DepthBuffer but we also draw it into a ColorBuffer so we upload it as a Sampleable Texture
        RenderTexture preDepth = RenderTexture.GetTemporaryRT((int)css.PixelWidth, (int)css.PixelHeight, true, []);

        // Bind depth texture as the target
        Graphics.Device.BindFramebuffer(preDepth.frameBuffer);
        Graphics.Device.Clear(0f, 0f, 0, 0f, ClearFlags.Depth | ClearFlags.Stencil);

        // Draw depth for all visible objects
        DrawRenderables(renderables, "RenderOrder", "DepthOnly", new ViewerData(css), culledRenderableIndices, false);

        // =======================================================
        // 6.1. Set the depth texture for use in post-processing
        PropertyState.SetGlobalTexture("_CameraDepthTexture", preDepth.InternalDepth);

        // =======================================================
        // 7. Opaque geometry
        RenderTexture forwardBuffer = RenderTexture.GetTemporaryRT((int)camera.PixelWidth, (int)camera.PixelHeight, true, [
            isHDR ? TextureImageFormat.Float4 : TextureImageFormat.Color4b, // Albedo
            TextureImageFormat.Float2, // Motion Vectors
            TextureImageFormat.Float3, // Normals
            TextureImageFormat.Float2, // Surface
            ]);

        // Copy the depth buffer to the forward buffer
        // This is technically not needed, however, a big reason people do a Pre-Depth pass outside post processing like SSAO
        // Is so the GPU can early cull lighting calculations in forward rendering
        // This turns Forward rendering into essentially deferred in the eyes of lighting, as it now only calculates lighting for pixels that are actually visible
        Graphics.Device.BindFramebuffer(preDepth.frameBuffer, FBOTarget.Read);
        Graphics.Device.BindFramebuffer(forwardBuffer.frameBuffer, FBOTarget.Draw);
        Graphics.Device.BlitFramebuffer(0, 0, preDepth.Width, preDepth.Height, 0, 0, forwardBuffer.Width, forwardBuffer.Height, ClearFlags.Depth, BlitFilter.Nearest);

        // 7.1 Bind the forward buffer fully, The bit only binds it for Drawing into, We need to bind it for reading too
        Graphics.Device.BindFramebuffer(forwardBuffer.frameBuffer);
        switch (camera.ClearFlags)
        {
            case CameraClearFlags.Skybox:
                Graphics.Device.Clear(
                    (float)camera.ClearColor.R,
                    (float)camera.ClearColor.G,
                    (float)camera.ClearColor.B,
                    (float)camera.ClearColor.A,
                    ClearFlags.Color | ClearFlags.Depth
                );

                RenderSkybox(css);
                break;

            case CameraClearFlags.SolidColor:
                Graphics.Device.Clear(
                    (float)camera.ClearColor.R,
                    (float)camera.ClearColor.G,
                    (float)camera.ClearColor.B,
                    (float)camera.ClearColor.A,
                    ClearFlags.Color | ClearFlags.Depth
                );
                break;

            case CameraClearFlags.Depth:
                Graphics.Device.Clear(0, 0, 0, 0, ClearFlags.Depth);
                break;

            case CameraClearFlags.Nothing:
                // Do not clear anything
                break;
        }

        DrawRenderables(renderables, "RenderOrder", "Opaque", new ViewerData(css), culledRenderableIndices, true);

        // 8.1 Set the Motion Vectors Texture for use in post-processing
        PropertyState.SetGlobalTexture("_CameraMotionVectorsTexture", forwardBuffer.InternalTextures[1]);
        // 8.2 Set the Normals Texture for use in post-processing
        PropertyState.SetGlobalTexture("_CameraNormalsTexture", forwardBuffer.InternalTextures[2]);
        // 8.3 Set the Surface Texture for use in post-processing
        PropertyState.SetGlobalTexture("_CameraSurfaceTexture", forwardBuffer.InternalTextures[3]);

        // 9. Apply opaque post-processing effects
        if (opaqueEffects.Count > 0)
            DrawImageEffects(forwardBuffer, opaqueEffects, ref isHDR);

        // 10. Transparent geometry
        DrawRenderables(renderables, "RenderOrder", "Transparent", new ViewerData(css), culledRenderableIndices, false);

        // 11. Apply final post-processing effects
        if (finalEffects.Count > 0)
            DrawImageEffects(forwardBuffer, finalEffects, ref isHDR);


        //if (data.DisplayGizmo)
        RenderGizmos(css);

        // 12. Blit the Result to the camera's Target whether thats the Screen or a RenderTexture

        // 13. Blit Result to target, If target is null Blit will go to the Screen/Window
        Graphics.Blit(forwardBuffer, target, null, 0, false, false);

        // 14. Post Render
        foreach (ImageEffect effect in all)
            effect.OnPostRender(camera);


        RenderTexture.ReleaseTemporaryRT(preDepth);
        RenderTexture.ReleaseTemporaryRT(forwardBuffer);

        // Reset bound framebuffer if any is bound
        Graphics.Device.UnbindFramebuffer();
        Graphics.Device.Viewport(0, 0, (uint)Window.InternalWindow.FramebufferSize.X, (uint)Window.InternalWindow.FramebufferSize.Y);
    }

    private static HashSet<int> CullRenderables(IReadOnlyList<IRenderable> renderables, Frustrum? worldFrustum, LayerMask cullingMask)
    {
        HashSet<int> culledRenderableIndices = [];
        for (int renderIndex = 0; renderIndex < renderables.Count; renderIndex++)
        {
            IRenderable renderable = renderables[renderIndex];

            //if (worldFrustum != null && CullRenderable(renderable, worldFrustum))
            //{
            //    culledRenderableIndices.Add(renderIndex);
            //    continue;
            //}

            if (cullingMask.HasLayer(renderable.GetLayer()) == false)
            {
                culledRenderableIndices.Add(renderIndex);
                continue;
            }
        }
        return culledRenderableIndices;
    }

    private static void SetupLightingAndShadows(CameraSnapshot css, IReadOnlyList<IRenderableLight> lights, IReadOnlyList<IRenderable> renderables)
    {
        ShadowAtlas.TryInitialize();
        ShadowAtlas.Clear();

        CreateLightBuffer(css.CameraPosition, css.CullingMask, lights, renderables);

        if (s_shadowMap.IsValid())
            PropertyState.SetGlobalTexture("_ShadowAtlas", s_shadowMap.InternalDepth);
        //PropertyState.SetGlobalBuffer("_Lights", LightBuffer, 0);
        //PropertyState.SetGlobalInt("_LightCount", LightCount);
        GlobalUniforms.SetShadowAtlasSize(new Double2(ShadowAtlas.GetSize(), ShadowAtlas.GetSize()));
    }

    // Reusable arrays to avoid allocations per frame
    private static (IRenderableLight light, double distanceSq)[] s_tempSpotLights = new (IRenderableLight, double)[32];
    private static (IRenderableLight light, double distanceSq)[] s_tempPointLights = new (IRenderableLight, double)[32];
    private static int s_tempSpotCount = 0;
    private static int s_tempPointCount = 0;

    private static void CreateLightBuffer(Double3 cameraPosition, LayerMask cullingMask, IReadOnlyList<IRenderableLight> lights, IReadOnlyList<IRenderable> renderables)
    {
        Graphics.Device.BindFramebuffer(ShadowAtlas.GetAtlas().frameBuffer);
        Graphics.Device.Clear(0.0f, 0.0f, 0.0f, 1.0f, ClearFlags.Depth | ClearFlags.Stencil);

        // We have AtlasWidth slots for shadow maps
        // a single shadow map can consume multiple slots if its larger then 128x128
        // We need to distribute these slots and resolutions out to lights
        // based on their distance from the camera
        int numDirLights = 0;
        int spotLightIndex = 0;
        int pointLightIndex = 0;
        const int MAX_SPOT_LIGHTS = 4;
        const int MAX_POINT_LIGHTS = 4;

        // Reset temp counts
        s_tempSpotCount = 0;
        s_tempPointCount = 0;
        DirectionalLight? closestDirectional = null;
        double closestDirDistSq = double.MaxValue;

        // Single pass: separate by type and calculate squared distances (faster than Distance)
        foreach (IRenderableLight light in lights)
        {
            if (cullingMask.HasLayer(light.GetLayer()) == false)
                continue;

            Double3 toLight = light.GetLightPosition() - cameraPosition;
            double distanceSq = toLight.X * toLight.X + toLight.Y * toLight.Y + toLight.Z * toLight.Z;

            if (light is DirectionalLight dir)
            {
                // Keep only the closest directional light
                if (distanceSq < closestDirDistSq)
                {
                    closestDirectional = dir;
                    closestDirDistSq = distanceSq;
                }
            }
            else if (light is SpotLight)
            {
                // Grow array if needed
                if (s_tempSpotCount >= s_tempSpotLights.Length)
                    Array.Resize(ref s_tempSpotLights, s_tempSpotLights.Length * 2);

                s_tempSpotLights[s_tempSpotCount++] = (light, distanceSq);
            }
            else if (light is PointLight)
            {
                // Grow array if needed
                if (s_tempPointCount >= s_tempPointLights.Length)
                    Array.Resize(ref s_tempPointLights, s_tempPointLights.Length * 2);

                s_tempPointLights[s_tempPointCount++] = (light, distanceSq);
            }
        }

        // Partial sort: only sort enough to get the N closest lights
        // This is O(n log k) instead of O(n log n) where k = MAX_LIGHTS
        PartialSort(s_tempSpotLights, s_tempSpotCount, MAX_SPOT_LIGHTS);
        PartialSort(s_tempPointLights, s_tempPointCount, MAX_POINT_LIGHTS);

        // Process directional light first
        if (closestDirectional.IsValid())
        {
            ProcessLight(closestDirectional, Math.Sqrt(closestDirDistSq), cameraPosition, renderables, ref numDirLights, ref spotLightIndex, ref pointLightIndex, MAX_SPOT_LIGHTS, MAX_POINT_LIGHTS);
        }

        // Process closest spot lights
        int spotLightsToProcess = Math.Min(s_tempSpotCount, MAX_SPOT_LIGHTS);
        for (int i = 0; i < spotLightsToProcess; i++)
        {
            (IRenderableLight light, double distanceSq) = s_tempSpotLights[i];
            ProcessLight(light, Math.Sqrt(distanceSq), cameraPosition, renderables, ref numDirLights, ref spotLightIndex, ref pointLightIndex, MAX_SPOT_LIGHTS, MAX_POINT_LIGHTS);
        }

        // Process closest point lights
        int pointLightsToProcess = Math.Min(s_tempPointCount, MAX_POINT_LIGHTS);
        for (int i = 0; i < pointLightsToProcess; i++)
        {
            (IRenderableLight light, double distanceSq) = s_tempPointLights[i];
            ProcessLight(light, Math.Sqrt(distanceSq), cameraPosition, renderables, ref numDirLights, ref spotLightIndex, ref pointLightIndex, MAX_SPOT_LIGHTS, MAX_POINT_LIGHTS);
        }

        // Set the light counts in global uniforms
        GlobalUniforms.SetSpotLightCount(spotLightIndex);
        GlobalUniforms.SetPointLightCount(pointLightIndex);
        GlobalUniforms.Upload();
    }

    // Partial sort: only sorts the first 'k' elements, much faster when k << n
    private static void PartialSort((IRenderableLight light, double distanceSq)[] array, int count, int k)
    {
        if (count <= 1 || k <= 0) return;

        k = Math.Min(k, count);

        // Use selection for small k, which is optimal for partial sorting
        for (int i = 0; i < k; i++)
        {
            int minIndex = i;
            double minDist = array[i].distanceSq;

            // Find minimum in remaining elements
            for (int j = i + 1; j < count; j++)
            {
                if (array[j].distanceSq < minDist)
                {
                    minDist = array[j].distanceSq;
                    minIndex = j;
                }
            }

            // Swap if needed
            if (minIndex != i)
            {
                (array[minIndex], array[i]) = (array[i], array[minIndex]);
            }
        }
    }

    private static void ProcessLight(IRenderableLight light, double distance, Double3 cameraPosition, IReadOnlyList<IRenderable> renderables,
        ref int numDirLights, ref int spotLightIndex, ref int pointLightIndex, int MAX_SPOT_LIGHTS, int MAX_POINT_LIGHTS)
    {
        // Calculate resolution based on distance (already calculated)
        int res = CalculateResolution(distance);
        if (light is DirectionalLight dir)
            res = (int)dir.ShadowResolution;

        if (light.DoCastShadows())
        {
            // Find a slot for the shadow map
            Int2? slot;
            bool isPointLight = light is PointLight;

            // Point lights need a 2x3 grid for cubemap faces
            if (isPointLight)
                slot = ShadowAtlas.ReserveCubemapTiles(res, light.GetLightID());
            else
                slot = ShadowAtlas.ReserveTiles(res, res, light.GetLightID());

            int AtlasX, AtlasY, AtlasWidth;

            if (slot != null)
            {
                AtlasX = slot.Value.X;
                AtlasY = slot.Value.Y;
                AtlasWidth = res;

                // Draw the shadow map
                s_shadowMap = ShadowAtlas.GetAtlas();

                // For point lights, render 6 faces
                if (isPointLight && light is PointLight pointLight)
                {
                    // Set point light uniforms for shadow rendering
                    PropertyState.SetGlobalVector("_PointLightPosition", pointLight.Transform.Position);
                    PropertyState.SetGlobalFloat("_PointLightRange", pointLight.Range);
                    PropertyState.SetGlobalFloat("_PointLightShadowBias", pointLight.ShadowBias);

                    for (int face = 0; face < 6; face++)
                    {
                        // Calculate viewport for this face in the 2x3 grid
                        int col = face % 2;
                        int row = face / 2;
                        int viewportX = AtlasX + (col * res);
                        int viewportY = AtlasY + (row * res);

                        Graphics.Device.Viewport(viewportX, viewportY, (uint)res, (uint)res);

                        pointLight.GetShadowMatrixForFace(face, out Double4x4 view, out Double4x4 proj, out Double3 forward, out Double3 up);
                        Double3 right = Double3.Cross(forward, up);
                        if (CAMERA_RELATIVE)
                            view.Translation *= new Double4(0, 0, 0, 1); // set all to 0 except W

                        Frustrum frustum = Frustrum.FromMatrix(proj * view);

                        HashSet<int> culledRenderableIndices = [];// CullRenderables(renderables, frustum);
                        AssignCameraMatrices(view, proj);
                        DrawRenderables(renderables, "LightMode", "ShadowCaster", new ViewerData(light.GetLightPosition(), forward, right, up), culledRenderableIndices, false);
                    }

                    // Reset uniforms for non-point lights
                    PropertyState.SetGlobalFloat("_PointLightRange", -1.0f);
                }
                else
                {
                    Double3 forward = ((MonoBehaviour)light).Transform.Forward;
                    if (light is DirectionalLight)
                        forward = -forward; // directional light is inverted atm
                    Double3 right = ((MonoBehaviour)light).Transform.Right;
                    Double3 up = ((MonoBehaviour)light).Transform.Up;

                    // Regular directional/spot light rendering
                    // Set range to -1 to indicate this is not a point light
                    PropertyState.SetGlobalFloat("_PointLightRange", -1.0f);

                    Graphics.Device.Viewport(slot.Value.X, slot.Value.Y, (uint)res, (uint)res);

                    // Use camera-following shadow matrix for directional lights
                    Double4x4 view, proj;
                    if (light is DirectionalLight dirLight)
                        dirLight.GetShadowMatrix(cameraPosition, res, out view, out proj);
                    else
                        light.GetShadowMatrix(out view, out proj);

                    if (CAMERA_RELATIVE)
                        view.Translation *= new Double4(0, 0, 0, 1); // set all to 0 except W

                    Frustrum frustum = Frustrum.FromMatrix(proj * view);

                    HashSet<int> culledRenderableIndices = [];// CullRenderables(renderables, frustum);
                    AssignCameraMatrices(view, proj);
                    DrawRenderables(renderables, "LightMode", "ShadowCaster", new ViewerData(light.GetLightPosition(), forward, right, up), culledRenderableIndices, false);
                }
            }
            else
            {
                AtlasX = -1;
                AtlasY = -1;
                AtlasWidth = 0;
            }


            if (light is DirectionalLight dirLight2)
            {
                dirLight2.UploadToGPU(CAMERA_RELATIVE, cameraPosition, AtlasX, AtlasY, AtlasWidth);
            }
            else if (light is SpotLight spotLight && spotLightIndex < MAX_SPOT_LIGHTS)
            {
                spotLight.UploadToGPU(CAMERA_RELATIVE, cameraPosition, AtlasX, AtlasY, AtlasWidth, spotLightIndex);
                spotLightIndex++;
            }
            else if (light is PointLight pointLight && pointLightIndex < MAX_POINT_LIGHTS)
            {
                pointLight.UploadToGPU(CAMERA_RELATIVE, cameraPosition, AtlasX, AtlasY, AtlasWidth, pointLightIndex);
                pointLightIndex++;
            }
        }
        else
        {
            if (light is DirectionalLight dirL)
            {
                dirL.UploadToGPU(CAMERA_RELATIVE, cameraPosition, -1, -1, 0);
            }
            else if (light is SpotLight spotLight && spotLightIndex < MAX_SPOT_LIGHTS)
            {
                spotLight.UploadToGPU(CAMERA_RELATIVE, cameraPosition, -1, -1, 0, spotLightIndex);
                spotLightIndex++;
            }
            else if (light is PointLight pointLight && pointLightIndex < MAX_POINT_LIGHTS)
            {
                pointLight.UploadToGPU(CAMERA_RELATIVE, cameraPosition, -1, -1, 0, pointLightIndex);
                pointLightIndex++;
            }
        }

        // Set the light counts in global uniforms
        GlobalUniforms.SetSpotLightCount(spotLightIndex);
        GlobalUniforms.SetPointLightCount(pointLightIndex);
        GlobalUniforms.Upload();
    }

    private static int CalculateResolution(double distance)
    {
        double t = Maths.Clamp(distance / 48f, 0, 1);
        int minSize = ShadowAtlas.GetMinShadowSize();
        int maxSize = ShadowAtlas.GetMaxShadowSize();
        int resolution = Maths.RoundToInt(Maths.Lerp(maxSize, minSize, t));

        // Clamp to valid range
        return Maths.Clamp(resolution, minSize, maxSize);
    }

    private static Double3 GetSunDirection(IReadOnlyList<IRenderableLight> lights)
    {
        if (lights.Count > 0 && lights[0] is IRenderableLight light && light.GetLightType() == LightType.Directional)
            return light.GetLightDirection();
        return Double3.UnitY;
    }

    private static void RenderSkybox(CameraSnapshot css)
    {
        s_skybox.SetMatrix("prowl_MatVP", css.Projection * css.OriginView);
        Graphics.DrawMeshNow(s_skyDome, s_skybox);
    }

    private static void RenderGizmos(CameraSnapshot css)
    {
        Double4x4 vp = css.Projection * css.View;
        (Mesh? wire, Mesh? solid) = Debug.GetGizmoDrawData(CAMERA_RELATIVE, css.CameraPosition);

        if (wire.IsValid() || solid.IsValid())
        {
            // The vertices have already been transformed by the gizmo system to be camera relative (if needed) so we just need to draw them
            s_gizmo.SetMatrix("prowl_MatVP", vp);
            if (wire.IsValid()) Graphics.DrawMeshNow(wire, s_gizmo);
            if (solid.IsValid()) Graphics.DrawMeshNow(solid, s_gizmo);
        }

        //List<GizmoBuilder.IconDrawCall> icons = Debug.GetGizmoIcons();
        //if (icons != null)
        //{
        //    buffer.SetMaterial(s_gizmo);
        //
        //    foreach (GizmoBuilder.IconDrawCall icon in icons)
        //    {
        //        Vector3 center = icon.center;
        //        if (CAMERA_RELATIVE)
        //            center -= css.cameraPosition;
        //        Matrix4x4 billboard = Matrix4x4.CreateBillboard(center, Vector3.zero, css.cameraUp, css.cameraForward);
        //
        //        buffer.SetMatrix("_Matrix_VP", (billboard * vp).ToFloat());
        //        buffer.SetTexture("_MainTex", icon.texture);
        //
        //        buffer.DrawSingle(s_quadMesh);
        //    }
        //}
    }

    #endregion

    private static void DrawImageEffects(RenderTexture forwardBuffer, List<ImageEffect> effects, ref bool isHDR)
    {
        // Early exit if no effects to process
        if (effects == null || effects.Count == 0)
            return;

        // Create two buffers for ping-pong rendering
        RenderTexture sourceBuffer = forwardBuffer;

        // Determine if we need to start in LDR mode
        bool firstEffectIsLDR = effects.Count > 0 && effects[0].TransformsToLDR;
        TextureImageFormat destFormat = isHDR && !firstEffectIsLDR ? TextureImageFormat.Float4 : TextureImageFormat.Color4b;

        // Create destination buffer
        RenderTexture destBuffer = RenderTexture.GetTemporaryRT(
            forwardBuffer.Width,
            forwardBuffer.Height,
            false,
            [destFormat]
        );

        // Update HDR flag if needed
        if (firstEffectIsLDR)
        {
            isHDR = false;
        }

        // Keep track of temporary render textures that need cleanup
        List<RenderTexture> tempTextures = [destBuffer];

        try
        {
            // Process each effect
            for (int i = 0; i < effects.Count; i++)
            {
                ImageEffect effect = effects[i];

                // Handle HDR to LDR transition
                if (isHDR && effect.TransformsToLDR)
                {
                    isHDR = false;

                    // If destination buffer is HDR, we need to replace it with LDR
                    if (destBuffer != forwardBuffer)
                    {
                        RenderTexture.ReleaseTemporaryRT(destBuffer);
                        tempTextures.Remove(destBuffer);
                    }

                    // Create new LDR destination buffer
                    destBuffer = RenderTexture.GetTemporaryRT(
                        forwardBuffer.Width,
                        forwardBuffer.Height,
                        false,
                        [TextureImageFormat.Color4b]
                    );

                    if (destBuffer != forwardBuffer)
                    {
                        tempTextures.Add(destBuffer);
                    }
                }

                // Apply the effect
                effect.OnRenderImage(sourceBuffer, destBuffer);

                // Swap buffers for next iteration
                (destBuffer, sourceBuffer) = (sourceBuffer, destBuffer);

                // Update temp texture tracking after swap
                // sourceBuffer now contains the result, destBuffer is the old source
                if (sourceBuffer != forwardBuffer && !tempTextures.Contains(sourceBuffer))
                {
                    tempTextures.Add(sourceBuffer);
                }
                if (destBuffer == forwardBuffer)
                {
                    tempTextures.Remove(destBuffer);
                }
            }

            // After all effects, copy result back to forwardBuffer if needed
            if (sourceBuffer != forwardBuffer)
            {
                Graphics.Device.BindFramebuffer(sourceBuffer.frameBuffer, FBOTarget.Read);
                Graphics.Device.BindFramebuffer(forwardBuffer.frameBuffer, FBOTarget.Draw);
                Graphics.Device.BlitFramebuffer(
                    0, 0, sourceBuffer.Width, sourceBuffer.Height,
                    0, 0, forwardBuffer.Width, forwardBuffer.Height,
                    ClearFlags.Color, BlitFilter.Nearest
                );
            }
        }
        catch (Exception ex)
        {
            // Re-throw the exception after cleanup
            throw new Exception($"Error in DrawImageEffects: {ex.Message}", ex);
        }
        finally
        {
            // Clean up all temporary render textures
            foreach (RenderTexture tempRT in tempTextures)
            {
                if (tempRT != forwardBuffer)
                {
                    RenderTexture.ReleaseTemporaryRT(tempRT);
                }
            }
        }
    }

    private static void DrawRenderables(IReadOnlyList<IRenderable> renderables, string tag, string tagValue, ViewerData viewer, HashSet<int> culledRenderableIndices, bool updatePreviousMatrices)
    {
        bool hasRenderOrder = !string.IsNullOrWhiteSpace(tag);
        for (int renderIndex = 0; renderIndex < renderables.Count; renderIndex++)
        {
            if (culledRenderableIndices?.Contains(renderIndex) ?? false)
                continue;

            IRenderable renderable = renderables[renderIndex];

            Material material = renderable.GetMaterial();
            if (material.Shader.IsNotValid()) continue;

            int passIndex = -1;
            foreach (ShaderPass pass in material.Shader.Passes)
            {
                passIndex++;

                // Skip this pass if it doesn't have the expected tag
                if (hasRenderOrder && !pass.HasTag(tag, tagValue))
                    continue;

                renderable.GetRenderingData(viewer, out PropertyState properties, out Mesh mesh, out Double4x4 model);

                // Store previous model matrix mainly for motion vectors, however, the user can use it for other things
                int instanceId = properties.GetInt("_ObjectID");
                if (updatePreviousMatrices && instanceId != 0)
                    TrackModelMatrix(instanceId, model);

                if (CAMERA_RELATIVE)
                    model.Translation -= new Double4(viewer.Position, 0.0);

                // Set per-object uniforms (these change every draw call)
                PropertyState.SetGlobalMatrix("prowl_ObjectToWorld", model);
                PropertyState.SetGlobalMatrix("prowl_WorldToObject", model.Invert());
                PropertyState.SetGlobalInt("_ObjectID", instanceId);

                PropertyState.SetGlobalColor("_MainColor", Color.White);

                material._properties.ApplyOverride(properties);

                Graphics.DrawMeshNow(mesh, material, passIndex);
            }
        }
    }

    private static bool CullRenderable(IRenderable renderable, Frustrum cameraFrustum)
    {
        renderable.GetCullingData(out bool isRenderable, out AABB bounds);

        return !isRenderable || !cameraFrustum.Intersects(bounds);
    }
}
