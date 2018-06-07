// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FowCustom/Toon/Basic" {
	Properties {
		_Color ("Main Color", Color) = (.5,.5,.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_ToonShade("ToonShader Cubemap(RGB)", CUBE) = "" { }
	}


	SubShader {
		Tags { "RenderType"="Opaque" }
		UsePass"CustomPass/Passes/SHADOWCASTER"
		Pass {
			Tags{ "LightMode" = "ForwardBase"}
			Name "BASE_Fow"
			//Cull Off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#include "../../Shaders/FogOfWar/FogOfWarMath.cginc"
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			sampler2D _MainTex;
			fixed4 _MainTex_ST;
			samplerCUBE _ToonShade;
			fixed4 _Color;

			half4 _ShadowColor;
			struct appdata {
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				half3 normal : NORMAL;
			};
		
			struct v2f {
				float4 pos : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				WARFOG_COORDS(1)
				half3 cubenormal : TEXCOORD2;
				SHADOW_COORDS(3)
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.cubenormal = mul(UNITY_MATRIX_MV, float4(v.normal, 0));
				TRANSFER_WARFOG(o, v.vertex);
				TRANSFER_SHADOW(o);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color * tex2D(_MainTex, i.texcoord);
				fixed4 cube = texCUBE(_ToonShade, i.cubenormal);
				fixed4 c = fixed4(2.0f * cube.rgb * col.rgb, col.a);

				float4 shadowCol = SHADOW_ATTENUATION(i)<0.5 ? 0.3 : 1;
				c.rgb = lerp(c.rgb, c.rgb * _ShadowColor.rgb, 1 - shadowCol.r);
				c = c * shadowCol;
				APPLY_WARFOG(c, i);
				return c;
			}
			ENDCG			
		}
	} 
}
