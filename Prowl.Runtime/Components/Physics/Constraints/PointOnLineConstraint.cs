// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;

using Prowl.Echo;
using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// Constrains a fixed point on one body to a line that is fixed on another body.
/// This constraint removes one or two degrees of translational freedom depending on the limit.
/// Useful for creating slider joints with rails.
/// </summary>
public class PointOnLineConstraint : PhysicsConstraint
{
    [SerializeField] private Double3 lineAxis = Double3.UnitX;
    [SerializeField] private Double3 anchor1 = Double3.Zero;
    [SerializeField] private Double3 anchor2 = Double3.Zero;
    [SerializeField] private double minDistance = double.NegativeInfinity;
    [SerializeField] private double maxDistance = double.PositiveInfinity;
    [SerializeField] private double softness = 0.00001;
    [SerializeField] private double limitSoftness = 0.0001;
    [SerializeField] private double biasFactor = 0.01;
    [SerializeField] private double limitBias = 0.2;

    private PointOnLine constraint;

    /// <summary>
    /// The line axis in local space of the first rigidbody.
    /// </summary>
    public Double3 LineAxis
    {
        get => lineAxis;
        set
        {
            lineAxis = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// Anchor point on the first body that defines the line.
    /// </summary>
    public Double3 Anchor1
    {
        get => anchor1;
        set
        {
            anchor1 = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// Anchor point on the second body that is constrained to the line.
    /// </summary>
    public Double3 Anchor2
    {
        get => anchor2;
        set
        {
            anchor2 = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// Minimum allowed distance along the line axis. Use double.NegativeInfinity for no minimum.
    /// </summary>
    public double MinDistance
    {
        get => minDistance;
        set
        {
            minDistance = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// Maximum allowed distance along the line axis. Use double.PositiveInfinity for no maximum.
    /// </summary>
    public double MaxDistance
    {
        get => maxDistance;
        set
        {
            maxDistance = value;
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
    /// Softness of the distance limit. Higher values make the limit softer.
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
    /// Gets the current distance along the line axis.
    /// </summary>
    public double Distance => constraint?.Distance ?? 0.0;

    /// <summary>
    /// Gets the accumulated impulse applied by this constraint.
    /// </summary>
    public Double3 Impulse
    {
        get
        {
            if (constraint == null) return Double3.Zero;
            var impulse = constraint.Impulse;
            return new Double3(impulse.X, impulse.Y, impulse.Z);
        }
    }

    protected override Constraint GetConstraint() => constraint;

    protected override void CreateConstraint(World world, RigidBody body1, RigidBody body2)
    {
        var worldAxis = LocalDirToWorld(lineAxis, Body1.Transform);
        var worldAnchor1 = LocalToWorld(anchor1, Body1.Transform);
        var worldAnchor2 = connectedBody != null
            ? LocalToWorld(anchor2, connectedBody.Transform)
            : new JVector(anchor2.X, anchor2.Y, anchor2.Z);

        constraint = world.CreateConstraint<PointOnLine>(body1, body2);

        var limit = new LinearLimit(minDistance, maxDistance);
        constraint.Initialize(worldAxis, worldAnchor1, worldAnchor2, limit);

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
}
