// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Jitter2.Collision.Shapes;

using Prowl.Echo;
using Prowl.Vector;

namespace Prowl.Runtime;

public sealed class CapsuleCollider : Collider
{
    [SerializeField] private double radius = 0.5f;
    [SerializeField] private double height = 2;

    public double Radius
    {
        get => radius;
        set
        {
            radius = value;
            OnValidate();
        }
    }

    public double Height
    {
        get => height;
        set
        {
            height = value;
            OnValidate();
        }
    }

    public override RigidBodyShape[] CreateShapes() => [new CapsuleShape(Maths.Max(radius, 0.01), Maths.Max(height, 0.01))];
}
