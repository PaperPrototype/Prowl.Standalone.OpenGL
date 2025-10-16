// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

namespace Prowl.Runtime.GraphicsBackend.Primitives
{
    [Flags]
    public enum ClearFlags
    {
        Color = 1 << 1,
        Depth = 1 << 2,
        Stencil = 1 << 3,
    }
}
