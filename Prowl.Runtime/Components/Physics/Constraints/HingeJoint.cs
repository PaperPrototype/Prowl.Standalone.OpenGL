// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;

using Prowl.Echo;
using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// A hinge joint that constrains two bodies to rotate around a shared axis.
/// Similar to a door hinge. Composed of HingeAngle and BallSocket constraints.
/// </summary>
public class HingeJoint : PhysicsJoint
{
    [SerializeField] private Double3 anchor = Double3.Zero;
    [SerializeField] private Double3 axis = Double3.UnitY;
    [SerializeField] private double minAngleDegrees = -180.0;
    [SerializeField] private double maxAngleDegrees = 180.0;
    [SerializeField] private bool hasMotor = false;
    [SerializeField] private double motorTargetVelocity = 0.0;
    [SerializeField] private double motorMaxForce = 100.0;

    private Jitter2.Dynamics.Constraints.HingeJoint hingeJoint;

    /// <summary>
    /// The anchor point in local space where the joint connects.
    /// </summary>
    public Double3 Anchor
    {
        get => anchor;
        set
        {
            anchor = value;
            RecreateConstraint();
        }
    }

    /// <summary>
    /// The axis of rotation in local space.
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
    /// Minimum angle limit in degrees.
    /// </summary>
    public double MinAngleDegrees
    {
        get => minAngleDegrees;
        set
        {
            minAngleDegrees = value;
            UpdateAngleLimits();
        }
    }

    /// <summary>
    /// Maximum angle limit in degrees.
    /// </summary>
    public double MaxAngleDegrees
    {
        get => maxAngleDegrees;
        set
        {
            maxAngleDegrees = value;
            UpdateAngleLimits();
        }
    }

    /// <summary>
    /// Whether this joint has a motor attached.
    /// </summary>
    public bool HasMotor
    {
        get => hasMotor;
        set
        {
            if (hasMotor != value)
            {
                hasMotor = value;
                RecreateConstraint();
            }
        }
    }

    /// <summary>
    /// Target velocity for the motor (if enabled).
    /// </summary>
    public double MotorTargetVelocity
    {
        get => motorTargetVelocity;
        set
        {
            motorTargetVelocity = value;
            if (hingeJoint?.Motor != null)
                hingeJoint.Motor.TargetVelocity = value;
        }
    }

    /// <summary>
    /// Maximum force the motor can apply (if enabled).
    /// </summary>
    public double MotorMaxForce
    {
        get => motorMaxForce;
        set
        {
            motorMaxForce = value;
            if (hingeJoint?.Motor != null)
                hingeJoint.Motor.MaximumForce = value;
        }
    }

    /// <summary>
    /// Gets the current angle of the hinge in degrees.
    /// </summary>
    public double CurrentAngleDegrees
    {
        get
        {
            if (hingeJoint?.HingeAngle == null) return 0.0;
            return (double)hingeJoint.HingeAngle.Angle * (180.0 / System.Math.PI);
        }
    }

    protected override void CreateConstraint(World world, RigidBody body1, RigidBody body2)
    {
        Jitter2.LinearMath.JVector worldAnchor = LocalToWorld(anchor, Body1.Transform);
        Jitter2.LinearMath.JVector worldAxis = LocalDirToWorld(axis, Body1.Transform);

        var angleLimit = AngularLimit.FromDegree(minAngleDegrees, maxAngleDegrees);

        hingeJoint = new Jitter2.Dynamics.Constraints.HingeJoint(
            world, body1, body2, worldAnchor, worldAxis, angleLimit, hasMotor);

        joint = hingeJoint;

        if (hasMotor && hingeJoint.Motor != null)
        {
            hingeJoint.Motor.TargetVelocity = motorTargetVelocity;
            hingeJoint.Motor.MaximumForce = motorMaxForce;
        }
    }

    protected override void DestroyConstraint()
    {
        hingeJoint = null;
        base.DestroyConstraint();
    }

    private void UpdateAngleLimits()
    {
        if (hingeJoint?.HingeAngle != null)
        {
            var angleLimit = AngularLimit.FromDegree(minAngleDegrees, maxAngleDegrees);
            hingeJoint.HingeAngle.Limit = angleLimit;
        }
    }
}
