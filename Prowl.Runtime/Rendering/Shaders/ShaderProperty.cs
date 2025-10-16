﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Echo;
using Prowl.Vector;

using Texture2D = Prowl.Runtime.Resources.Texture2D;

namespace Prowl.Runtime.Rendering.Shaders;

public enum ShaderPropertyType { Float, Vector2, Vector3, Vector4, Color, Matrix, Texture2D }

public class ShaderProperty
{
    public string Name;
    public string DisplayName;

    [field: SerializeField]
    public ShaderPropertyType PropertyType { get; private set; }

    public Double4 Value;
    public Double4x4 MatrixValue;

    public Texture2D Texture2DValue;

    public ShaderProperty() { }

    public void Set(ShaderProperty other)
    {
        if (other.PropertyType != PropertyType)
            return;

        Value = other.Value;
        MatrixValue = other.MatrixValue;
        Texture2DValue = other.Texture2DValue;
    }

    public ShaderProperty(double value)
    {
        Value = new(value);
        PropertyType = ShaderPropertyType.Float;
    }

    public static implicit operator ShaderProperty(double value)
        => new(value);

    public static implicit operator double(ShaderProperty value)
        => value.Value.X;

    public ShaderProperty(Double2 value)
    {
        Value = new(value, 0, 0);
        PropertyType = ShaderPropertyType.Vector2;
    }

    public static implicit operator ShaderProperty(Double2 value)
        => new(value);

    public static implicit operator Double2(ShaderProperty value)
        => new(value.Value.X, value.Value.Y);

    public ShaderProperty(Double3 value)
    {
        Value = new(value, 0);
        PropertyType = ShaderPropertyType.Vector3;
    }

    public static implicit operator ShaderProperty(Double3 value)
        => new(value);

    public static implicit operator Double3(ShaderProperty value)
        => new(value.Value.X, value.Value.Y, value.Value.Z);

    public ShaderProperty(Double4 value)
    {
        Value = value;
        PropertyType = ShaderPropertyType.Vector4;
    }

    public static implicit operator ShaderProperty(Double4 value)
        => new(value);

    public static implicit operator Double4(ShaderProperty value)
        => value.Value;

    public ShaderProperty(Color value)
    {
        Value = new(value.R, value.G, value.B, value.A);
        PropertyType = ShaderPropertyType.Color;
    }

    public static implicit operator ShaderProperty(Color value)
        => new(value);

    public static implicit operator Color(ShaderProperty value)
        => new((float)value.Value.X, (float)value.Value.Y, (float)value.Value.Z, (float)value.Value.W);

    public ShaderProperty(Double4x4 value)
    {
        MatrixValue = value;
        PropertyType = ShaderPropertyType.Matrix;
    }

    public static implicit operator ShaderProperty(Double4x4 value)
        => new(value);

    public static implicit operator Double4x4(ShaderProperty value)
        => value.MatrixValue;

    public ShaderProperty(Texture2D value)
    {
        Texture2DValue = value;
        PropertyType = ShaderPropertyType.Texture2D;
    }

    public static implicit operator ShaderProperty(Texture2D value)
        => new(value);

    public static implicit operator Texture2D(ShaderProperty value)
        => value.Texture2DValue;
}
