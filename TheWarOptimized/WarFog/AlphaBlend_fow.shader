

Shader "FowCustom/Alpha Blended_Fow" {
Properties
{
_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
_MainTex ("Base (RGB)", 2D) = "white" {}

}
 
SubShader
{
	Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask RGB
	Cull Off 
	Lighting Off 
	ZWrite Off
	LOD 200
 
	Pass
	{
        
		Tags {"LightMode" = "ForwardBase"}

		Name "BASE_Fow"
 
		CGPROGRAM
 
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fwdbase

		#pragma multi_compile DEPTH_SHADOW_OFF DEPTH_SHADOW_ON

		//#pragma multi_compile_fog

		#include "../../Shaders/FogOfWar/FogOfWarMath.cginc"

		#include "UnityCG.cginc"
		#include "AutoLight.cginc"
		#include "Lighting.cginc"


		sampler2D _MainTex;
		fixed4 _TintColor;
		fixed4 _ShadowColor;
		struct appdata_t 
		{
			float4 vertex : POSITION;
			half2 texcoord : TEXCOORD0;
		};


		struct v2f
		{
			float4 pos     : SV_POSITION;
			half2 uv      : TEXCOORD1;
			LIGHTING_COORDS(3,4)
			WARFOG_COORDS(5)
		};
 
		v2f vert(appdata_tan v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord.xy;

			TRANSFER_WARFOG(o, v.vertex);
 
			TRANSFER_VERTEX_TO_FRAGMENT(o);
			return o;
		}
 
		float4 frag(v2f i) : COLOR
		{

			float4 colorTex = tex2D(_MainTex, i.uv.xy );
			float  atten = LIGHT_ATTENUATION(i);

			float4 c = colorTex * atten * _TintColor*2;

			float4 shadowCol = SHADOW_ATTENUATION(i)<0.5 ? 0.3 : 1;
			c.rgb = lerp(c.rgb, c.rgb * _ShadowColor.rgb, 1 - shadowCol.r);
			c = c * shadowCol;
			APPLY_WARFOG(c, i);
			return c;

		}
 
		ENDCG
	}
	} 
	CustomEditor "QueneChangeShaderGUI"
}