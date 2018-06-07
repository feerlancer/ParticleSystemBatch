// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FowCustom/Toon/Basic_Map" {
	Properties {
		_Color ("Main Color", Color) = (.5,.5,.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}


	SubShader {
		Tags { "RenderType"="Opaque" }
		UsePass"CustomPass/Passes/SHADOWCASTER"
		Pass {
			Tags{ "LightMode" = "ForwardBase"}//map�ں�����Ⱦ������overdraw
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
			fixed4 _Color;

			half4 _ShadowColor;
			struct appdata {
				half4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				fixed3 normal : NORMAL;
			};
		
			struct v2f {
				float4 pos : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				//fixed3 worldPos : TEXCOORD1;
				WARFOG_COORDS(1)
				SHADOW_COORDS(2)
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				TRANSFER_WARFOG(o, v.vertex);
				TRANSFER_SHADOW(o);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color * tex2D(_MainTex, i.texcoord);
				fixed4 c = fixed4(fixed3(1.286, 1.255, 1.318)*col.rgb, col.a);
				fixed4 shadowCol = SHADOW_ATTENUATION(i)<0.5 ? 0.3 : 1;
				c.rgb = lerp(c.rgb, c.rgb * _ShadowColor.rgb, 1 - shadowCol.r);
				c = c * shadowCol;
				APPLY_WARFOG(c, i);
				return c;
			}
			ENDCG			
		}
	} 
}
