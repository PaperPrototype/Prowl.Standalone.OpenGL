// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

namespace Prowl.Runtime.GraphicsBackend;

public abstract class GraphicsVertexArray : IDisposable
{
    public abstract bool IsDisposed { get; protected set; }
    public abstract void Dispose();
}
