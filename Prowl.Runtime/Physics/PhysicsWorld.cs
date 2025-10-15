// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;

using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

using Prowl.Vector;

namespace Prowl.Runtime;

public class PhysicsWorld
{
    public static void IgnoreCollisionBetween(Rigidbody3D bodyA, Rigidbody3D bodyB) => LayerFilter.IgnoreCollisionBetween(bodyA, bodyB);

    public static void EnableCollisionBetween(Rigidbody3D bodyA, Rigidbody3D bodyB) => LayerFilter.EnableCollisionBetween(bodyA, bodyB);

    public World World { get; private set; }

    public Double3 Gravity = new Double3(0, -9.81f, 0);
    public int SolverIterations = 8;
    public int RelaxIterations = 4;
    public int Substep = 2;
    public bool AllowSleep = true;
    public bool UseMultithreading = true;
    public bool AutoSyncTransforms = true;

    public PhysicsWorld()
    {
        World = new World();

        World.DynamicTree.Filter = World.DefaultDynamicTreeFilter;
        World.BroadPhaseFilter = new LayerFilter();
        World.NarrowPhaseFilter = new TriangleEdgeCollisionFilter();
    }

    public void Clear()
    {
        World?.Clear();
    }

    public void Update()
    {
        // Configure world settings
        World.AllowDeactivation = AllowSleep;

        World.SubstepCount = Substep;
        World.SolverIterations = (SolverIterations, RelaxIterations);

        World.Gravity = new JVector(Gravity.X, Gravity.Y, Gravity.Z);

        World.Step(Time.fixedDeltaTime, UseMultithreading);
    }

    /// <summary>
    /// Casts a ray against all colliders in this physics world.
    /// </summary>
    public bool Raycast(Double3 origin, Double3 direction)
    {
        direction = Double3.Normalize(direction);
        var jOrigin = new JVector(origin.X, origin.Y, origin.Z);
        var jDirection = new JVector(direction.X, direction.Y, direction.Z);

        return World.DynamicTree.RayCast(jOrigin, jDirection,
            PreFilter, PostFilter,
            out _, out _, out _);
    }

    /// <summary>
    /// Casts a ray against all colliders and returns detailed information about the hit.
    /// </summary>
    public bool Raycast(Double3 origin, Double3 direction, out RaycastHit hitInfo)
    {
        direction = Double3.Normalize(direction);
        var jOrigin = new JVector(origin.X, origin.Y, origin.Z);
        var jDirection = new JVector(direction.X, direction.Y, direction.Z);

        hitInfo = new RaycastHit();
        bool hit = World.DynamicTree.RayCast(jOrigin, jDirection,
            PreFilter, PostFilter,
            out IDynamicTreeProxy shape, out JVector normal, out double lambda);

        if (hit)
        {
            var result = new DynamicTree.RayCastResult
            {
                Entity = shape,
                Lambda = lambda,
                Normal = normal
            };
            hitInfo.SetFromJitterResult(result, origin, direction);
        }

        return hit;
    }

    /// <summary>
    /// Casts a ray within a maximum distance.
    /// </summary>
    public bool Raycast(Double3 origin, Double3 direction, double maxDistance)
    {
        direction = Double3.Normalize(direction);
        var jOrigin = new JVector(origin.X, origin.Y, origin.Z);
        var jDirection = new JVector(direction.X, direction.Y, direction.Z);

        return World.DynamicTree.RayCast(jOrigin, jDirection,
            PreFilter, PostFilter,
            out _, out _, out var dist) && dist <= maxDistance;
    }

    /// <summary>
    /// Casts a ray within a maximum distance and returns detailed information.
    /// </summary>
    public bool Raycast(Double3 origin, Double3 direction, double maxDistance, out RaycastHit hitInfo)
    {
        direction = Double3.Normalize(direction);
        var jOrigin = new JVector(origin.X, origin.Y, origin.Z);
        var jDirection = new JVector(direction.X, direction.Y, direction.Z);

        hitInfo = new RaycastHit();
        bool hit = World.DynamicTree.RayCast(jOrigin, jDirection,
            PreFilter, PostFilter,
            out IDynamicTreeProxy shape, out JVector normal, out double lambda) && lambda <= maxDistance;

        if (hit)
        {
            var result = new DynamicTree.RayCastResult
            {
                Entity = shape,
                Lambda = lambda,
                Normal = normal,
            };
            hitInfo.SetFromJitterResult(result, origin, direction);
        }

        return hit;
    }

    /// <summary>
    /// Casts a ray with layer mask filtering.
    /// </summary>
    public bool Raycast(Double3 origin, Double3 direction, double maxDistance, LayerMask layerMask)
    {
        direction = Double3.Normalize(direction);
        var jOrigin = new JVector(origin.X, origin.Y, origin.Z);
        var jDirection = new JVector(direction.X, direction.Y, direction.Z);

        return World.DynamicTree.RayCast(jOrigin, jDirection,
            shape => PreFilterWithLayer(shape, layerMask), PostFilter,
            out _, out _, out double lambda) && lambda <= maxDistance;
    }

    /// <summary>
    /// Casts a ray with layer mask filtering and returns detailed information.
    /// </summary>
    public bool Raycast(Double3 origin, Double3 direction, out RaycastHit hitInfo, double maxDistance, LayerMask layerMask)
    {
        direction = Double3.Normalize(direction);
        var jOrigin = new JVector(origin.X, origin.Y, origin.Z);
        var jDirection = new JVector(direction.X, direction.Y, direction.Z);

        hitInfo = new RaycastHit();
        bool hit = World.DynamicTree.RayCast(jOrigin, jDirection,
            shape => PreFilterWithLayer(shape, layerMask), PostFilter,
            out IDynamicTreeProxy shape, out JVector normal, out double lambda) && lambda <= maxDistance;

        if (hit)
        {
            var result = new DynamicTree.RayCastResult
            {
                Entity = shape,
                Lambda = lambda,
                Normal = normal,
            };
            hitInfo.SetFromJitterResult(result, origin, direction);
        }

        return hit;
    }

    private static bool PreFilter(IDynamicTreeProxy proxy)
    {
        return true;
    }

    private static bool PreFilterWithLayer(IDynamicTreeProxy proxy, LayerMask layerMask)
    {
        if (proxy is RigidBodyShape shape)
        {
            if (!PreFilter(proxy)) return false;

            var userData = shape.RigidBody.Tag as Rigidbody3D.RigidBodyUserData;

            return layerMask.HasLayer(userData.Layer);
        }

        return false;
    }

    private static bool PostFilter(DynamicTree.RayCastResult result)
    {
        return true;
    }

    #region Shape Casting

    /// <summary>
    /// Casts a sphere along a direction and returns the first hit.
    /// </summary>
    /// <param name="origin">Starting position of the sphere center.</param>
    /// <param name="radius">Radius of the sphere.</param>
    /// <param name="direction">Direction to cast the sphere.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hitInfo">Information about what was hit.</param>
    /// <returns>True if the sphere hit something.</returns>
    public bool SphereCast(Double3 origin, double radius, Double3 direction, double maxDistance, out ShapeCastHit hitInfo)
    {
        return SphereCast(origin, radius, direction, maxDistance, out hitInfo, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a sphere along a direction with layer filtering.
    /// </summary>
    public bool SphereCast(Double3 origin, double radius, Double3 direction, double maxDistance, out ShapeCastHit hitInfo, LayerMask layerMask)
    {
        direction = Double3.Normalize(direction);

        var sphere = new SphereShape(radius);
        var jOrigin = new JVector(origin.X, origin.Y, origin.Z);
        var jDirection = new JVector(direction.X, direction.Y, direction.Z);
        var sweep = jDirection * maxDistance;

        hitInfo = new ShapeCastHit();

        // Query all potential shapes in the sweep path
        ShapeCastHit closestHit = new ShapeCastHit { fraction = double.MaxValue };
        bool foundHit = false;

        // Get all shapes from the dynamic tree that could potentially be hit
        var potentialShapes = new System.Collections.Generic.List<IDynamicTreeProxy>();

        // Create a bounding box that encompasses the entire sweep
        JBoundingBox sweepBox = new JBoundingBox();
        sphere.CalculateBoundingBox(JQuaternion.Identity, jOrigin, out var startBox);
        sphere.CalculateBoundingBox(JQuaternion.Identity, jOrigin + sweep, out var endBox);

        sweepBox.Min = JVector.Min(startBox.Min, endBox.Min);
        sweepBox.Max = JVector.Max(startBox.Max, endBox.Max);

        World.DynamicTree.Query(potentialShapes, in sweepBox);

        foreach (var proxy in potentialShapes)
        {
            if (proxy is not RigidBodyShape targetShape) continue;

            // Check layer mask
            var userData = targetShape.RigidBody.Tag as Rigidbody3D.RigidBodyUserData;
            if (!layerMask.HasLayer(userData.Layer)) continue;

            var targetBody = targetShape.RigidBody;

            // Perform sweep test
            bool hit = NarrowPhase.Sweep(
                sphere, targetShape,
                JQuaternion.Identity, targetBody.Data.Orientation,
                jOrigin, targetBody.Data.Position,
                sweep, JVector.Zero,
                out JVector pointA, out JVector pointB, out JVector normal, out double lambda);

            if (hit && lambda >= 0 && lambda <= 1.0 && lambda < closestHit.fraction)
            {
                closestHit.hit = true;
                closestHit.fraction = lambda;
                closestHit.normal = -(new Double3(normal.X, normal.Y, normal.Z));
                closestHit.point = new Double3(pointA.X, pointA.Y, pointA.Z);
                closestHit.hitPoint = new Double3(pointB.X, pointB.Y, pointB.Z);
                closestHit.rigidbody = userData.Rigidbody;
                closestHit.shape = targetShape;
                closestHit.transform = userData.Rigidbody?.GameObject?.Transform;
                foundHit = true;
            }
        }

        hitInfo = closestHit;
        return foundHit;
    }

    /// <summary>
    /// Casts a capsule along a direction and returns the first hit.
    /// </summary>
    /// <param name="point1">Start point of the capsule's line segment.</param>
    /// <param name="point2">End point of the capsule's line segment.</param>
    /// <param name="radius">Radius of the capsule.</param>
    /// <param name="direction">Direction to cast the capsule.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hitInfo">Information about what was hit.</param>
    /// <returns>True if the capsule hit something.</returns>
    public bool CapsuleCast(Double3 point1, Double3 point2, double radius, Double3 direction, double maxDistance, out ShapeCastHit hitInfo)
    {
        return CapsuleCast(point1, point2, radius, direction, maxDistance, out hitInfo, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a capsule along a direction with layer filtering.
    /// </summary>
    public bool CapsuleCast(Double3 point1, Double3 point2, double radius, Double3 direction, double maxDistance, out ShapeCastHit hitInfo, LayerMask layerMask)
    {
        direction = Double3.Normalize(direction);

        // Calculate capsule properties
        Double3 capsuleCenter = (point1 + point2) * 0.5;
        Double3 capsuleAxis = point2 - point1;
        double capsuleLength = Double3.Length(capsuleAxis);

        // Create a capsule shape (aligned along Y-axis)
        var capsule = new CapsuleShape(radius, capsuleLength);

        // Calculate rotation to align capsule with the segment
        var jOrigin = new JVector(capsuleCenter.X, capsuleCenter.Y, capsuleCenter.Z);
        var jDirection = new JVector(direction.X, direction.Y, direction.Z);
        var sweep = jDirection * maxDistance;

        // Calculate capsule orientation
        JQuaternion capsuleOrientation;
        if (capsuleLength > 1e-6)
        {
            Double3 normalizedAxis = capsuleAxis / capsuleLength;
            Double3 yAxis = new Double3(0, 1, 0);

            // If axis is aligned with Y, no rotation needed
            if (System.Math.Abs(Double3.Dot(normalizedAxis, yAxis) - 1.0) < 1e-6)
            {
                capsuleOrientation = JQuaternion.Identity;
            }
            else if (System.Math.Abs(Double3.Dot(normalizedAxis, yAxis) + 1.0) < 1e-6)
            {
                // Axis is opposite to Y, rotate 180 degrees around X
                capsuleOrientation = JQuaternion.CreateFromAxisAngle(new JVector(1, 0, 0), System.Math.PI);
            }
            else
            {
                // Calculate rotation from Y-axis to the capsule axis
                Double3 rotAxis = Double3.Cross(yAxis, normalizedAxis);
                rotAxis = Double3.Normalize(rotAxis);
                double angle = System.Math.Acos(Double3.Dot(yAxis, normalizedAxis));
                capsuleOrientation = JQuaternion.CreateFromAxisAngle(
                    new JVector(rotAxis.X, rotAxis.Y, rotAxis.Z), angle);
            }
        }
        else
        {
            capsuleOrientation = JQuaternion.Identity;
        }

        hitInfo = new ShapeCastHit();

        ShapeCastHit closestHit = new ShapeCastHit { fraction = double.MaxValue };
        bool foundHit = false;

        // Get all shapes from the dynamic tree that could potentially be hit
        var potentialShapes = new System.Collections.Generic.List<IDynamicTreeProxy>();

        // Create a bounding box that encompasses the entire sweep
        JBoundingBox sweepBox = new JBoundingBox();
        capsule.CalculateBoundingBox(capsuleOrientation, jOrigin, out var startBox);
        capsule.CalculateBoundingBox(capsuleOrientation, jOrigin + sweep, out var endBox);

        sweepBox.Min = JVector.Min(startBox.Min, endBox.Min);
        sweepBox.Max = JVector.Max(startBox.Max, endBox.Max);

        World.DynamicTree.Query(potentialShapes, in sweepBox);

        foreach (var proxy in potentialShapes)
        {
            if (proxy is not RigidBodyShape targetShape) continue;

            // Check layer mask
            var userData = targetShape.RigidBody.Tag as Rigidbody3D.RigidBodyUserData;
            if (!layerMask.HasLayer(userData.Layer)) continue;

            var targetBody = targetShape.RigidBody;

            // Perform sweep test
            bool hit = NarrowPhase.Sweep(
                capsule, targetShape,
                capsuleOrientation, targetBody.Data.Orientation,
                jOrigin, targetBody.Data.Position,
                sweep, JVector.Zero,
                out JVector pointA, out JVector pointB, out JVector normal, out double lambda);

            if (hit && lambda >= 0 && lambda <= 1.0 && lambda < closestHit.fraction)
            {
                closestHit.hit = true;
                closestHit.fraction = lambda;
                closestHit.normal = -(new Double3(normal.X, normal.Y, normal.Z));
                closestHit.point = new Double3(pointA.X, pointA.Y, pointA.Z);
                closestHit.hitPoint = new Double3(pointB.X, pointB.Y, pointB.Z);
                closestHit.rigidbody = userData.Rigidbody;
                closestHit.shape = targetShape;
                closestHit.transform = userData.Rigidbody?.GameObject?.Transform;
                foundHit = true;
            }
        }

        hitInfo = closestHit;
        return foundHit;
    }

    #endregion
}
