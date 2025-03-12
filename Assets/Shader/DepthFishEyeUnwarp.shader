Shader "Custom/DepthFishEyeUnwarp"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_DepthScale("Depth Scale", Float) = 0.001
		_MinRange("Min Range(m)", Range(0, 10)) = 0
		_MaxRange("Max Range(m)", Range(0, 10)) = 5.0
		[Toggle] _GrayScale("Gray Scale", Float) = 0
		_Red("Red", Range(0, 1)) = 0.2
		_Green("Green", Range(0, 1)) = 0.2
		_Blue("Blue", Range(0, 1)) = 0.2
		_Strength("Distortion Strength", Float) = 0.5
	}

		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			Pass
			{
				ZWrite Off
				Cull Off
				Fog { Mode Off }

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				sampler2D _MainTex;
				float _DepthScale;
				float _MinRange;
				float _MaxRange;
				bool _GrayScale;
				float _Red;
				float _Green;
				float _Blue;
				float _Strength; // FishEye Unwarp Strength

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
					// 1️⃣ Εφαρμόζουμε το FishEye Unwarp
					float2 unwarpedUV = FisheyeCorrection(i.uv, _Strength);

					// 2️⃣ Διαβάζουμε το βάθος από το texture
					float maxGray16 = 0xffff;
					float z = tex2D(_MainTex, unwarpedUV).r; // r είναι το unscaled depth
					float distMeters = z * maxGray16 * _DepthScale;

					// 3️⃣ Φιλτράρισμα βάθους
					if (distMeters < _MinRange || distMeters > _MaxRange || distMeters == 0)
						return float4(0, 0, 0, 1);

					// 4️⃣ Κανονικοποίηση βάθους
					float norm = (distMeters - _MinRange) / (_MaxRange - _MinRange);

					// 5️⃣ Αν grayscale, επιστρέφουμε μόνο γκρι
					if (_GrayScale)
					{
						return float4(norm, norm, norm, 1);
					}

					// 6️⃣ Εφαρμόζουμε το χρωματικό μοντέλο
					_Red = clamp(_Red, 0, 1);
					_Green = clamp(_Green, 0, 1);
					_Blue = clamp(_Blue, 0, 1);

					float r = pow(_Red, 1 - norm);
					float g = _Green * (-pow(norm, 2) + norm) * 15;
					float b = pow(_Blue, norm);

					return float4(r, g, b, 1);
				}

				v2f vert(appdata_base v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.texcoord;
					return o;
				}

				ENDCG
			}
		}
			FallBack Off
}