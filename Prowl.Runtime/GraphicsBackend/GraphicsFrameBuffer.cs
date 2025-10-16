// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

namespace Prowl.Runtime.GraphicsBackend;

public abstract unsafe class GraphicsFrameBuffer
{
    public struct Attachment
    {
        public GraphicsTexture texture;
        public bool isDepth;
    }

    public abstract bool IsDisposed { get; protected set; }
    public abstract uint Width { get; protected set; }
    public abstract uint Height { get; protected set; }

    public abstract void Dispose();
}
