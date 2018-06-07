
			struct appdata_t {
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				half3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				half3 cubenormal : TEXCOORD2;
				WARFOG_COORDS(1)
				SHADOW_COORDS(3)
			};

			sampler2D _MainTex;
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

			half4 _ShadowColor;
			v2f vert(appdata_t v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.cubenormal = mul(UNITY_MATRIX_MV, fixed4(v.normal,0));
				TRANSFER_WARFOG(o, v.vertex);
				TRANSFER_SHADOW(o);
				return o;

			}

			fixed4 frag(v2f i) : SV_Target
			{

				fixed4 col = tex2D(_MainTex, i.texcoord);
				fixed4 masks = tex2D(_MaskTex, i.texcoord);
				fixed emmask = masks.g;
				fixed teamcolormask = masks.r;
				//fixed spmask = masks.b;
				//fixed basemask = 1.0 - spmask;
				fixed changemask = (col.r * 0.3 + col.g * 0.59 + col.b* 0.11 + 0.2)*teamcolormask;

				fixed4 fincol = col*(1 - teamcolormask) + changemask*_TeamColor;

				fixed4 cube = texCUBE(_reflect_df, i.cubenormal);

				fixed4 time = _Time + _TimeEditor;
				fixed4 speed = (sin(time.g * _speed)*0.5 + 1)*_max;

				//fixed4 c = fixed4(cube*spmask*_r*(cube - .2)*fincol.rgb + cube*basemask*_b + 1.5*fincol.rgb*(cube)+fincol.rgb*emmask*speed, 1);
				fixed4 c = fixed4(cube*_b + 1.5*fincol.rgb*(cube)+fincol.rgb*emmask*speed, 1);

				fixed4 shadowCol = SHADOW_ATTENUATION(i)<0.5 ? 0.3 : 1;
				c.rgb = lerp(c.rgb, c.rgb * _ShadowColor.rgb, 1 - shadowCol.r);
				c = c * shadowCol;
				APPLY_WARFOG(c, i);
				return c;
			}