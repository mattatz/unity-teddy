// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Teddy/Demo/Puppet" {

	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {} 
		_ToonParams ("Toon Params", Vector) = (0.47, 0.32, 1.44, -1)

		_DisplacementParams ("Displacement Params", Vector) = (0.15, 0.75, 1.0, -1)
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

		CGINCLUDE

		#include "UnityCG.cginc"
		#include "./Common/SimplexNoise3D.cginc"

		#pragma target 3.0

		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 uv : TEXCOORD0;
		};

		struct v2f {
			float4 vertex : SV_POSITION;
			float3 screenPos : TANGENT;
			float2 uv : TEXCOORD0;
		};

		half4 _Color;
		sampler2D _Ramp;
		half4 _ToonParams;
		half4 _DisplacementParams;

		v2f vert (appdata v) {
			v2f OUT;

			v.vertex.xyz += v.normal * snoise(v.vertex.xyz * _DisplacementParams.x + float3(0, _Time.y, 0) * _DisplacementParams.y) * _DisplacementParams.z;
			OUT.vertex = UnityObjectToClipPos(v.vertex);
			OUT.screenPos = ComputeScreenPos(OUT.vertex);
			OUT.uv = v.uv;

			return OUT;
		}

		ENDCG

		Pass {
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			half4 frag (v2f IN) : SV_Target {
				float3 dx = ddx(IN.screenPos.xyz);
				float3 dy = ddy(IN.screenPos.xyz);
				float3 normal = normalize(cross(dx, dy));

				half d = dot(normal, normalize(float3(0.5, -0.75, 0.5))) * _ToonParams.x + _ToonParams.y;
				d = saturate(d);
				half3 ramp = tex2D(_Ramp, float2(d, d)).rgb;
				ramp = pow(ramp, _ToonParams.z);
				return half4(ramp, 1) * _Color;
			}
			ENDCG
		}

	}
}
