﻿Shader "Default/Gizmo"

Pass "Gizmo"
{
	Blend
    {
        Src Color SourceAlpha
        Src Alpha SourceAlpha

        Dest Color InverseSourceAlpha
        Dest Alpha InverseSourceAlpha

        Mode Color Add
        Mode Alpha Add
    }

    // Stencil state
    DepthStencil
    {
        // Depth write
        DepthWrite On

        // Comparison kind
        DepthTest LessEqual
    }

    // Rasterizer culling mode
    Cull None

	HLSLPROGRAM
        #pragma vertex Vertex
        #pragma fragment Fragment
        
        #include "Prowl.hlsl"

        struct Attributes
        {
            float3 pos : POSITION;
            float4 col : COLOR0;
            float2 uv : TEXCOORD0;
        };


        struct Varyings
        {
            float4 pos : SV_POSITION;
            float4 col : INTERPOLATE0;
            float2 uv : INTERPOLATE1;
        };

		Texture2D<float4> _MainTex;
		SamplerState sampler_MainTex;

        Varyings Vertex(Attributes input)
        {
            Varyings output = (Varyings)0;

            output.pos = mul(PROWL_MATRIX_MVP, float4(input.pos, 1.0));
            output.col = input.col;
            output.uv = input.uv;

            return output;
        }


		float4 Fragment(Varyings input) : SV_TARGET
		{
			float4 color = _MainTex.Sample(sampler_MainTex, input.uv) * input.col;
			if(color.a < 0.5)
				discard;
            return color;
		}
	ENDHLSL
}
