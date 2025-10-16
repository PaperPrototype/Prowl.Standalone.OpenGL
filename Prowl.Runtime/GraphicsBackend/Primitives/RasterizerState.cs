﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

namespace Prowl.Runtime.GraphicsBackend.Primitives;

public struct RasterizerState
{
    public enum DepthMode { Never, Less, Equal, Lequal, Greater, Notequal, Gequal, Always }
    public enum Blending { Zero, One, SrcColor, OneMinusSrcColor, DstColor, OneMinusDstColor, SrcAlpha, OneMinusSrcAlpha, DstAlpha, OneMinusDstAlpha, ConstantColor, OneMinusConstantColor, ConstantAlpha, OneMinusConstantAlpha, SrcAlphaSaturate, Src1Color, OneMinusSrc1Color, Src1Alpha, OneMinusSrc1Alpha }
    public enum BlendMode { Add, Subtract, ReverseSubtract, Min, Max }
    public enum PolyFace { None, Front, Back, FrontAndBack }
    public enum WindingOrder { CW, CCW }

    public bool depthTest = true;
    public bool depthWrite = true;
    public DepthMode depthMode = DepthMode.Lequal;

    public bool doBlend = true;
    public Blending blendSrc = Blending.SrcAlpha;
    public Blending blendDst = Blending.OneMinusSrcAlpha;
    public BlendMode blendMode = BlendMode.Add;

    public PolyFace cullFace = PolyFace.Back;

    public WindingOrder winding = WindingOrder.CW;

    public RasterizerState()
    {
        // Default constructor
    }
}
