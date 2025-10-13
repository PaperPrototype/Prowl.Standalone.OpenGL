// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;

using Prowl.Echo;
using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// Base class for all physics constraints that connect two rigidbodies.
/// </summary>
public abstract class PhysicsConstraint : MonoBehaviour
{
    [SerializeField] protected Rigidbody3D connectedBody;
    [SerializeField] protected bool enabledOnStart = true;

    /// <summary>
    /// The rigidbody connected by this constraint. If null, the constraint connects to the world.
    /// </summary>
    public Rigidbody3D ConnectedBody
    {
        get => connectedBody;
        set
        {
            if (connectedBody != value)
            {
                connectedBody = value;
                RecreateConstraint();
            }
        }
    }

    /// <summary>
    /// The first rigidbody (owner of this component).
    /// </summary>
    protected Rigidbody3D Body1 => GetComponentInParent<Rigidbody3D>();

    /// <summary>
    /// Gets or sets whether this constraint is enabled.
    /// </summary>
    public bool Enabled
    {
        get => GetConstraint()?.IsEnabled ?? false;
        set
        {
            var constraint = GetConstraint();
            if (constraint != null) constraint.IsEnabled = value;
        }
    }

    public override void OnEnable()
    {
        if (GameObject?.Scene == null) return;
        RecreateConstraint();
    }

    public override void OnDisable()
    {
        DestroyConstraint();
    }

    public override void OnValidate()
    {
        if (GameObject?.Scene == null) return;
        RecreateConstraint();
    }

    public override void DrawGizmos()
    {
        var constraint = GetConstraint();
        if (constraint != null && GameObject?.Scene?.Physics?.World != null)
        {
            constraint.DebugDraw(JitterGizmosDrawer.Instance);
        }
    }

    /// <summary>
    /// Gets the underlying Jitter2 constraint.
    /// </summary>
    protected abstract Constraint GetConstraint();

    /// <summary>
    /// Creates the constraint in the physics world.
    /// </summary>
    protected abstract void CreateConstraint(World world, RigidBody body1, RigidBody body2);

    /// <summary>
    /// Destroys the constraint.
    /// </summary>
    protected abstract void DestroyConstraint();

    /// <summary>
    /// Recreates the constraint with current settings.
    /// </summary>
    protected void RecreateConstraint()
    {
        DestroyConstraint();

        var body1 = Body1;
        if (body1 == null || body1._body == null || body1._body.Handle.IsZero)
            return;

        var world = GameObject.Scene.Physics.World;
        if (world == null) return;

        // If no connected body is specified, create a static body at the world origin
        RigidBody body2;
        if (connectedBody == null || connectedBody._body == null || connectedBody._body.Handle.IsZero)
        {
            // Create a temporary static body for world-space constraints
            body2 = world.CreateRigidBody();
            body2.IsStatic = true;
        }
        else
        {
            body2 = connectedBody._body;
        }

        CreateConstraint(world, body1._body, body2);

        // Set initial enabled state
        var constraint = GetConstraint();
        if (constraint != null)
        {
            constraint.IsEnabled = enabledOnStart;
        }
    }

    /// <summary>
    /// Converts a local position to world space.
    /// </summary>
    protected Jitter2.LinearMath.JVector LocalToWorld(Double3 localPos, Transform transform)
    {
        var worldPos = transform.TransformPoint(localPos);
        return new Jitter2.LinearMath.JVector(worldPos.X, worldPos.Y, worldPos.Z);
    }

    /// <summary>
    /// Converts a local direction to world space.
    /// </summary>
    protected Jitter2.LinearMath.JVector LocalDirToWorld(Double3 localDir, Transform transform)
    {
        var worldDir = transform.TransformDirection(localDir);
        return new Jitter2.LinearMath.JVector(worldDir.X, worldDir.Y, worldDir.Z);
    }
}
