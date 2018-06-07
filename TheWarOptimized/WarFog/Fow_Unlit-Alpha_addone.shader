// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "FowCustom/Unlit/Transparent_addone" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_FOWColor("Color", Color) = (0.5, 0.5, 0.5, 1)
}

SubShader {
	Tags {"Queue"="Transparent+1" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha 
	
	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			//#pragma multi_compile_fog
			
			#include "../../Shaders/FogOfWar/FogOfWarMath.cginc"
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
				WARFOG_COORDS(1)
			};

			sampler2D _MainTex;
			fixed4 _MainTex_ST;

			sampler2D _FogOfWar;
			fixed _LevelWidth;
			fixed _LevelHeight;
			fixed _Scale;
			fixed4 _Origin;
			fixed4 _FOWColor;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				TRANSFER_WARFOG(o, v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.texcoord);
				APPLY_WARFOG(col, i);
				return col;
			}
		ENDCG
	}
}

}
