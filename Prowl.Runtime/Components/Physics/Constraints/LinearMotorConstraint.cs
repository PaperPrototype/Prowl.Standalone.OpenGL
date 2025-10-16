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
/// A motor constraint that drives relative translational movement along two axes fixed in the reference frames of the bodies.
/// Useful for creating powered sliders and linear actuators.
/// </summary>
public class LinearMotorConstraint : PhysicsConstraint
{
    [SerializeField] private Double3 axis1 = Double3.UnitX;
    [SerializeField] private Double3 axis2 = Double3.UnitX;
    [SerializeField] private double targetVelocity = 0.0;
    [SerializeField] private double maximumForce = 0.0;

    private LinearMotor constraint;

    /// <summary>
    /// The motor axis in local space of the first rigidbody.
    /// </summary>
    public Double3 Axis1
    {
        get => axis1;
        set
        {
            axis1 = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// The motor axis in local space of the second rigidbody.
    /// If no connected body is specified, this is in world space.
    /// </summary>
    public Double3 Axis2
    {
        get => axis2;
        set
        {
            axis2 = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// Target velocity for the motor. Positive values move along the axis direction.
    /// </summary>
    public double TargetVelocity
    {
        get => targetVelocity;
        set
        {
            targetVelocity = value;
            if (constraint != null) constraint.TargetVelocity = value;
        }
    }

    /// <summary>
    /// Maximum force the motor can apply. Set to 0 to disable the motor.
    /// </summary>
    public double MaximumForce
    {
        get => maximumForce;
        set
        {
            maximumForce = value;
            if (constraint != null) constraint.MaximumForce = value;
        }
    }

    /// <summary>
    /// Gets the accumulated impulse applied by this constraint.
    /// </summary>
    public double Impulse => constraint?.Impulse ?? 0.0;

    protected override Constraint GetConstraint() => constraint;

    protected override void CreateConstraint(World world, RigidBody body1, RigidBody body2)
    {
        JVector worldAxis1 = LocalDirToWorld(axis1, Body1.Transform);
        JVector worldAxis2 = connectedBody != null
            ? LocalDirToWorld(axis2, connectedBody.Transform)
            : new JVector(axis2.X, axis2.Y, axis2.Z);

        constraint = world.CreateConstraint<LinearMotor>(body1, body2);
        constraint.Initialize(worldAxis1, worldAxis2);

        constraint.TargetVelocity = targetVelocity;
        constraint.MaximumForce = maximumForce;
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
