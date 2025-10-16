﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime.Resources;
using Prowl.Vector;
using Prowl.Vector.Geometry;

namespace Prowl.Runtime.Rendering;

public struct RenderingData
{
    public bool DisplayGizmo;
    public Double4x4 GridMatrix;
    public Color GridColor;
    public Double3 GridSizes;
}

public interface IRenderable
{
    public Material GetMaterial();
    public int GetLayer();

    public void GetRenderingData(ViewerData viewer, out PropertyState properties, out Mesh drawData, out Double4x4 model);

    public void GetCullingData(out bool isRenderable, out AABB bounds);
}

public enum LightType
{
    Directional,
    Spot,
    Point,
    //Area
}

public interface IRenderableLight
{
    public int GetLightID();
    public int GetLayer();
    public LightType GetLightType();
    public Double3 GetLightPosition();
    public Double3 GetLightDirection();
    public bool DoCastShadows();
    public void GetShadowMatrix(out Double4x4 view, out Double4x4 projection);
}

public abstract class RenderPipeline : EngineObject
{
    public abstract void Render(Camera camera, in RenderingData data);
}
