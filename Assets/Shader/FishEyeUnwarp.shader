Shader "Unlit/FishEyeUnwarp"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Strength("Distortion Strength", Float) = 0.5
	}

		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				struct appdata_t
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float _Strength; // Strength of correction

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}

				float2 FisheyeCorrection(float2 uv, float strength)
				{
					float2 centeredUV = uv * 2.0 - 1.0; // Normalize to range [-1,1]
					float r = length(centeredUV);
					float distortion = 1.0 + strength * (r * r);
					float2 correctedUV = centeredUV / distortion;
					return (correctedUV + 1.0) / 2.0; // Convert back to [0,1]
				}

				float4 frag(v2f i) : SV_Target
				{
					float2 unwarpedUV = FisheyeCorrection(i.uv, _Strength);
					return tex2D(_MainTex, unwarpedUV);
				}
				ENDCG
			}
		}
}