// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Vector;

namespace Prowl.Runtime;

/// <summary>
/// A character controller that handles collision detection and movement.
/// provides just the core functionality.
/// </summary>
public class CharacterController : MonoBehaviour
{
    /// <summary>
    /// The shape type used for the character controller collision detection.
    /// </summary>
    public enum ColliderShape
    {
        Capsule,
        Cylinder
    }

    public ColliderShape Shape = ColliderShape.Cylinder;
    public double Radius = 0.5;
    public double Height = 1.8;
    public double SkinWidth = 0.02;

    /// <summary>
    /// Maximum angle in degrees for a surface to be considered walkable (default: 45 degrees)
    /// </summary>
    public double MaxSlopeAngle = 55.0;

    /// <summary>
    /// Distance to snap down to ground when walking off slopes (default: 0.5)
    /// </summary>
    public double SnapDownDistance = 0.15;

    /// <summary>
    /// Whether the character controller is currently grounded.
    /// </summary>
    public bool IsGrounded { get; private set; }

    private ShapeCastHit lastGroundHit;
    private Double3 lastVelocity;

    /// <summary>
    /// Moves the character controller by the specified motion vector.
    /// This handles collision detection and sliding.
    /// Also updates the IsGrounded state.
    /// </summary>
    public void Move(Double3 motion)
    {
        Double3 position = GameObject.Transform.position;
        lastVelocity = motion;

        // Update grounded state before moving
        UpdateGroundedState(position);

        // Perform movement with collision
        Double3 finalPosition = CollideAndSlide(position, motion, 0);

        // Snap down to ground if moving horizontally on slopes
        if (IsGrounded && motion.Y <= 0)
        {
            finalPosition = SnapToGround(finalPosition);
        }

        GameObject.Transform.position = finalPosition;
    }

    /// <summary>
    /// Updates the grounded state by performing a ground check.
    /// </summary>
    private void UpdateGroundedState(Double3 position)
    {
        double groundCheckDistance = 0.1;
        IsGrounded = PerformGroundCheck(position, groundCheckDistance, out lastGroundHit);
    }

    /// <summary>
    /// Performs a ground check using shape casting.
    /// Only considers the character grounded if the surface angle is walkable.
    /// </summary>
    private bool PerformGroundCheck(Double3 position, double distance, out ShapeCastHit hitInfo)
    {
        bool hit = PerformShapeCast(position, new Double3(0, -1, 0), distance, out hitInfo);

        if (!hit)
            return false;

        // Check if the surface is walkable
        double slopeAngle = GetSlopeAngle(hitInfo.normal);
        return slopeAngle <= MaxSlopeAngle;
    }

    /// <summary>
    /// Attempts to set the height of the collider.
    /// Returns true if successful, false if the new size would collide with something.
    /// </summary>
    public bool TrySetHeight(double newHeight)
    {
        double minHeight = Shape == ColliderShape.Capsule ? Radius * 2 : 0.1;
        if (newHeight <= minHeight)
            return false;

        Double3 position = GameObject.Transform.position;
        bool wouldCollide = CheckShapeOverlap(position, newHeight, Radius);

        if (!wouldCollide)
        {
            Height = newHeight;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to set the radius of the collider.
    /// Returns true if successful, false if the new size would collide with something.
    /// </summary>
    public bool TrySetRadius(double newRadius)
    {
        if (newRadius <= 0)
            return false;

        if (Shape == ColliderShape.Capsule && newRadius * 2 >= Height)
            return false;

        Double3 position = GameObject.Transform.position;
        bool wouldCollide = CheckShapeOverlap(position, Height, newRadius);

        if (!wouldCollide)
        {
            Radius = newRadius;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a shape with the given dimensions would overlap with anything.
    /// </summary>
    private bool CheckShapeOverlap(Double3 position, double height, double radius)
    {
        double effectiveRadius = radius - SkinWidth;

        if (Shape == ColliderShape.Capsule)
        {
            Double3 bottom = position + new Double3(0, radius, 0);
            Double3 top = position + new Double3(0, height - radius, 0);
            return GameObject.Scene.Physics.CheckCapsule(bottom, top, effectiveRadius);
        }
        else // Cylinder
        {
            Double3 center = position + new Double3(0, height * 0.5, 0);
            return GameObject.Scene.Physics.CheckCylinder(center, effectiveRadius, height, Quaternion.Identity);
        }
    }

    private Double3 GetShapeCenter(Double3 position)
    {
        if (Shape == ColliderShape.Capsule)
            return position + new Double3(0, Height * 0.5, 0);
        else // Cylinder
            return position + new Double3(0, Height * 0.5, 0);
    }

    private Double3 GetCapsuleBottom(Double3 position)
    {
        return position + new Double3(0, Radius, 0);
    }

    private Double3 GetCapsuleTop(Double3 position)
    {
        return position + new Double3(0, Height - Radius, 0);
    }

    private double GetEffectiveRadius()
    {
        return Radius - SkinWidth;
    }

    /// <summary>
    /// Performs a shape cast based on the current shape type.
    /// </summary>
    private bool PerformShapeCast(Double3 position, Double3 direction, double distance, out ShapeCastHit hitInfo)
    {
        if (Shape == ColliderShape.Capsule)
        {
            return GameObject.Scene.Physics.CapsuleCast(
                GetCapsuleBottom(position),
                GetCapsuleTop(position),
                GetEffectiveRadius(),
                direction,
                distance,
                out hitInfo
            );
        }
        else // Cylinder
        {
            return GameObject.Scene.Physics.CylinderCast(
                GetShapeCenter(position),
                GetEffectiveRadius(),
                Height,
                Quaternion.Identity,
                direction,
                distance,
                out hitInfo
            );
        }
    }

    private Double3 CollideAndSlide(Double3 position, Double3 velocity, int depth)
    {
        const int MaxDepth = 5;
        if (depth >= MaxDepth)
            return position;

        double moveDistance = Double3.Length(velocity);
        if (moveDistance < 0.0001)
            return position;

        Double3 moveDirection = Double3.Normalize(velocity);

        bool hit = PerformShapeCast(
            position,
            moveDirection,
            moveDistance + SkinWidth,
            out var hitInfo
        );

        if (hit)
        {
            // Move to safe distance from hit point
            double safeDistance = (moveDistance * hitInfo.fraction - SkinWidth);
            position += moveDirection * safeDistance;

            // Calculate remaining movement after hitting surface
            double remainingDistance = moveDistance - safeDistance;
            Double3 remainingMove = moveDirection * remainingDistance;

            // Project remaining movement onto the hit surface (slide)
            Double3 slideMove = ProjectOntoSurface(remainingMove, hitInfo.normal);

            // Check if the surface is too steep to walk on
            double slopeAngle = GetSlopeAngle(hitInfo.normal);
            if (slopeAngle > MaxSlopeAngle)
            {
                // For steep slopes, remove vertical component if moving up
                if (slideMove.Y > 0)
                {
                    slideMove.Y = 0;
                }
            }

            // Prevent jittering by checking if slide move is very small
            if (Double3.Length(slideMove) < 0.0001)
                return position;

            // Recurse with remaining slide movement
            return CollideAndSlide(position, slideMove, depth + 1);
        }
        else
        {
            position += velocity;
        }

        return position;
    }

    private Double3 ProjectOntoSurface(Double3 movement, Double3 surfaceNormal)
    {
        // Project remaining movement onto the hit surface
        return Double3.ProjectOntoPlane(movement, surfaceNormal);
    }

    /// <summary>
    /// Calculates the angle of a surface in degrees from horizontal.
    /// </summary>
    private double GetSlopeAngle(Double3 normal)
    {
        // Angle between surface normal and up vector
        return System.Math.Acos(normal.Y) * (180.0 / System.Math.PI);
    }

    /// <summary>
    /// Snaps the character down to the ground when walking on slopes.
    /// This prevents the character from "floating" when transitioning between slopes.
    /// </summary>
    private Double3 SnapToGround(Double3 position)
    {
        // Only snap if we have horizontal velocity
        double horizontalSpeed = System.Math.Sqrt(lastVelocity.X * lastVelocity.X + lastVelocity.Z * lastVelocity.Z);
        if (horizontalSpeed < 0.0001)
            return position;

        // Check if there's ground below us within snap distance
        bool hit = PerformShapeCast(
            position,
            new Double3(0, -1, 0),
            SnapDownDistance,
            out var hitInfo
        );

        if (hit)
        {
            // Check if the surface is walkable
            double slopeAngle = GetSlopeAngle(hitInfo.normal);
            if (slopeAngle <= MaxSlopeAngle)
            {
                // Snap down to the surface
                double snapDistance = hitInfo.fraction * SnapDownDistance - SkinWidth;
                if (snapDistance > 0)
                {
                    position.Y -= snapDistance;
                }
            }
        }

        return position;
    }

    public override void DrawGizmos()
    {
        if (GameObject.Scene.Physics == null) return;

        Double3 position = GameObject.Transform.position;

        if (Shape == ColliderShape.Capsule)
        {
            Debug.DrawWireCapsule(GetCapsuleBottom(position), GetCapsuleTop(position), Radius, Color.Cyan, 16);
        }
        else // Cylinder
        {
            Debug.DrawWireCylinder(GetShapeCenter(position), Quaternion.Identity, Radius, Height, Color.Cyan, 16);
        }

        // Draw ground hit if grounded
        if (lastGroundHit.hit)
        {
            lastGroundHit.DrawGizmos();
        }
    }
}
