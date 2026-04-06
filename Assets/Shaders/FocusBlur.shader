Shader "Custom/FocusBlur"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" "RenderPipeLine"="UniversalPipeline" }
		ZTest Always ZWrite Off Cull Off

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			float _BlurStrength;

			half4 Frag(Varyings input) : SV_Target
			{
				float2 uv = input.texcoord;
				float2 d = _BlitTexture_TexelSize.xy * _BlurStrength;

				half4 col =
					  SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv)                      * 0.2270 +
                      SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( d.x,    0)) * 0.1945 +
                      SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x,    0)) * 0.1945 +
                      SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(    0,  d.y)) * 0.1945 +
                      SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(    0, -d.y)) * 0.1945 +
                      SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( d.x,  d.y)) * 0.0133 +
                      SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x,  d.y)) * 0.0133 +
                      SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( d.x, -d.y)) * 0.0133 +
                      SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x, -d.y)) * 0.0133;

                  return col;
				}
				ENDHLSL
			}
		}
	}