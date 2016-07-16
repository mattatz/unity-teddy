Shader "Teddy/DebugNormal" {

	Properties {
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

		CGINCLUDE

		#include "UnityCG.cginc"

		#pragma target 3.0

		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 uv : TEXCOORD0;
		};

		struct v2f {
			float4 vertex : SV_POSITION;
			float3 normal : NORMAL;
			float2 uv : TEXCOORD0;
		};

		v2f vert (appdata v) {
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			o.normal = mul(_Object2World, float4(v.normal, 0.0)).xyz;
			o.uv = v.uv;
			return o;
		}

		ENDCG

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			fixed4 frag (v2f i) : SV_Target {
				return fixed4(i.normal, 1.0);
			}
			ENDCG
		}
	}
}
