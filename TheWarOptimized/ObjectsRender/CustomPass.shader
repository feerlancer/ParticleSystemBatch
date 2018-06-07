Shader "CustomPass/Passes" {
    Properties {
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
		Pass{
			Name "ForwardBaseShadowOptimized"
			Tags{ "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma multi_compile_fwdbase
			#pragma vertex vert
			#pragma fragment frag
			//#pragma target 2.0

			#include "../../Shaders/FogOfWar/FogOfWarMath.cginc"
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "TheWarLight.cginc"
			ENDCG
		}

		Pass{
			Name "BakForwardBaseShadow"
			Tags{ "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma multi_compile_fwdbase
			#pragma vertex vert
			#pragma fragment frag
			//#pragma target 2.0

			#include "../../Shaders/FogOfWar/FogOfWarMath.cginc"
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			struct appdata_t {

				float4 vertex : POSITION;
				fixed2 texcoord : TEXCOORD0;
				fixed3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				fixed2 texcoord : TEXCOORD0;
				fixed3 cubenormal : TEXCOORD2;
				fixed3 worldPos : TEXCOORD1;

				SHADOW_COORDS(3)
			};

			sampler2D _MainTex;
			//fixed _pow;
			fixed4 _MainTex_ST;
			sampler2D _MaskTex;
			samplerCUBE _reflect_df;
			fixed _speed;
			fixed _max;
			fixed _r;
			fixed _g;
			fixed _b;
			fixed4 _TimeEditor;
			fixed4 _TeamColor;

			fixed4 _ShadowColor;
			v2f vert(appdata_t v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.cubenormal = mul(UNITY_MATRIX_MV, fixed4(v.normal,0));
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);

				TRANSFER_SHADOW(o);
				return o;

			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed2 uvem = i.texcoord*.5 + .5;

			fixed2 uvteamcolor = i.texcoord*.5;
			uvteamcolor.x += 0;
			uvteamcolor.y += 0.5;

			fixed2 uvsp = i.texcoord*.5;

			fixed4 col = tex2D(_MainTex, i.texcoord);

			fixed4 emmask = tex2D(_MainTex, uvem);

			fixed4 teamcolormask = tex2D(_MainTex, uvteamcolor);

			fixed4 spmask = tex2D(_MainTex, uvsp);

			fixed basemask = 1 - spmask.a;

			fixed changemask = (col.r * 0.3 + col.g * 0.59 + col.b* 0.11 + 0.2)*teamcolormask.a;

			fixed4 fincol = col*(1 - teamcolormask.a) + changemask*_TeamColor;

			fixed4 cube = texCUBE(_reflect_df, i.cubenormal);

			fixed4 time = _Time + _TimeEditor;
			fixed4 speed = (sin(time.g * _speed)*0.5 + 1)*_max;

			fixed4 c = fixed4(cube*spmask.a*_r*(cube - .2)*fincol.rgb + cube*basemask*_b + 1.5*fincol.rgb*(cube)+fincol.rgb*emmask.a*speed, 1);

			fixed4 shadowCol = SHADOW_ATTENUATION(i)<0.5 ? 0.3 : 1;
			c.rgb = lerp(c.rgb, c.rgb * _ShadowColor.rgb, 1 - shadowCol.r);
			c = c * shadowCol;
			return ApplyWarFog(c, i.worldPos);
			}
				ENDCG
			}


		Pass{
			Stencil{
				Ref 1
				Comp Always
				Pass Replace
				ZFail Replace
			}
			Name "ForwardBaseShadowOptimizedOutlinePre"
			Tags{ "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma multi_compile_fwdbase
			#pragma vertex vert
			#pragma fragment frag

			#include "../../Shaders/FogOfWar/FogOfWarMath.cginc"
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "TheWarLight.cginc"
			ENDCG
			}

        Pass {
            Name "Outline"
			Cull Front
			//ZWrite Off
			ZTest Off
			stencil{
				Ref 1
				Comp NotEqual
				Pass Keep
			}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#include "UnityCG.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            //#pragma multi_compile_shadowcaster
            //#pragma only_renderers d3d9 d3d11 glcore gles 
            //#pragma target 3.0

            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                //o.pos = UnityObjectToClipPos( float4(v.vertex.xyz + v.normal*_OutlineWidth,1) );
				//return o;
				o.pos = UnityObjectToClipPos(v.vertex);
				float3 norm = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
				norm.xy = normalize(norm.xy);
				float2 offset = TransformViewToProjection(norm.xy);
				offset *= _OutlineWidth;
				o.pos.xy += (offset *o.pos.z*0.001);
				o.pos.z = 0;
				return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                return fixed4(_OutlineColor.xyz,1);
            }
            ENDCG
        }

			// Pass to render object as a shadow caster
		Pass{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#pragma target 2.0
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct v2f {
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
    }
    FallBack Off
}
