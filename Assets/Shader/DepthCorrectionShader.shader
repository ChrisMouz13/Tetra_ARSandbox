Shader "Custom/DepthCorrectionShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
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
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);

				// 🔹 Remapping UV για διόρθωση παραμόρφωσης
				float2 adjustedUV = v.uv;
				adjustedUV.x = (adjustedUV.x - 0.5) * 1.6 + 0.5; // Επέκταση οριζόντια
				adjustedUV.y = (adjustedUV.y - 0.5) * 1.4 + 0.5; // Επέκταση κάθετα

				o.uv = adjustedUV;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				if (i.uv.x < 0 || i.uv.x > 1 || i.uv.y < 0 || i.uv.y > 1)
					return fixed4(0, 0, 0, 1); // Out-of-bounds → Μαύρο

				return tex2D(_MainTex, i.uv);
			}
			ENDCG
		}
	}
}