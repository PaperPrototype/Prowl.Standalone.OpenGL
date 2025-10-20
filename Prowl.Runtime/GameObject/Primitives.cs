using System.Runtime.CompilerServices;
using Prowl.Runtime.Resources;
using Prowl.Vector;

namespace Prowl.Runtime;

public static class Primitives
{
    private static Material _standardMaterial;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject Cube(string name)
    {
        _standardMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Standard));
        return Cube(name, Double3.Zero, Double3.One, _standardMaterial);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject Cube(Double3 position)
    {
        _standardMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Standard));
        return Cube(position.ToString(), position, Double3.One, _standardMaterial);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject Cube(string name, Double3 position)
    {
        _standardMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Standard));
        return Cube(name, position, Double3.One, _standardMaterial);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject Cube(string name, Double3 position, Double3 scale)
    {
        _standardMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Standard));
        return Cube(name, position, scale, _standardMaterial);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject Cube(string name, Double3 position, Double3 scale, Material material)
    {
        // scaled mesh
        var mesh = Mesh.CreateCube(scale);

        // game object
        var go = new GameObject(name);
        go.Transform.Position = position;

        // visuals
        var ren = go.AddComponent<MeshRenderer>();
        ren.Mesh = mesh;
        ren.Material = material;

        // final game object
        return go;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject PhysicsCube(string name, bool isStatic = false)
    {
        _standardMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Standard));
        return PhysicsCube(name, Double3.Zero, Double3.One, _standardMaterial, isStatic);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject PhysicsCube(Double3 position, bool isStatic = false)
    {
        _standardMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Standard));
        return PhysicsCube(position.ToString(), position, Double3.One, _standardMaterial, isStatic);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject PhysicsCube(string name, Double3 position, bool isStatic = false)
    {
        _standardMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Standard));
        return PhysicsCube(name, position, Double3.One, _standardMaterial, isStatic);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject PhysicsCube(string name, Double3 position, Double3 size, bool isStatic = false)
    {
        _standardMaterial ??= new Material(Shader.LoadDefault(DefaultShader.Standard));
        return PhysicsCube(name, position, size, _standardMaterial, isStatic);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject PhysicsCube(string name, Double3 position, Double3 size, Material material, bool isStatic = false)
    {
        // scaled mesh
        var mesh = Mesh.CreateCube(size);

        // game object
        var go = new GameObject(name);
        go.Transform.Position = position;

        // visuals
        var ren = go.AddComponent<MeshRenderer>();
        ren.Mesh = mesh;
        ren.Material = material;

        // rigidbody
        var rb = go.AddComponent<Rigidbody3D>();
        rb.IsStatic = isStatic;

        // scaled box collider
        var col = go.AddComponent<BoxCollider>();
        col.Size = size;

        // final game object
        return go;
    }
}