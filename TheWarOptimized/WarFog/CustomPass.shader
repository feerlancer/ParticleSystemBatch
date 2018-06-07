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
