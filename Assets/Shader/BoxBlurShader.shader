Shader "Unlit/BoxBlurShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_BlurSize("Blur Size", Float) = 1.0
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
				float _BlurSize;

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}

				float4 frag(v2f i) : SV_Target
				{
					float2 texelSize = _BlurSize / float2(1024, 1024);
					float4 sum = float4(0, 0, 0, 0);

					// 3x3 Box Blur kernel
					for (int x = -1; x <= 1; x++)
					{
						for (int y = -1; y <= 1; y++)
						{
							sum += tex2D(_MainTex, i.uv + float2(x, y) * texelSize);
						}
					}

					return sum / 9.0; // Normalize
				}
				ENDCG
			}
		}
}
