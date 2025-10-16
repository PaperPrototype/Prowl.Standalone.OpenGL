// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Prowl.Runtime.GraphicsBackend.Primitives;

namespace Prowl.Runtime.GraphicsBackend
{
    public abstract class GraphicsTexture : IDisposable
    {
        public abstract TextureType Type { get; protected set; }
        public abstract bool IsDisposed { get; protected set; }
        public abstract void Dispose();
    }
}
