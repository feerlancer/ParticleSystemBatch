// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FowCustom/Toon/Basic Outline" {
	Properties {
		_Color ("Main Color", Color) = (.5,.5,.5,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.0001, 1)) = .005
		_MainTex ("Base (RGB)", 2D) = "white" { }
		_ToonShade ("ToonShader Cubemap(RGB)", CUBE) = "" { }
		_FOWColor("Color", Color) = (0.5, 0.5, 0.5, 1)
	}
	
	SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 100

		Pass
        {
            Cull Front 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "../../Shaders/FogOfWar/FogOfWarMath.cginc"
            #include "UnityCG.cginc"
 
            struct v2f
            {
                float4 vertex :POSITION;
            };
 
            fixed _Outline;
            fixed4 _OutlineColor;
 
            v2f vert(appdata_full v)
            {
                v2f o;
                
                fixed4 view_vertex = mul(UNITY_MATRIX_MV,v.vertex);
                fixed3 view_normal = mul((float3x3)UNITY_MATRIX_IT_MV,v.normal);
                view_vertex.xyz += normalize(view_normal) * _Outline; //记得normalize
                o.vertex = mul(UNITY_MATRIX_P,view_vertex);
                return o;
            }
 
            fixed4 frag(v2f IN):COLOR
            {
                return _OutlineColor;
            }
            ENDCG
        }

		Pass {
			
			Cull Off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#pragma multi_compile_fog

			#include "../../Shaders/FogOfWar/FogOfWarMath.cginc"
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			samplerCUBE _ToonShade;
			fixed4 _MainTex_ST;
			fixed4 _Color;

			sampler2D _FogOfWar;
			fixed _LevelWidth;
			fixed _LevelHeight;
			fixed _Scale;
			fixed4 _Origin;
			fixed4 _FOWColor;

			struct appdata {
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				half3 normal : NORMAL;
			};
			
			struct v2f {
				float4 pos : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				half3 cubenormal : TEXCOORD2;
				WARFOG_COORDS(1)
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.cubenormal = mul (UNITY_MATRIX_MV, float4(v.normal,0));
				TRANSFER_WARFOG(o, v.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color * tex2D(_MainTex, i.texcoord);
				fixed4 cube = texCUBE(_ToonShade, i.cubenormal);
				fixed4 c = fixed4(2.0f * cube.rgb * col.rgb, col.a);
				APPLY_WARFOG(c, i);
				return c;
			}
			ENDCG			
		}
	}
	Fallback "Toon/Basic"
}
