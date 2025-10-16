﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime.Rendering;
using Prowl.Runtime.Resources;
using Prowl.Vector;
using Prowl.Vector.Geometry;

namespace Prowl.Runtime;

public class MeshRenderer : MonoBehaviour, IRenderable
{
    public Mesh Mesh;
    public Material Material;
    public Color MainColor = Color.White;

    private PropertyState _properties = new();

    public override void Update()
    {
        if (Mesh.IsValid() && Material.IsValid())
        {
            _properties.Clear();
            _properties.SetInt("_ObjectID", InstanceID);
            _properties.SetColor("_MainColor", MainColor);
            GameObject.Scene.PushRenderable(this);
        }
    }

    public Material GetMaterial() => Material;
    public int GetLayer() => GameObject.LayerIndex;

    public void GetRenderingData(ViewerData viewer, out PropertyState properties, out Mesh drawData, out Double4x4 model)
    {
        drawData = Mesh;
        properties = _properties;
        model = Transform.LocalToWorldMatrix;
    }

    public void GetCullingData(out bool isRenderable, out AABB bounds)
    {
        isRenderable = true;
        //bounds = Bounds.CreateFromMinMax(new Vector3(999999), new Vector3(999999));
        bounds = Mesh.bounds.TransformBy(Transform.LocalToWorldMatrix);
    }
}
