
	//fixed4 _TintColor;
	
	struct appdata_t {
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
	};
	//sampler2D _MainTex;
	//float4 _MainTex_ST;

	UNITY_INSTANCING_CBUFFER_START(MyProperties)
		//UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
		UNITY_DEFINE_INSTANCED_PROP(sampler2D, _MainTex)
	UNITY_INSTANCING_CBUFFER_END

	v2f vert (appdata_t v)
	{
		v2f o;
		UNITY_SETUP_INSTANCE_ID(v);
		o.vertex = UnityObjectToClipPos(v.vertex);
		//o.color =  2.0f *v.color*_TintColor ;
		o.color = 2.0f *v.color;
		//o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
		o.texcoord = v.texcoord;
		return o;
	}

	fixed4 fragParticle (v2f i) : SV_Target
	{
		fixed4 col = i.color*tex2D(_MainTex, i.texcoord);
		return col;
	}
