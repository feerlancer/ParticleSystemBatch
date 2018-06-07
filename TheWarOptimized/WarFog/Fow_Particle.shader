/*
 *      Author: Starking. 
 *      Version: 17.09.30
 */
 
Shader "FowCustom/Particles/AllInOne_Fow" {
	Properties {
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Main Texture", 2D) = "white" {}
		[KeywordEnum(NONE,ALPHAMASK,CHANNELMASK,DISSOLVE,ADDITIVE,MULTIPLY,DESATURATE)] _SecondType("Second Texture Type", float) = 0
		[Enum(R,0,G,1,B,2,A,3)]_MaskChannel("Mask Channel",float) = 0
		_MaskTex ("Second Texture", 2D) = "white" {}
		_Strength ("Strength", Range(0, 1)) = 1
		_Cutoff ("Cutoff" ,Range(0,1)) = 0
		_EdgeColor ("Edge Color", Color) = (1,1,1,1)
		_EdgeWidth ("Edge Width", Range(0,3)) = 0.15
		[KeywordEnum(NONE,SCROLL,ANISPRITES)] _ScrollType("Animation Type", float) = 0
		_ScrollMain("Main Count (UV), Speed (UV)", vector) = (1,1,0,0)
		_ScrollMask("Second Count (UV), Speed (UV)", vector) = (1,1,0,0)
		_DissloveTex("Dissolve Noise", 2D) = "white" {}//±£¡ÙÃÊªª”√
		[Toggle(_LUT_ON)]_LutOn("Use LUT?",float) = 0
		_LutMap("Ramp(LUT,U)",2D) = "white" {}
		_LUT("LUT",Range(0,1)) = 0.5
		_LUTU("LUT Time",Range(0,1)) = 0
		[Toggle]_VertColorAlphaIsLifeTime("Use Vert Color (A) Ctrl?",float) = 0
		[Toggle(_RIM_ON)]_RimOn("Use Rim?",float) = 0
		_RimColor("Rim Color", Color) = (1,1,1,1)
		_RimPower("Rim Power", Range(0.3,5)) = 3
		[Toggle(_DISPLACEMENT_ON)] _DisplacementOn("Use Displacement?", float) = 0
		_DisplacementTex("Displacement Texture",2D) = "black" {}
		_Displacement("Displacement (R, G, B, H)", vector) = (1,1,1,0.1)
		_Vibrance("Vibrance",float) = 10
		_Explosion("Explosion (Delay, Add, Min, Max)",vector) = (0,0.5,0.02,0.98)
		//_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", float) = 0
		[HideInInspector] _Mode("Blend Mode", float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", float) = 0
		[Toggle]_ZWrite("ZWrite", float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", float) = 4
		_ColorMask("Color Mask", float) = 14
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	sampler2D _MainTex;	float4 _MainTex_ST;

#ifndef _SECONDTYPE_NONE
	fixed _MaskType;
	fixed _MaskChannel;
	sampler2D _MaskTex;	float4 _MaskTex_ST;
	half _Strength;
	#ifdef _SECONDTYPE_DISSOLVE
		half _Cutoff;
		half _EdgeWidth;
		fixed4 _EdgeColor;
	#endif
#endif

#ifndef _SCROLLTYPE_NONE
	fixed _ScrollType;
	half4 _ScrollMain;
	half4 _ScrollMask;
#endif

#if _SECONDTYPE_DISSOLVE || _LUT_ON || _DISPLACEMENT_ON
	fixed _VertColorAlphaIsLifeTime;
#endif

#ifdef _LUT_ON
	sampler2D _LutMap;
	half _LUT;
	half _LUTU;
#endif

#ifdef _DISPLACEMENT_ON
	sampler2D _DisplacementTex;
	half4 _Displacement;
	half _Vibrance;
	half4 _Explosion;
#endif

#ifdef _RIM_ON
	fixed4 _RimColor;
	half _RimPower;
#endif

//#ifdef SOFTPARTICLES_ON
//	sampler2D_float _CameraDepthTexture;
//	fixed _InvFade;
//#endif

	fixed _Mode;

	//vert func vertex offset
	half3 Displacement(half3 normal, fixed4 dispColor, half4 displacement, half vibrance, out half3 disp) {
		disp = 1;
		half lifeFactor = _Time.x * vibrance;
		disp.x = sin(lifeFactor * 2 * UNITY_PI) * 0.5f + 0.5f;
		disp.y = sin((lifeFactor + 0.33333333f) * 2 * UNITY_PI) * 0.5f + 0.5f;
		disp.z = sin((lifeFactor + 0.66666667f) * 2 * UNITY_PI) * 0.5f + 0.5f;
		disp.xyz *= displacement.xyz;
		disp = normalize(disp);
		half d = dispColor.r * disp.x + dispColor.g * disp.y + dispColor.b * disp.z;
		return normal * displacement.w * d;
	}

	//vert func uv scroll
	void ScrollUV(inout half2 uv, half4 scrollParam) {
#if defined(_SCROLLTYPE_SCROLL)
		uv = uv * scrollParam.xy + _Time.xx * scrollParam.zw;
#elif defined(_SCROLLTYPE_ANISPRITES)
		half2 uvOff;
		uvOff.x = floor(_Time.y * scrollParam.z) / scrollParam.x;
		uvOff.y = 1 - floor(_Time.y * scrollParam.z / scrollParam.x) / scrollParam.y;
		uvOff = fmod(uvOff, 1);
		uv = uv / scrollParam.xy + uvOff;
#endif
	}

	//frag func channel mask
	fixed4 ChannelMask(half maskChannel) {
		fixed stepA = 1 - saturate(3 - maskChannel);//greater A level
		fixed stepB = 1 - saturate(abs(2 - maskChannel));//greater B Level But Not Greater A Level
		fixed stepG = 1 - saturate(abs(1 - maskChannel));//greater G Level But Not Greater B Level
		fixed stepR = 1 - saturate(abs(0 - maskChannel));//Not Greater G Level
		return fixed4(stepR, stepG, stepB, stepA);
	}

	//frag func main texture combine second texture
	void TextureBlend(inout fixed4 tex, fixed4 mask) {
#if defined(_SECONDTYPE_ALPHAMASK)
		fixed4 channelMask = ChannelMask(_MaskChannel);
		tex.a *= lerp(1, mask.r * channelMask.r + mask.g * channelMask.g + mask.b * channelMask.b + mask.a * channelMask.a, _Strength);
#elif defined(_SECONDTYPE_ADDITIVE)
		tex += mask * _Strength;
#elif defined(_SECONDTYPE_MULTIPLY)
		tex *= lerp(1, mask, _Strength);
#endif
	}

	//frag func main texture blend
	void TextureBlend(inout fixed4 tex) {
#if defined(_SECONDTYPE_DESATURATE)
		half grayscale = tex.r * 0.3 + tex.g * 0.59 + tex.b * 0.11;
		fixed4 grayColor = fixed4(grayscale, grayscale, grayscale, tex.a);
		tex = lerp(tex, grayColor, _Strength);
#elif defined(_SECONDTYPE_CHANNELMASK)
		fixed4 channelMask = ChannelMask(_MaskChannel);
		tex.rgb = tex.rrr * channelMask.r + tex.ggg * channelMask.g + tex.bbb * channelMask.b + tex.aaa * channelMask.a;
#endif	
	}

	//frag func dissolve factor
	half GetDissolveFactor(fixed4 mask, half4 channelMask) {
		return mask.r * channelMask.r + mask.g * channelMask.g + mask.b * channelMask.b + mask.a * channelMask.a;
	}

	//frag func dissolve
	void Dissolve(inout fixed3 col, half dissMask, half dissolve, half cutoff, half edgeWidth, fixed3 edgeColor) {
		dissolve = dissolve - (1 - dissolve) * cutoff;
		clip(dissMask - dissolve);
		dissolve = saturate(dissolve);
		fixed useEdgeWidth = 1 - step(edgeWidth, 0);
		fixed edgeFactor = 1 - saturate((dissMask - dissolve) / (edgeWidth * dissolve + 0.001));
		col = lerp(col, col + edgeColor * 2 - 1, edgeFactor * useEdgeWidth);
	}

	//frag func dissolve with lut and displacement
	fixed3 LUTDisplacementDissolve(fixed3 dispColor, half3 disp, sampler2D lutMap, half4 explosion, half dissolve, out half dissMask) {
		half dlut = dispColor.r * disp.x + dispColor.g * disp.y + dispColor.b * disp.z;
		dissMask = 1 - dlut;
		half delay = saturate(explosion.x);//emission time
		half maxDist = max(delay, 1 - delay);
		half near = abs(dissolve - delay) / maxDist;
		dlut = dlut - pow((1 - near), 0.75) * explosion.y;
		dlut = saturate(dlut * abs(explosion.w - explosion.z) + min(explosion.z, explosion.w) + dissolve);
		dlut = clamp(dlut, 0.02, 0.98);
		return tex2D(lutMap, float2(dlut, 0.5)).rgb;
	}

	ENDCG


	SubShader{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
		LOD 100
		Blend [_SrcBlend] [_DstBlend]
		ZWrite [_ZWrite]
		ZTest [_ZTest]
		Cull [_Cull]
		ColorMask [_ColorMask]
		Lighting Off

		Pass {

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			//#pragma multi_compile_fog
			//#pragma multi_compile_particles
			//#pragma multi_compile_instancing
			#pragma shader_feature _SECONDTYPE_NONE _SECONDTYPE_ALPHAMASK _SECONDTYPE_CHANNELMASK _SECONDTYPE_DISSOLVE _SECONDTYPE_ADDITIVE _SECONDTYPE_MULTIPLY _SECONDTYPE_DESATURATE
			#pragma shader_feature _SCROLLTYPE_NONE _SCROLLTYPE_SCROLL _SCROLLTYPE_ANISPRITES
			#pragma shader_feature _ _RIM_ON
			#pragma shader_feature _ _LUT_ON
			#pragma shader_feature _ _DISPLACEMENT_ON

			#include "../../Shaders/FogOfWar/FogOfWarMath.cginc"

			fixed4 _TintColor;

			struct a2v {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				half2 texcoord : TEXCOORD0;
#if (defined(_DISPLACEMENT_ON) || defined(_RIM_ON))
				half3 normal : NORMAL;
#endif
				//UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				half2 uvMain : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
#ifdef _RIM_ON
				half3 normal : NORMAL;
				half3 viewDir : TEXCOORD2;
#endif

#if (defined(_SECONDTYPE_ALPHAMASK) || defined(_SECONDTYPE_DISSOLVE) || defined(_SECONDTYPE_ADDITIVE) || defined(_SECONDTYPE_MULTIPLY))
	#ifdef _RIM_ON
				half2 uvMask : TEXCOORD3;
	#else
				half2 uvMask : TEXCOORD2;
	#endif
		
	#ifdef _DISPLACEMENT_ON
		#ifdef _RIM_ON
				half3 disp : TEXCOORD4;
		#else
				half3 disp : TEXCOORD3;
		#endif
	#endif
#else
	#ifdef _DISPLACEMENT_ON
		#ifdef _RIM_ON
				half3 disp : TEXCOORD3;
		#else
				half3 disp : TEXCOORD2;
		#endif
	#endif
#endif
				WARFOG_COORDS(5)
				//UNITY_VERTEX_OUTPUT_STEREO
				//UNITY_FOG_COORDS(6)
				//UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			//UNITY_INSTANCING_CBUFFER_START(Props)
			//	UNITY_DEFINE_INSTANCED_PROP(fixed4, _TintColor)
			//UNITY_INSTANCING_CBUFFER_END

			v2f vert(a2v v)
			{
				v2f o;

				//UNITY_SETUP_INSTANCE_ID(v);
				//UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.uvMain = (v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw);
#ifndef _SCROLLTYPE_NONE
				ScrollUV(/* inout */o.uvMain, _ScrollMain);
#endif

#if (defined(_SECONDTYPE_ALPHAMASK) || defined(_SECONDTYPE_DISSOLVE) || defined(_SECONDTYPE_ADDITIVE) || defined(_SECONDTYPE_MULTIPLY))
				o.uvMask = (v.texcoord.xy * _MaskTex_ST.xy + _MaskTex_ST.zw);
	#ifndef _SCROLLTYPE_NONE
				ScrollUV(/* inout */o.uvMask, _ScrollMask);
	#endif
#endif

#ifdef _DISPLACEMENT_ON

	#ifdef _SECONDTYPE_DISSOLVE
				fixed4 displaceColor = tex2Dlod(_DisplacementTex, float4(o.uvMask, 0, 0));
	#else
				fixed4 displaceColor = tex2Dlod(_DisplacementTex, float4(v.texcoord.xy, 0, 0));
	#endif
				v.vertex.xyz += Displacement(v.normal, displaceColor, _Displacement, _Vibrance, /* out */o.disp);
#endif

				//UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.color = v.color;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//float4x4 bbmv = UNITY_MATRIX_MV;
				//bbmv._m00 = -1.0 / length(unity_WorldToObject[0].xyz);
				//bbmv._m10 = 0.0f;
				//bbmv._m20 = 0.0f;
				//bbmv._m01 = 0.0f;
				//bbmv._m11 = -1.0 / length(unity_WorldToObject[1].xyz);
				//bbmv._m21 = 0.0f;
				//bbmv._m02 = 0.0f;
				//bbmv._m12 = 0.0f;
				//bbmv._m22 = -1.0 / length(unity_WorldToObject[2].xyz);
				//o.vertex = mul(UNITY_MATRIX_P, mul(bbmv, v.vertex));

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				TRANSFER_WARFOG(o, v.vertex);
#ifdef _RIM_ON
				o.normal = mul(v.normal, (float3x3)unity_WorldToObject);
				o.viewDir = _WorldSpaceCameraPos.xyz - o.worldPos.xyz;
#endif
				//UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				//UNITY_SETUP_INSTANCE_ID(i);

				fixed4 tex = tex2D(_MainTex, i.uvMain);

#if (defined(_SECONDTYPE_ALPHAMASK) || defined(_SECONDTYPE_ADDITIVE) || defined(_SECONDTYPE_MULTIPLY))
				TextureBlend(/* inout */tex, tex2D(_MaskTex, i.uvMask));
#else
				TextureBlend(/* inout */tex);
#endif

				fixed4 iColor = i.color;//vert color

#if (defined(_SECONDTYPE_DISSOLVE) || defined(_LUT_ON) || defined(_DISPLACEMENT_ON))
				iColor.a = lerp(iColor.a, 1, _VertColorAlphaIsLifeTime); 
#endif

				fixed4 col = 2.0 * iColor * tex;

#ifdef _LUT_ON
				fixed3 lutColor = fixed3(0, 0, 0);
	#if !(defined(_DISPLACEMENT_ON) && defined(_SECONDTYPE_DISSOLVE))
				half lut = lerp(_LUTU, _LUTU * (1 - i.color.a), _VertColorAlphaIsLifeTime);
				lutColor = tex2D(_LutMap, float2(clamp(lut, 0.02, 0.98), 0.5)).rgb;
	#endif
#endif

#ifdef _SECONDTYPE_DISSOLVE

				half dissolve = lerp(_Strength, 1 - i.color.a, _VertColorAlphaIsLifeTime);//0 use Strength, 1 use 1 - icolor.a
	#if (defined(_LUT_ON) && defined(_DISPLACEMENT_ON))
				fixed3 dispColor = tex2D(_DisplacementTex, i.uvMask).rgb;
				half dissMask = 0;
				lutColor = LUTDisplacementDissolve(dispColor, i.disp, _LutMap, _Explosion, dissolve, /* out */dissMask);
				Dissolve(/* out */lutColor, dissMask, dissolve, _Cutoff, _EdgeWidth, _EdgeColor.rgb);
	#else
				half dissMask = GetDissolveFactor(tex2D(_MaskTex, i.uvMask), ChannelMask(_MaskChannel));
				Dissolve(/* out */col.rgb, dissMask, dissolve, _Cutoff, _EdgeWidth, _EdgeColor.rgb);
	#endif
#endif

#ifdef _LUT_ON
				col.rgb = lerp(col.rgb, lerp(lutColor * col.rgb, lutColor, saturate(_LUT * 2 - 1)), saturate(_LUT * 2));
#endif

				col *= _TintColor;

#ifdef _RIM_ON
				half NdotV = saturate(dot(normalize(i.normal), normalize(i.viewDir)));
				fixed3 rimColor = pow(1 - NdotV, _RimPower) * (_RimColor.rgb * 2 - 1);
				col.rgb += rimColor;
#endif
				APPLY_WARFOG(col, i);
				return col;
			}

			ENDCG 
		}	
	}
	CustomEditor "FowParticleShaderInspector"
}
