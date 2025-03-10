Shader "UI/LavaShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Speed("Flow Speed", Float) = 0.5
		_Color("Main Color", Color) = (1, 0, 0, 1) // Κόκκινο ως βασικό χρώμα
		_Stencil("Stencil ID", Float) = 0
		_StencilComp("Stencil Comparison", Float) = 8
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 15
	}

		SubShader
		{
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off

			Stencil
			{
				Ref[_Stencil]
				Comp[_StencilComp]
				Pass[_StencilOp]
				ReadMask[_StencilReadMask]
				WriteMask[_StencilWriteMask]
			}

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
				float _Speed;
				float4 _Color;

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					return o;
				}

				// 🔥 Perlin Noise για κίνηση
				float random(float2 uv)
				{
					return frac(sin(dot(uv.xy, float2(12.9898, 78.233))) * 43758.5453);
				}

				float noise(float2 uv)
				{
					float2 i = floor(uv);
					float2 f = frac(uv);
					float a = random(i);
					float b = random(i + float2(1.0, 0.0));
					float c = random(i + float2(0.0, 1.0));
					float d = random(i + float2(1.0, 1.0));
					float2 u = f * f * (3.0 - 2.0 * f);
					return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
				}
				fixed4 frag(v2f i) : SV_Target
				{
					float2 uv = i.uv;

					// 🔥 Προσθήκη παραμόρφωσης για την κίνηση της λάβας
					float flow = sin(_Time.y * _Speed * 2.0) * 0.02;
					uv.y -= _Time.y * _Speed + flow; // Κίνηση προς τα πάνω + κυματισμός

					// 🔥 Random Noise για bubbling effect
					float n = noise(uv * 5.0 + float2(_Time.y * 0.5, _Time.y * 0.3));

					// 🔥 Χρωματική παλέτα: Κόκκινο → Πορτοκαλί → Κίτρινο
					float r = smoothstep(0.3, 0.6, n) * 1.0;
					float g = smoothstep(0.4, 0.7, n) * 0.5;
					float b = smoothstep(0.5, 0.8, n) * 0.2;

					// 🔥 Αυξάνουμε την ένταση στις άκρες ώστε να ξεφεύγει λίγο από τα όρια
					float edgeGlow = smoothstep(0.4, 0.7, n) * 0.3;

					float alpha = smoothstep(0.3, 0.6, n) + edgeGlow; // Κάνει πιο φωτεινές τις άκρες

					return float4(r, g, b, alpha);
				}
				ENDCG
			}
		}
}