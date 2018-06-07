
Shader "SJ/PostEffectStack" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BloomTex ("", 2D) = "" {}
		_OverlayTex ("", 2D) = "" {}

	}
	
	CGINCLUDE

		#pragma multi_compile __ BLOOM
		#pragma multi_compile __ COLOR_ADJUST
		#pragma multi_compile __ OVERLAY

		#include "UnityCG.cginc"
		#include "ColorAdjust.cginc"
		//#include "Bloom.cginc"

		sampler2D _MainTex;
		uniform half4 _MainTex_TexelSize;
		half4 _MainTex_ST;

		sampler2D _BloomTex;

		sampler2D _OverlayTex;
		half _Overlay_Intensity;

		struct v2f_simple 
		{
			float4 pos : SV_POSITION; 
			half2 uv : TEXCOORD0;

        #if UNITY_UV_STARTS_AT_TOP
			half2 uvFlippedY : TEXCOORD1;
		#endif
		};	
		
		v2f_simple VertPostEffectStack ( appdata_img v )
		{
			v2f_simple o;
			
			o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
        	o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
        	
        #if UNITY_UV_STARTS_AT_TOP
        	o.uvFlippedY = o.uv;
        	if (_MainTex_TexelSize.y < 0.0)
        		o.uv.y = 1.0 - o.uv.y;
        #endif
        	        	
			return o; 
		}

		fixed4 FragPostEffectStack(v2f_simple i) : SV_Target
		{
			fixed4 color;
			#if UNITY_UV_STARTS_AT_TOP
				color = tex2D(_MainTex, i.uvFlippedY);
			#else
				color = tex2D(_MainTex, i.uv);
			#endif


			#if BLOOM
				color += tex2D(_BloomTex, i.uv);
			#endif

			#if COLOR_ADJUST
				color = ColorAdjust(color);
			#endif

			#if OVERLAY
				half4 toBlend = tex2D(_OverlayTex,i.uv) * _Overlay_Intensity;
				color *= toBlend;
			#endif
			return color;

		}

			ENDCG

			SubShader {
			Cull Off ZWrite Off ZTest Always

				// 0
				Pass
			{

				CGPROGRAM
				#pragma vertex VertPostEffectStack
				#pragma fragment FragPostEffectStack

				ENDCG

			}
		}
				FallBack Off
}
