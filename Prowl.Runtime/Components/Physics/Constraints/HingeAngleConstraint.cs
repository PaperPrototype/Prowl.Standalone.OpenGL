// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;

using Prowl.Echo;
using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// Constrains two bodies to only allow rotation around a specified axis, removing two angular degrees of freedom.
/// A hinge joint that can optionally enforce angle limits.
/// </summary>
public class HingeAngleConstraint : PhysicsConstraint
{
    [SerializeField] private Double3 hingeAxis = Double3.UnitY;
    [SerializeField] private double minAngle = -180.0;
    [SerializeField] private double maxAngle = 180.0;
    [SerializeField] private double softness = 0.001;
    [SerializeField] private double limitSoftness = 0.001;
    [SerializeField] private double biasFactor = 0.2;
    [SerializeField] private double limitBias = 0.1;

    private HingeAngle constraint;

    /// <summary>
    /// The hinge axis in local space of this rigidbody.
    /// </summary>
    public Double3 HingeAxis
    {
        get => hingeAxis;
        set
        {
            hingeAxis = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// Minimum angle in degrees. Default is -180.
    /// </summary>
    public double MinAngle
    {
        get => minAngle;
        set
        {
            minAngle = value;
            UpdateLimits();
        }
    }

    /// <summary>
    /// Maximum angle in degrees. Default is 180.
    /// </summary>
    public double MaxAngle
    {
        get => maxAngle;
        set
        {
            maxAngle = value;
            UpdateLimits();
        }
    }

    /// <summary>
    /// Softness of the constraint. Higher values make the constraint softer.
    /// </summary>
    public double Softness
    {
        get => softness;
        set
        {
            softness = value;
            if (constraint != null) constraint.Softness = value;
        }
    }

    /// <summary>
    /// Softness of the angle limit. Higher values make the limit softer.
    /// </summary>
    public double LimitSoftness
    {
        get => limitSoftness;
        set
        {
            limitSoftness = value;
            if (constraint != null) constraint.LimitSoftness = value;
        }
    }

    /// <summary>
    /// Bias factor for error correction. Higher values correct errors faster.
    /// </summary>
    public double BiasFactor
    {
        get => biasFactor;
        set
        {
            biasFactor = value;
            if (constraint != null) constraint.Bias = value;
        }
    }

    /// <summary>
    /// Bias factor for limit error correction.
    /// </summary>
    public double LimitBias
    {
        get => limitBias;
        set
        {
            limitBias = value;
            if (constraint != null) constraint.LimitBias = value;
        }
    }

    /// <summary>
    /// Gets the current hinge angle in degrees.
    /// </summary>
    public double Angle
    {
        get
        {
            if (constraint == null) return 0.0;
            return constraint.Angle.Degree;
        }
    }

    /// <summary>
    /// Gets the accumulated impulse applied by this constraint.
    /// </summary>
    public Double3 Impulse
    {
        get
        {
            if (constraint == null) return Double3.Zero;
            Jitter2.LinearMath.JVector impulse = constraint.Impulse;
            return new Double3(impulse.X, impulse.Y, impulse.Z);
        }
    }

    protected override Constraint GetConstraint() => constraint;

    protected override void CreateConstraint(World world, RigidBody body1, RigidBody body2)
    {
        Jitter2.LinearMath.JVector worldAxis = LocalDirToWorld(hingeAxis, Body1.Transform);

        constraint = world.CreateConstraint<HingeAngle>(body1, body2);

        var limit = AngularLimit.FromDegree(minAngle, maxAngle);
        constraint.Initialize(worldAxis, limit);

        constraint.Softness = softness;
        constraint.LimitSoftness = limitSoftness;
        constraint.Bias = biasFactor;
        constraint.LimitBias = limitBias;
    }

    protected override void DestroyConstraint()
    {
        if (constraint != null && !constraint.Handle.IsZero)
        {
            Body1._body.World.Remove(constraint);
            constraint = null;
        }
    }

    private void UpdateLimits()
    {
        if (constraint != null)
        {
            var limit = AngularLimit.FromDegree(minAngle, maxAngle);
            constraint.Limit = limit;
        }
    }
}
