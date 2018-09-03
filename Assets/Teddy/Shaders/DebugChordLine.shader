// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Teddy/DebugChordLine" {

	Properties {
		_Color0 ("From Color", Color) = (0, 0, 1, 1)
		_Color1 ("To Color", Color) = (1, 0, 0, 1)
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

		CGINCLUDE

		#include "UnityCG.cginc"

		#pragma target 3.0

		fixed4 _Color0, _Color1;

		struct appdata {
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f {
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		v2f vert (appdata v) {
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			return o;
		}

		ENDCG

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			fixed4 frag (v2f i) : SV_Target {
				return lerp(_Color0, _Color1, i.uv.y);
			}
			ENDCG
		}
	}
}
