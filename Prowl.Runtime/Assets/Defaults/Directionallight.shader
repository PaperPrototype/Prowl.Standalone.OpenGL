﻿Shader "Default/DictionalLight"

Pass 0
{
	Vertex
	{
		in vec3 vertexPosition;

		void main() 
		{
			gl_Position =vec4(vertexPosition, 1.0);
		}
	}

	Fragment
	{
		layout(location = 0) out vec4 gBuffer_lighting;
		
		uniform mat4 matProjection;
		uniform mat4 mvpInverse;
		uniform mat4 matViewInverse;

		uniform vec2 Resolution;
		
		uniform vec3 LightDirection;
		uniform vec4 LightColor;
		uniform float LightIntensity;
		
		uniform sampler2D gAlbedoAO; // Albedo & Roughness
		uniform sampler2D gNormalMetallic; // Normal & Metalness
		uniform sampler2D gPositionRoughness; // Depth

		uniform sampler2D shadowMap; // Shadowmap
		uniform mat4 matCamViewInverse;
		uniform mat4 matShadowView;
		uniform mat4 matShadowSpace;
		
		uniform float u_Bias;
		uniform float u_NormalBias;
		uniform float u_Radius;
		uniform int u_QualitySamples;
		
		#include "PBR"

		float random(vec2 co) {
		    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453123);
		} 
		// ----------------------------------------------------------------------------
		
		vec2 VogelDiskSample(int sampleIndex, int samplesCount, float phi)
		{
		    float GoldenAngle = 2.4;
		
		    float r = sqrt(float(sampleIndex) + 0.5) / sqrt(float(samplesCount));
		    float theta = float(sampleIndex) * GoldenAngle + phi;
		
		    float sine = sin(theta);
		    float cosine = cos(theta);
		
		    return vec2(r * cosine, r * sine);
		}

		float InterleavedGradientNoise(vec2 position_screen)
		{
		    vec3 magic = vec3(0.06711056, 0.00583715, 52.9829189);
		    return fract(magic.z * fract(dot(position_screen, magic.xy)));
		}
		
		float pcf_poisson_filter(vec2 uv, float z0, float bias, float filter_radius_uv)
		{
		    float sum = 0.0;
			float gradient = InterleavedGradientNoise(gl_FragCoord.xy);
		    for (int i = 0; i < u_QualitySamples; ++i)
		    {
				vec2 sampleUV = VogelDiskSample(i, u_QualitySamples, gradient);
		        float shadow_map_depth = texture(shadowMap, uv + sampleUV * vec2(u_Radius, u_Radius)).r;
		        sum += shadow_map_depth < (z0 - bias) ? 0.0 : 1.0;
		    }
		
			return clamp(sum / float(u_QualitySamples), 0.0, 1.0);
		}

		
		// ------------------------------------------------------------------
		
		float ShadowCalculation(vec3 p, vec3 gPos, vec3 normal, vec3 lightDir){
            // Calculate bias based on surface angle
            float bias = u_Bias*tan(acos(max(dot(normal, lightDir), 0.0)));
            bias = clamp(bias, 0, 0.01);

            // Transform position to light space
            vec4 fragPosLightSpace = matShadowSpace * vec4(p, 1.0);
            vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
            projCoords = projCoords * 0.5 + 0.5;

            // Check if fragment is outside shadow map bounds
            if (projCoords.x > 1.0 || projCoords.y > 1.0 || projCoords.z > 1.0 || 
                projCoords.x < 0.0 || projCoords.y < 0.0 || projCoords.z < 0.0)
                return 0.0;

            // Simple comparison: depth from shadow map vs. current fragment depth
            float shadowMapDepth = texture(shadowMap, projCoords.xy).r;
            float currentDepth = projCoords.z;
            
            // 1.0 = in shadow, 0.0 = not in shadow
            return (shadowMapDepth < (currentDepth - bias)) ? 1.0 : 0.0;
		} 
		// ----------------------------------------------------------------------------


		void main()
		{
			vec2 TexCoords = gl_FragCoord.xy / Resolution;
		
			vec4 gPosRough = textureLod(gPositionRoughness, TexCoords, 0);
			vec3 gPos = gPosRough.rgb;
			if(gPos == vec3(0, 0, 0)) discard;
		
			vec3 gAlbedo = textureLod(gAlbedoAO, TexCoords, 0).rgb;
			vec4 gNormalMetal = textureLod(gNormalMetallic, TexCoords, 0);
			vec3 gNormal = gNormalMetal.rgb;
			float gMetallic = gNormalMetal.a;
			float gRoughness = gPosRough.a;

			// calculate reflectance at normal incidence; if dia-electric (like plastic) use F0 
			// of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow)    
			vec3 F0 = vec3(0.04); 
			F0 = mix(F0, gAlbedo, gMetallic);
			vec3 N = normalize(gNormal);
			vec3 V = normalize(-gPos);

			
			vec3 L = normalize(-LightDirection);
			vec3 H = normalize(V + L);

			vec3 radiance = LightColor.rgb * LightIntensity;    
			
			// cook-torrance brdf
			float NDF = DistributionGGX(N, H, gRoughness);        
			float G   = GeometrySmith(N, V, L, gRoughness);  
			vec3 F    = FresnelSchlick(max(dot(H, V), 0.0), F0);
			
			vec3 nominator    = NDF * G * F;
			float denominator = 4 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.001; 
			vec3 specular     = nominator / denominator;

			vec3 kS = F;
			vec3 kD = vec3(1.0) - kS;
			kD *= 1.0 - gMetallic;     

			// shadows
			//vec4 fragPosLightSpace = matShadowSpace * matViewInverse * vec4(gPos, 1);
			vec4 fragPosLightSpace = matCamViewInverse * vec4(gPos + (N * u_NormalBias), 1);
			
			float shadow = ShadowCalculation(fragPosLightSpace.xyz, gPos, N, L);
			    
			// add to outgoing radiance Lo
			float NdotL = max(dot(N, L), 0.0);                
			vec3 color = ((kD * gAlbedo) / PI + specular) * radiance * (1.0 - shadow) * NdotL;
			//vec3 color = ((kD * gAlbedo) / PI + specular) * radiance * NdotL;

			gBuffer_lighting = vec4(color, 1.0);

			//vec4 depth = matProjection * vec4(gPos, 1.0);
			//gl_FragDepth = depth.z / depth.w;
		}

	}
}