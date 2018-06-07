// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "FowCustom/Unlit/Transparent" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha 
	
	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#pragma target 2.0
			#pragma multi_compile DEPTH_SHADOW_OFF DEPTH_SHADOW_ON
			//#pragma multi_compile_fog
			
			#include "../../Shaders/FogOfWar/FogOfWarMath.cginc"
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			struct appdata_t {
				half4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
			};
			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
				fixed3 worldPos : TEXCOORD1;
				SHADOW_COORDS(2)
			};
			sampler2D _MainTex;
			fixed4 _MainTex_ST;

			fixed4 _ShadowColor;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				TRANSFER_SHADOW(o);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.texcoord);
				fixed4 shadowCol = SHADOW_ATTENUATION(i)<0.5 ? 0.3 : 1;
				c.rgb = lerp(c.rgb, c.rgb * _ShadowColor.rgb, 1 - shadowCol.r);
				c = c * shadowCol;
				return ApplyWarFog(c, i.worldPos);
			}
		ENDCG
	}
}

}
