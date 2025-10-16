// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Prowl.Echo;
using Prowl.Runtime;

namespace Prowl.Vector;

public class Transform
{
    #region Properties

    #region Position
    public Double3 Position
    {
        get
        {
            if (Parent != null)
                return MakeSafe(Double4x4.TransformPoint(_localPosition, Parent.LocalToWorldMatrix));
            else
                return MakeSafe(_localPosition);
        }
        set
        {
            Double3 newPosition = value;
            if (Parent != null)
                newPosition = Parent.InverseTransformPoint(newPosition);

            if (!_localPosition.Equals(newPosition))
            {
                _localPosition = MakeSafe(newPosition);
                _version++;
            }
        }
    }

    public Double3 LocalPosition
    {
        get => MakeSafe(_localPosition);
        set
        {
            if (!_localPosition.Equals(value))
            {
                _localPosition = MakeSafe(value);
                _version++;
            }
        }
    }
    #endregion

    #region Rotation
    public Quaternion Rotation
    {
        get
        {
            Quaternion worldRot = _localRotation;
            Transform p = Parent;
            while (p != null)
            {
                worldRot = p._localRotation * worldRot;
                p = p.Parent;
            }
            return MakeSafe(worldRot);
        }
        set
        {
            Quaternion newVale;
            if (Parent != null)
                newVale = MakeSafe(Quaternion.NormalizeSafe(Quaternion.Inverse(Parent.Rotation) * value));
            else
                newVale = MakeSafe(Quaternion.NormalizeSafe(value));

            if (LocalRotation != newVale)
            {
                LocalRotation = newVale;
            }
        }
    }

    public Quaternion LocalRotation
    {
        get => MakeSafe(_localRotation);
        set
        {
            if (_localRotation != value)
            {
                _localRotation = MakeSafe(value);
                _version++;
            }
        }
    }

    public Double3 EulerAngles
    {
        get => MakeSafe(Rotation.EulerAngles);
        set
        {
            Rotation = MakeSafe(Quaternion.FromEuler(value));
        }
    }

    public Double3 LocalEulerAngles
    {
        get => MakeSafe(_localRotation.EulerAngles);
        set
        {
            _localRotation = MakeSafe(Quaternion.FromEuler(value));
            _version++;
        }
    }
    #endregion

    #region Scale

    public Double3 LocalScale
    {
        get => MakeSafe(_localScale);
        set
        {
            if (!_localScale.Equals(value))
            {
                _localScale = MakeSafe(value);
                _version++;
            }
        }
    }

    public Double3 LossyScale
    {
        get
        {
            Double3 scale = LocalScale;
            Transform p = Parent;
            while (p != null)
            {
                scale = p.LocalScale * scale;
                p = p.Parent;
            }
            return MakeSafe(scale);
        }
    }

    #endregion

    public Double3 Right { get => Rotation * Double3.UnitX; }     // TODO: Setter
    public Double3 Up { get => Rotation * Double3.UnitY; }           // TODO: Setter
    public Double3 Forward { get => Rotation * Double3.UnitZ; } // TODO: Setter

    public Double4x4 WorldToLocalMatrix => LocalToWorldMatrix.Invert();

    public Double4x4 LocalToWorldMatrix
    {
        get
        {
            Double4x4 t = Double4x4.CreateTRS(_localPosition, _localRotation, _localScale);
            return Parent != null ? (Parent.LocalToWorldMatrix * t) : t;
        }
    }

    public Transform Parent
    {
        get => GameObject?.Parent?.Transform;
        set => GameObject?.SetParent(value?.GameObject, true);
    }

    // https://forum.unity.com/threads/transform-haschanged-would-be-better-if-replaced-by-a-version-number.700004/
    // Replacement for hasChanged
    public uint Version
    {
        get => _version;
        set => _version = value;
    }

    public Transform Root => Parent == null ? this : Parent.Root;


    #endregion

    #region Fields

    [SerializeField] Double3 _localPosition;
    [SerializeField] Double3 _localScale = Double3.One;
    [SerializeField] Quaternion _localRotation = Quaternion.Identity;

    [SerializeIgnore]
    uint _version = 1;

    public GameObject GameObject { get; internal set; }
    #endregion

    public void SetLocalTransform(Double3 position, Quaternion rotation, Double3 scale)
    {
        _localPosition = position;
        _localRotation = rotation;
        _localScale = scale;
        _version++;
    }

    private double MakeSafe(double v) => double.IsNaN(v) ? 0 : v;
    private Double3 MakeSafe(Double3 v) => new(MakeSafe(v.X), MakeSafe(v.Y), MakeSafe(v.Z));
    private Quaternion MakeSafe(Quaternion v) => new(MakeSafe(v.X), MakeSafe(v.Y), MakeSafe(v.Z), MakeSafe(v.W));

    public Transform? Find(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        string[] names = path.Split('/');
        Transform currentTransform = this;

        foreach (string name in names)
        {
            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

            Transform? childTransform = FindImmediateChild(currentTransform, name);
            if (childTransform == null)
                return null;

            currentTransform = childTransform;
        }

        return currentTransform;
    }

    private Transform? FindImmediateChild(Transform parent, string name)
    {
        foreach (GameObject child in parent.GameObject.Children)
            if (child.Name == name)
                return child.Transform;
        return null;
    }

    public Transform? DeepFind(string name)
    {
        if (name == null) return null;
        if (name == GameObject.Name) return this;
        foreach (GameObject child in GameObject.Children)
        {
            Transform? t = child.Transform.DeepFind(name);
            if (t != null) return t;
        }
        return null;
    }

    public static string GetPath(Transform target, Transform root)
    {
        string path = target.GameObject.Name;
        while (target.Parent != null)
        {
            target = target.Parent;
            path = target.GameObject.Name + "/" + path;
            if (target == root)
                break;
        }
        return path;
    }

    public void Translate(Double3 translation, Transform? relativeTo = null)
    {
        if (relativeTo != null)
            Position += relativeTo.TransformDirection(translation);
        else
            Position += translation;
    }

    public void Rotate(Double3 eulerAngles, bool relativeToSelf = true)
    {
        Quaternion eulerRot = Quaternion.FromEuler(eulerAngles);
        if (relativeToSelf)
            LocalRotation *= eulerRot;
        else
            Rotation *= (Quaternion.Inverse(Rotation) * eulerRot * Rotation);
    }

    public void Rotate(Double3 axis, double angle, bool relativeToSelf = true)
    {
        RotateAroundInternal(relativeToSelf ? TransformDirection(axis) : axis, angle * Maths.Deg2Rad);
    }

    public void RotateAround(Double3 point, Double3 axis, double angle)
    {
        Double3 worldPos = Position;
        Quaternion q = Quaternion.AxisAngle(axis, angle);
        Double3 dif = worldPos - point;
        dif = q * dif;
        worldPos = point + dif;
        Position = worldPos;
        RotateAroundInternal(axis, angle * Maths.Deg2Rad);
    }

    internal void RotateAroundInternal(Double3 worldAxis, double rad)
    {
        Double3 localAxis = InverseTransformDirection(worldAxis);
        if (Double3.LengthSquared(localAxis) > double.Epsilon)
        {
            localAxis = Double3.Normalize(localAxis);
            Quaternion q = Quaternion.AxisAngle(localAxis, rad);
            _localRotation = Quaternion.NormalizeSafe(_localRotation * q);
        }
    }


    #region Transform

    public Double3 TransformPoint(Double3 inPosition) => Double4x4.TransformPoint(new Double4(inPosition, 1.0), LocalToWorldMatrix).XYZ;
    public Double3 InverseTransformPoint(Double3 inPosition) => Double4x4.TransformPoint(new Double4(inPosition, 1.0), WorldToLocalMatrix).XYZ;
    public Quaternion InverseTransformRotation(Quaternion worldRotation) => Quaternion.Inverse(Rotation) * worldRotation;

    public Double3 TransformDirection(Double3 inDirection) => Rotation * inDirection;
    public Double3 InverseTransformDirection(Double3 inDirection) => Quaternion.Inverse(Rotation) * inDirection;

    public Double3 TransformVector(Double3 inVector)
    {
        Double3 worldVector = inVector;

        Transform cur = this;
        while (cur != null)
        {
            worldVector *= cur._localScale;
            worldVector = cur._localRotation * worldVector;

            cur = cur.Parent;
        }
        return worldVector;
    }
    public Double3 InverseTransformVector(Double3 inVector)
    {
        Double3 newVector, localVector;
        if (Parent != null)
            localVector = Parent.InverseTransformVector(inVector);
        else
            localVector = inVector;

        newVector = Quaternion.Inverse(_localRotation) * localVector;
        if (!_localScale.Equals(Double3.One))
            newVector *= InverseSafe(_localScale);

        return newVector;
    }

    public Quaternion TransformRotation(Quaternion inRotation)
    {
        Quaternion worldRotation = inRotation;

        Transform cur = this;
        while (cur != null)
        {
            worldRotation = cur._localRotation * worldRotation;
            cur = cur.Parent;
        }
        return worldRotation;
    }

    #endregion

    public Double4x4 GetWorldRotationAndScale()
    {
        Double4x4 ret = Double4x4.CreateTRS(new Double3(0, 0, 0), _localRotation, _localScale);
        if (Parent != null)
        {
            Double4x4 parentTransform = Parent.GetWorldRotationAndScale();
            ret = (parentTransform * ret);
        }
        return ret;
    }

    static double InverseSafe(double f) => Maths.Abs(f) > double.Epsilon ? 1.0F / f : 0.0F;
    static Double3 InverseSafe(Double3 v) => new(InverseSafe(v.X), InverseSafe(v.Y), InverseSafe(v.Z));
}
