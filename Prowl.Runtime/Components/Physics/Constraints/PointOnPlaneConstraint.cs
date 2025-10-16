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
/// Constrains a fixed point on one body to a plane that is fixed on another body.
/// This constraint removes one degree of translational freedom if the limit is enforced.
/// Useful for creating sliding surfaces or limiting movement to a plane.
/// </summary>
public class PointOnPlaneConstraint : PhysicsConstraint
{
    [SerializeField] private Double3 planeNormal = Double3.UnitY;
    [SerializeField] private Double3 anchor1 = Double3.Zero;
    [SerializeField] private Double3 anchor2 = Double3.Zero;
    [SerializeField] private double minDistance = double.NegativeInfinity;
    [SerializeField] private double maxDistance = double.PositiveInfinity;
    [SerializeField] private double softness = 0.00001;
    [SerializeField] private double biasFactor = 0.01;

    private PointOnPlane constraint;

    /// <summary>
    /// The plane normal in local space of the first rigidbody.
    /// </summary>
    public Double3 PlaneNormal
    {
        get => planeNormal;
        set
        {
            planeNormal = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// Anchor point on the first body that defines the plane position.
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
    /// Anchor point on the second body that is constrained to the plane.
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
    /// Minimum allowed distance from the plane. Use double.NegativeInfinity for no minimum.
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
    /// Maximum allowed distance from the plane. Use double.PositiveInfinity for no maximum.
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
    /// Gets the accumulated impulse applied by this constraint.
    /// </summary>
    public double Impulse => constraint?.Impulse ?? 0.0;

    protected override Constraint GetConstraint() => constraint;

    protected override void CreateConstraint(World world, RigidBody body1, RigidBody body2)
    {
        JVector worldNormal = LocalDirToWorld(planeNormal, Body1.Transform);
        JVector worldAnchor1 = LocalToWorld(anchor1, Body1.Transform);
        JVector worldAnchor2 = connectedBody != null
            ? LocalToWorld(anchor2, connectedBody.Transform)
            : new JVector(anchor2.X, anchor2.Y, anchor2.Z);

        constraint = world.CreateConstraint<PointOnPlane>(body1, body2);

        var limit = new LinearLimit(minDistance, maxDistance);
        constraint.Initialize(worldNormal, worldAnchor1, worldAnchor2, limit);

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
