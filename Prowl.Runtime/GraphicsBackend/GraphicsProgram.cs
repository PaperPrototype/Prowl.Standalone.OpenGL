// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;

using Prowl.Vector;

namespace Prowl.Runtime.GraphicsBackend;

public abstract class GraphicsProgram : IDisposable
{
    private static int _nextId = 0;

    public int ID { get; }

    public abstract bool IsDisposed { get; protected set; }
    public abstract void Dispose();

    protected GraphicsProgram()
    {
        ID = System.Threading.Interlocked.Increment(ref _nextId);
    }

    // Uniform cache - tracks what values are currently set in this shader program
    internal class UniformCache
    {
        public Dictionary<string, float> floats = [];
        public Dictionary<string, int> ints = [];
        public Dictionary<string, Float2> vectors2 = [];
        public Dictionary<string, Float3> vectors3 = [];
        public Dictionary<string, Float4> vectors4 = [];
        public Dictionary<string, Float4x4> matrices = [];
        public Dictionary<string, GraphicsBuffer> buffers = [];

        public void Clear()
        {
            floats.Clear();
            ints.Clear();
            vectors2.Clear();
            vectors3.Clear();
            vectors4.Clear();
            matrices.Clear();
            buffers.Clear();
        }
    }

    internal UniformCache uniformCache = new();
}
