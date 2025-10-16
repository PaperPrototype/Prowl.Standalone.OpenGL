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
    public Double3 position
    {
        get
        {
            if (parent != null)
                return MakeSafe(Double4x4.TransformPoint(m_LocalPosition, parent.localToWorldMatrix));
            else
                return MakeSafe(m_LocalPosition);
        }
        set
        {
            Double3 newPosition = value;
            if (parent != null)
                newPosition = parent.InverseTransformPoint(newPosition);

            if (!m_LocalPosition.Equals(newPosition))
            {
                m_LocalPosition = MakeSafe(newPosition);
                _version++;
            }
        }
    }

    public Double3 localPosition
    {
        get => MakeSafe(m_LocalPosition);
        set
        {
            if (!m_LocalPosition.Equals(value))
            {
                m_LocalPosition = MakeSafe(value);
                _version++;
            }
        }
    }
    #endregion

    #region Rotation
    public Quaternion rotation
    {
        get
        {
            Quaternion worldRot = m_LocalRotation;
            Transform p = parent;
            while (p != null)
            {
                worldRot = p.m_LocalRotation * worldRot;
                p = p.parent;
            }
            return MakeSafe(worldRot);
        }
        set
        {
            var newVale = Quaternion.Identity;
            if (parent != null)
                newVale = MakeSafe(Quaternion.NormalizeSafe(Quaternion.Inverse(parent.rotation) * value));
            else
                newVale = MakeSafe(Quaternion.NormalizeSafe(value));
            if(localRotation != newVale)
            {
                localRotation = newVale;
            }
        }
    }

    public Quaternion localRotation
    {
        get => MakeSafe(m_LocalRotation);
        set
        {
            if (m_LocalRotation != value)
            {
                m_LocalRotation = MakeSafe(value);
                _version++;
            }
        }
    }

    public Double3 eulerAngles
    {
        get => MakeSafe(rotation.EulerAngles);
        set
        {
            rotation = MakeSafe(Quaternion.FromEuler(value));
        }
    }

    public Double3 localEulerAngles
    {
        get => MakeSafe(m_LocalRotation.EulerAngles);
        set
        {
            m_LocalRotation = MakeSafe(Quaternion.FromEuler(value));
            _version++;
        }
    }
    #endregion

    #region Scale

    public Double3 localScale
    {
        get => MakeSafe(m_LocalScale);
        set
        {
            if (!m_LocalScale.Equals(value))
            {
                m_LocalScale = MakeSafe(value);
                _version++;
            }
        }
    }

    public Double3 lossyScale
    {
        get
        {
            Double3 scale = localScale;
            Transform p = parent;
            while (p != null)
            {
                scale = p.localScale * scale;
                p = p.parent;
            }
            return MakeSafe(scale);
        }
    }

    #endregion

    public Double3 right { get => rotation * Double3.UnitX; }     // TODO: Setter
    public Double3 up { get => rotation * Double3.UnitY; }           // TODO: Setter
    public Double3 forward { get => rotation * Double3.UnitZ; } // TODO: Setter

    public Double4x4 worldToLocalMatrix => localToWorldMatrix.Invert();

    public Double4x4 localToWorldMatrix
    {
        get
        {
            Double4x4 t = Double4x4.CreateTRS(m_LocalPosition, m_LocalRotation, m_LocalScale);
            return parent != null ? (parent.localToWorldMatrix * t) : t;
        }
    }

    public Transform parent
    {
        get => gameObject?.parent?.Transform;
        set => gameObject?.SetParent(value?.gameObject, true);
    }

    // https://forum.unity.com/threads/transform-haschanged-would-be-better-if-replaced-by-a-version-number.700004/
    // Replacement for hasChanged
    public uint version
    {
        get => _version;
        set => _version = value;
    }

    public Transform root => parent == null ? this : parent.root;


    #endregion

    #region Fields

    [SerializeField] Double3 m_LocalPosition;
    [SerializeField] Double3 m_LocalScale = Double3.One;
    [SerializeField] Quaternion m_LocalRotation = Quaternion.Identity;

    [SerializeIgnore]
    uint _version = 1;

    public GameObject gameObject { get; internal set; }
    #endregion

    public void SetLocalTransform(Double3 position, Quaternion rotation, Double3 scale)
    {
        m_LocalPosition = position;
        m_LocalRotation = rotation;
        m_LocalScale = scale;
        _version++;
    }

    private double MakeSafe(double v) => double.IsNaN(v) ? 0 : v;
    private Double3 MakeSafe(Double3 v) => new Double3(MakeSafe(v.X), MakeSafe(v.Y), MakeSafe(v.Z));
    private Quaternion MakeSafe(Quaternion v) => new Quaternion(MakeSafe(v.X), MakeSafe(v.Y), MakeSafe(v.Z), MakeSafe(v.W));

    public Transform? Find(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        var names = path.Split('/');
        var currentTransform = this;

        foreach (var name in names)
        {
            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

            var childTransform = FindImmediateChild(currentTransform, name);
            if (childTransform == null)
                return null;

            currentTransform = childTransform;
        }

        return currentTransform;
    }

    private Transform? FindImmediateChild(Transform parent, string name)
    {
        foreach (var child in parent.gameObject.children)
            if (child.Name == name)
                return child.Transform;
        return null;
    }

    public Transform? DeepFind(string name)
    {
        if (name == null) return null;
        if (name == gameObject.Name) return this;
        foreach (var child in gameObject.children)
        {
            var t = child.Transform.DeepFind(name);
            if (t != null) return t;
        }
        return null;
    }

    public static string GetPath(Transform target, Transform root)
    {
        string path = target.gameObject.Name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.gameObject.Name + "/" + path;
            if (target == root)
                break;
        }
        return path;
    }

    public void Translate(Double3 translation, Transform? relativeTo = null)
    {
        if (relativeTo != null)
            position += relativeTo.TransformDirection(translation);
        else
            position += translation;
    }

    public void Rotate(Double3 eulerAngles, bool relativeToSelf = true)
    {
        Quaternion eulerRot = Quaternion.FromEuler(eulerAngles);
        if (relativeToSelf)
            localRotation = localRotation * eulerRot;
        else
            rotation = rotation * (Quaternion.Inverse(rotation) * eulerRot * rotation);
    }

    public void Rotate(Double3 axis, double angle, bool relativeToSelf = true)
    {
        RotateAroundInternal(relativeToSelf ? TransformDirection(axis) : axis, angle * Maths.Deg2Rad);
    }

    public void RotateAround(Double3 point, Double3 axis, double angle)
    {
        Double3 worldPos = position;
        Quaternion q = Quaternion.AxisAngle(axis, angle);
        Double3 dif = worldPos - point;
        dif = q * dif;
        worldPos = point + dif;
        position = worldPos;
        RotateAroundInternal(axis, angle * Maths.Deg2Rad);
    }

    internal void RotateAroundInternal(Double3 worldAxis, double rad)
    {
        Double3 localAxis = InverseTransformDirection(worldAxis);
        if (Double3.LengthSquared(localAxis) > double.Epsilon)
        {
            localAxis = Double3.Normalize(localAxis);
            Quaternion q = Quaternion.AxisAngle(localAxis, rad);
            m_LocalRotation = Quaternion.NormalizeSafe(m_LocalRotation * q);
        }
    }


    #region Transform

    public Double3 TransformPoint(Double3 inPosition) => Double4x4.TransformPoint(new Double4(inPosition, 1.0), localToWorldMatrix).XYZ;
    public Double3 InverseTransformPoint(Double3 inPosition) => Double4x4.TransformPoint(new Double4(inPosition, 1.0), worldToLocalMatrix).XYZ;
    public Quaternion InverseTransformRotation(Quaternion worldRotation) => Quaternion.Inverse(rotation) * worldRotation;

    public Double3 TransformDirection(Double3 inDirection) => rotation * inDirection;
    public Double3 InverseTransformDirection(Double3 inDirection) => Quaternion.Inverse(rotation) * inDirection;

    public Double3 TransformVector(Double3 inVector)
    {
        Double3 worldVector = inVector;

        Transform cur = this;
        while (cur != null)
        {
            worldVector = worldVector * cur.m_LocalScale;
            worldVector = cur.m_LocalRotation * worldVector;

            cur = cur.parent;
        }
        return worldVector;
    }
    public Double3 InverseTransformVector(Double3 inVector)
    {
        Double3 newVector, localVector;
        if (parent != null)
            localVector = parent.InverseTransformVector(inVector);
        else
            localVector = inVector;

        newVector = Quaternion.Inverse(m_LocalRotation) * localVector;
        if (!m_LocalScale.Equals(Double3.One))
            newVector = newVector * InverseSafe(m_LocalScale);

        return newVector;
    }

    public Quaternion TransformRotation(Quaternion inRotation)
    {
        Quaternion worldRotation = inRotation;

        Transform cur = this;
        while (cur != null)
        {
            worldRotation = cur.m_LocalRotation * worldRotation;
            cur = cur.parent;
        }
        return worldRotation;
    }

    #endregion

    public Double4x4 GetWorldRotationAndScale()
    {
        Double4x4 ret = Double4x4.CreateTRS(new Double3(0, 0, 0), m_LocalRotation, m_LocalScale);
        if (parent != null)
        {
            Double4x4 parentTransform = parent.GetWorldRotationAndScale();
            ret = (parentTransform * ret);
        }
        return ret;
    }

    static double InverseSafe(double f) => Maths.Abs(f) > double.Epsilon ? 1.0F / f : 0.0F;
    static Double3 InverseSafe(Double3 v) => new Double3(InverseSafe(v.X), InverseSafe(v.Y), InverseSafe(v.Z));
}
