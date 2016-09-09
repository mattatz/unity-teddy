Shader "Teddy/Demo/Toon" {

	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {} 
		_Params ("Toon Params", Vector) = (0.5, 0.5, 1.5, -1)
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
			float3 lightDir : TEXCOORD1;
		};

		half4 _Color;
		sampler2D _Ramp;
		half4 _Params;

		v2f vert (appdata v) {
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			// o.normal = mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz;
			o.normal = v.normal;
			o.uv = v.uv;
			o.lightDir = ObjSpaceLightDir(v.vertex);
			return o;
		}

		ENDCG

		Pass {
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			half4 frag (v2f i) : SV_Target {
				half d = dot(normalize(i.normal), normalize(i.lightDir)) * _Params.x + _Params.y;
				d = saturate(d);
				half3 ramp = tex2D(_Ramp, float2(d, d)).rgb;
				ramp = pow(ramp, _Params.z);
				return half4(ramp, 1) * _Color;
			}
			ENDCG
		}

	}
}
