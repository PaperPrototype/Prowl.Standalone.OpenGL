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
    /// Generic shape cast that returns all hits along the sweep path.
    /// </summary>
    /// <param name="shape">The shape to cast.</param>
    /// <param name="orientation">The orientation of the casting shape.</param>
    /// <param name="origin">Starting position of the shape.</param>
    /// <param name="direction">Direction to cast the shape.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hits">List to populate with all hits found.</param>
    /// <param name="layerMask">Layer mask for filtering.</param>
    /// <returns>Number of hits found.</returns>
    public int ShapeCastAll(RigidBodyShape shape, JQuaternion orientation, Double3 origin, Double3 direction, double maxDistance, List<ShapeCastHit> hits, LayerMask layerMask)
    {
        direction = Double3.Normalize(direction);

        var jOrigin = new JVector(origin.X, origin.Y, origin.Z);
        var jDirection = new JVector(direction.X, direction.Y, direction.Z);
        var sweep = jDirection * maxDistance;

        hits.Clear();

        // Get all shapes from the dynamic tree that could potentially be hit
        var potentialShapes = new List<IDynamicTreeProxy>();

        // Create a bounding box that encompasses the entire sweep
        JBoundingBox sweepBox = new JBoundingBox();
        shape.CalculateBoundingBox(orientation, jOrigin, out var startBox);
        shape.CalculateBoundingBox(orientation, jOrigin + sweep, out var endBox);

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
                shape, targetShape,
                orientation, targetBody.Data.Orientation,
                jOrigin, targetBody.Data.Position,
                sweep, JVector.Zero,
                out JVector pointA, out JVector pointB, out JVector normal, out double lambda);

            if (hit && lambda >= 0 && lambda <= 1.0)
            {
                var castHit = new ShapeCastHit
                {
                    hit = true,
                    fraction = lambda,
                    normal = -(new Double3(normal.X, normal.Y, normal.Z)),
                    point = new Double3(pointA.X, pointA.Y, pointA.Z),
                    hitPoint = new Double3(pointB.X, pointB.Y, pointB.Z),
                    rigidbody = userData.Rigidbody,
                    shape = targetShape,
                    transform = userData.Rigidbody?.GameObject?.Transform
                };
                hits.Add(castHit);
            }
        }

        return hits.Count;
    }

    /// <summary>
    /// Generic shape cast that returns all hits with default layer mask.
    /// </summary>
    public int ShapeCastAll(RigidBodyShape shape, JQuaternion orientation, Double3 origin, Double3 direction, double maxDistance, List<ShapeCastHit> hits)
    {
        return ShapeCastAll(shape, orientation, origin, direction, maxDistance, hits, LayerMask.Everything);
    }

    /// <summary>
    /// Generic shape cast that returns only the closest hit.
    /// </summary>
    /// <param name="shape">The shape to cast.</param>
    /// <param name="orientation">The orientation of the casting shape.</param>
    /// <param name="origin">Starting position of the shape.</param>
    /// <param name="direction">Direction to cast the shape.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hitInfo">Information about the closest hit.</param>
    /// <param name="layerMask">Layer mask for filtering.</param>
    /// <returns>True if the shape hit something.</returns>
    public bool ShapeCast(RigidBodyShape shape, JQuaternion orientation, Double3 origin, Double3 direction, double maxDistance, out ShapeCastHit hitInfo, LayerMask layerMask)
    {
        var hits = new List<ShapeCastHit>();
        int hitCount = ShapeCastAll(shape, orientation, origin, direction, maxDistance, hits, layerMask);

        if (hitCount > 0)
        {
            // Find closest hit
            ShapeCastHit closest = hits[0];
            for (int i = 1; i < hits.Count; i++)
            {
                if (hits[i].fraction < closest.fraction)
                    closest = hits[i];
            }
            hitInfo = closest;
            return true;
        }

        hitInfo = new ShapeCastHit();
        return false;
    }

    /// <summary>
    /// Generic shape cast that returns only the closest hit with default orientation and layer mask.
    /// </summary>
    public bool ShapeCast(RigidBodyShape shape, Double3 origin, Double3 direction, double maxDistance, out ShapeCastHit hitInfo)
    {
        return ShapeCast(shape, JQuaternion.Identity, origin, direction, maxDistance, out hitInfo, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a sphere along a direction and returns the closest hit.
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
    /// Casts a sphere along a direction with layer filtering and returns the closest hit.
    /// </summary>
    public bool SphereCast(Double3 origin, double radius, Double3 direction, double maxDistance, out ShapeCastHit hitInfo, LayerMask layerMask)
    {
        var sphere = new SphereShape(radius);
        return ShapeCast(sphere, JQuaternion.Identity, origin, direction, maxDistance, out hitInfo, layerMask);
    }

    /// <summary>
    /// Casts a sphere along a direction and returns all hits.
    /// </summary>
    /// <param name="origin">Starting position of the sphere center.</param>
    /// <param name="radius">Radius of the sphere.</param>
    /// <param name="direction">Direction to cast the sphere.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hits">List to populate with all hits found.</param>
    /// <returns>Number of hits found.</returns>
    public int SphereCastAll(Double3 origin, double radius, Double3 direction, double maxDistance, List<ShapeCastHit> hits)
    {
        return SphereCastAll(origin, radius, direction, maxDistance, hits, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a sphere along a direction with layer filtering and returns all hits.
    /// </summary>
    public int SphereCastAll(Double3 origin, double radius, Double3 direction, double maxDistance, List<ShapeCastHit> hits, LayerMask layerMask)
    {
        var sphere = new SphereShape(radius);
        return ShapeCastAll(sphere, JQuaternion.Identity, origin, direction, maxDistance, hits, layerMask);
    }

    /// <summary>
    /// Casts a capsule along a direction and returns the closest hit.
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
    /// Casts a capsule along a direction with layer filtering and returns the closest hit.
    /// </summary>
    public bool CapsuleCast(Double3 point1, Double3 point2, double radius, Double3 direction, double maxDistance, out ShapeCastHit hitInfo, LayerMask layerMask)
    {
        // Calculate capsule properties
        Double3 capsuleCenter = (point1 + point2) * 0.5;
        Double3 capsuleAxis = point2 - point1;
        double capsuleLength = Double3.Length(capsuleAxis);

        // Create a capsule shape (aligned along Y-axis)
        var capsule = new CapsuleShape(radius, capsuleLength);

        // Calculate orientation to align capsule with the segment
        JQuaternion capsuleOrientation = CalculateCapsuleOrientation(capsuleAxis, capsuleLength);

        return ShapeCast(capsule, capsuleOrientation, capsuleCenter, direction, maxDistance, out hitInfo, layerMask);
    }

    /// <summary>
    /// Casts a capsule along a direction and returns all hits.
    /// </summary>
    /// <param name="point1">Start point of the capsule's line segment.</param>
    /// <param name="point2">End point of the capsule's line segment.</param>
    /// <param name="radius">Radius of the capsule.</param>
    /// <param name="direction">Direction to cast the capsule.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hits">List to populate with all hits found.</param>
    /// <returns>Number of hits found.</returns>
    public int CapsuleCastAll(Double3 point1, Double3 point2, double radius, Double3 direction, double maxDistance, List<ShapeCastHit> hits)
    {
        return CapsuleCastAll(point1, point2, radius, direction, maxDistance, hits, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a capsule along a direction with layer filtering and returns all hits.
    /// </summary>
    public int CapsuleCastAll(Double3 point1, Double3 point2, double radius, Double3 direction, double maxDistance, List<ShapeCastHit> hits, LayerMask layerMask)
    {
        // Calculate capsule properties
        Double3 capsuleCenter = (point1 + point2) * 0.5;
        Double3 capsuleAxis = point2 - point1;
        double capsuleLength = Double3.Length(capsuleAxis);

        // Create a capsule shape (aligned along Y-axis)
        var capsule = new CapsuleShape(radius, capsuleLength);

        // Calculate orientation to align capsule with the segment
        JQuaternion capsuleOrientation = CalculateCapsuleOrientation(capsuleAxis, capsuleLength);

        return ShapeCastAll(capsule, capsuleOrientation, capsuleCenter, direction, maxDistance, hits, layerMask);
    }

    /// <summary>
    /// Helper method to calculate the orientation needed to align a capsule (Y-axis aligned) with a given axis.
    /// </summary>
    private static JQuaternion CalculateCapsuleOrientation(Double3 capsuleAxis, double capsuleLength)
    {
        if (capsuleLength <= 1e-6)
            return JQuaternion.Identity;

        Double3 normalizedAxis = capsuleAxis / capsuleLength;
        Double3 yAxis = new Double3(0, 1, 0);

        // If axis is aligned with Y, no rotation needed
        if (Math.Abs(Double3.Dot(normalizedAxis, yAxis) - 1.0) < 1e-6)
        {
            return JQuaternion.Identity;
        }
        // If axis is opposite to Y, rotate 180 degrees around X
        else if (Math.Abs(Double3.Dot(normalizedAxis, yAxis) + 1.0) < 1e-6)
        {
            return JQuaternion.CreateFromAxisAngle(new JVector(1, 0, 0), Math.PI);
        }
        // Calculate rotation from Y-axis to the capsule axis
        else
        {
            Double3 rotAxis = Double3.Cross(yAxis, normalizedAxis);
            rotAxis = Double3.Normalize(rotAxis);
            double angle = Math.Acos(Double3.Dot(yAxis, normalizedAxis));
            return JQuaternion.CreateFromAxisAngle(new JVector(rotAxis.X, rotAxis.Y, rotAxis.Z), angle);
        }
    }

    /// <summary>
    /// Casts a box along a direction and returns the closest hit.
    /// </summary>
    /// <param name="origin">Starting position of the box center.</param>
    /// <param name="size">Size of the box (width, height, depth).</param>
    /// <param name="orientation">Orientation of the box.</param>
    /// <param name="direction">Direction to cast the box.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hitInfo">Information about what was hit.</param>
    /// <returns>True if the box hit something.</returns>
    public bool BoxCast(Double3 origin, Double3 size, JQuaternion orientation, Double3 direction, double maxDistance, out ShapeCastHit hitInfo)
    {
        return BoxCast(origin, size, orientation, direction, maxDistance, out hitInfo, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a box along a direction with layer filtering and returns the closest hit.
    /// </summary>
    public bool BoxCast(Double3 origin, Double3 size, JQuaternion orientation, Double3 direction, double maxDistance, out ShapeCastHit hitInfo, LayerMask layerMask)
    {
        var box = new BoxShape(size.X, size.Y, size.Z);
        return ShapeCast(box, orientation, origin, direction, maxDistance, out hitInfo, layerMask);
    }

    /// <summary>
    /// Casts a box along a direction and returns all hits.
    /// </summary>
    /// <param name="origin">Starting position of the box center.</param>
    /// <param name="size">Size of the box (width, height, depth).</param>
    /// <param name="orientation">Orientation of the box.</param>
    /// <param name="direction">Direction to cast the box.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hits">List to populate with all hits found.</param>
    /// <returns>Number of hits found.</returns>
    public int BoxCastAll(Double3 origin, Double3 size, JQuaternion orientation, Double3 direction, double maxDistance, List<ShapeCastHit> hits)
    {
        return BoxCastAll(origin, size, orientation, direction, maxDistance, hits, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a box along a direction with layer filtering and returns all hits.
    /// </summary>
    public int BoxCastAll(Double3 origin, Double3 size, JQuaternion orientation, Double3 direction, double maxDistance, List<ShapeCastHit> hits, LayerMask layerMask)
    {
        var box = new BoxShape(size.X, size.Y, size.Z);
        return ShapeCastAll(box, orientation, origin, direction, maxDistance, hits, layerMask);
    }

    /// <summary>
    /// Casts a cylinder along a direction and returns the closest hit.
    /// </summary>
    /// <param name="origin">Starting position of the cylinder center.</param>
    /// <param name="radius">Radius of the cylinder.</param>
    /// <param name="height">Height of the cylinder.</param>
    /// <param name="orientation">Orientation of the cylinder.</param>
    /// <param name="direction">Direction to cast the cylinder.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hitInfo">Information about what was hit.</param>
    /// <returns>True if the cylinder hit something.</returns>
    public bool CylinderCast(Double3 origin, double radius, double height, JQuaternion orientation, Double3 direction, double maxDistance, out ShapeCastHit hitInfo)
    {
        return CylinderCast(origin, radius, height, orientation, direction, maxDistance, out hitInfo, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a cylinder along a direction with layer filtering and returns the closest hit.
    /// </summary>
    public bool CylinderCast(Double3 origin, double radius, double height, JQuaternion orientation, Double3 direction, double maxDistance, out ShapeCastHit hitInfo, LayerMask layerMask)
    {
        var cylinder = new CylinderShape(radius, height);
        return ShapeCast(cylinder, orientation, origin, direction, maxDistance, out hitInfo, layerMask);
    }

    /// <summary>
    /// Casts a cylinder along a direction and returns all hits.
    /// </summary>
    /// <param name="origin">Starting position of the cylinder center.</param>
    /// <param name="radius">Radius of the cylinder.</param>
    /// <param name="height">Height of the cylinder.</param>
    /// <param name="orientation">Orientation of the cylinder.</param>
    /// <param name="direction">Direction to cast the cylinder.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hits">List to populate with all hits found.</param>
    /// <returns>Number of hits found.</returns>
    public int CylinderCastAll(Double3 origin, double radius, double height, JQuaternion orientation, Double3 direction, double maxDistance, List<ShapeCastHit> hits)
    {
        return CylinderCastAll(origin, radius, height, orientation, direction, maxDistance, hits, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a cylinder along a direction with layer filtering and returns all hits.
    /// </summary>
    public int CylinderCastAll(Double3 origin, double radius, double height, JQuaternion orientation, Double3 direction, double maxDistance, List<ShapeCastHit> hits, LayerMask layerMask)
    {
        var cylinder = new CylinderShape(radius, height);
        return ShapeCastAll(cylinder, orientation, origin, direction, maxDistance, hits, layerMask);
    }

    /// <summary>
    /// Casts a cone along a direction and returns the closest hit.
    /// </summary>
    /// <param name="origin">Starting position of the cone center.</param>
    /// <param name="radius">Base radius of the cone.</param>
    /// <param name="height">Height of the cone.</param>
    /// <param name="orientation">Orientation of the cone.</param>
    /// <param name="direction">Direction to cast the cone.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hitInfo">Information about what was hit.</param>
    /// <returns>True if the cone hit something.</returns>
    public bool ConeCast(Double3 origin, double radius, double height, JQuaternion orientation, Double3 direction, double maxDistance, out ShapeCastHit hitInfo)
    {
        return ConeCast(origin, radius, height, orientation, direction, maxDistance, out hitInfo, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a cone along a direction with layer filtering and returns the closest hit.
    /// </summary>
    public bool ConeCast(Double3 origin, double radius, double height, JQuaternion orientation, Double3 direction, double maxDistance, out ShapeCastHit hitInfo, LayerMask layerMask)
    {
        var cone = new ConeShape(radius, height);
        return ShapeCast(cone, orientation, origin, direction, maxDistance, out hitInfo, layerMask);
    }

    /// <summary>
    /// Casts a cone along a direction and returns all hits.
    /// </summary>
    /// <param name="origin">Starting position of the cone center.</param>
    /// <param name="radius">Base radius of the cone.</param>
    /// <param name="height">Height of the cone.</param>
    /// <param name="orientation">Orientation of the cone.</param>
    /// <param name="direction">Direction to cast the cone.</param>
    /// <param name="maxDistance">Maximum distance to cast.</param>
    /// <param name="hits">List to populate with all hits found.</param>
    /// <returns>Number of hits found.</returns>
    public int ConeCastAll(Double3 origin, double radius, double height, JQuaternion orientation, Double3 direction, double maxDistance, List<ShapeCastHit> hits)
    {
        return ConeCastAll(origin, radius, height, orientation, direction, maxDistance, hits, LayerMask.Everything);
    }

    /// <summary>
    /// Casts a cone along a direction with layer filtering and returns all hits.
    /// </summary>
    public int ConeCastAll(Double3 origin, double radius, double height, JQuaternion orientation, Double3 direction, double maxDistance, List<ShapeCastHit> hits, LayerMask layerMask)
    {
        var cone = new ConeShape(radius, height);
        return ShapeCastAll(cone, orientation, origin, direction, maxDistance, hits, layerMask);
    }

    #endregion
}
