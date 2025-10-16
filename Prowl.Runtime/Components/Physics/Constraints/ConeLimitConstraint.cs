// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;

using Prowl.Echo;
using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// Restricts the tilt of one body relative to another body within a cone shape.
/// Useful for creating ball-and-socket joints with angular limits (ragdoll joints).
/// </summary>
public class ConeLimitConstraint : PhysicsConstraint
{
    [SerializeField] private Double3 axis = Double3.UnitY;
    [SerializeField] private double minAngle = 0.0;
    [SerializeField] private double maxAngle = 45.0;
    [SerializeField] private double softness = 0.001;
    [SerializeField] private double biasFactor = 0.2;

    private ConeLimit constraint;

    /// <summary>
    /// The cone axis in local space of this rigidbody.
    /// </summary>
    public Double3 Axis
    {
        get => axis;
        set
        {
            axis = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// Minimum cone angle in degrees. Default is 0.
    /// </summary>
    public double MinAngle
    {
        get => minAngle;
        set
        {
            minAngle = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// Maximum cone angle in degrees. Default is 45.
    /// This defines the cone's opening angle from the axis.
    /// </summary>
    public double MaxAngle
    {
        get => maxAngle;
        set
        {
            maxAngle = value;
            RecreateConstraint();
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
    /// Gets the current angle between the axes in degrees.
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
    public double Impulse => constraint?.Impulse ?? 0.0;

    protected override Constraint GetConstraint() => constraint;

    protected override void CreateConstraint(World world, RigidBody body1, RigidBody body2)
    {
        var worldAxis = LocalDirToWorld(axis, Body1.Transform);

        constraint = world.CreateConstraint<ConeLimit>(body1, body2);

        var limit = AngularLimit.FromDegree(minAngle, maxAngle);
        constraint.Initialize(worldAxis, limit);

        constraint.Softness = softness;
        constraint.Bias = biasFactor;
    }

    protected override void DestroyConstraint()
    {
        if (constraint != null && !constraint.Handle.IsZero)
        {
            Body1._body.World.Remove(constraint);
            constraint = null;
        }
    }
}
