float Remap(float value, float From1, float To1, float From2, float To2)
{
	return From2 + (value - From1) * (To2 - From2) / (To1 - From1);
}

sampler2D _FogNoise;

fixed MagicFog(float2 UV)
{
	UV.x *= 6;
	UV.y *= 6;
	fixed4 Color1 = tex2D(_FogNoise, UV + _Time.x * float2(0.2, 0.2) * 2);
	fixed4 Color2 = tex2D(_FogNoise, UV + _Time.x * float2(-0.2, 0.2) * -2);
	fixed4 Color3 = tex2D(_FogNoise, UV + _Time.x * float2(0.2, -0.2) * -2);
	fixed4 Color4 = tex2D(_FogNoise, UV + _Time.x * float2(-0.2, -0.2) * 2);
	fixed result = pow((Color1.r * Color2.g * Color3.b * Color3.a) * 3, 2);
	return result;
}

fixed SteamFog(float2 UV)
{
	UV.x *= 3;
	UV.y *= 3;
	fixed4 Color1 = tex2D(_FogNoise, UV + _Time.x * float2(0.2, 0.17) * 2);
	fixed result = pow(Color1.r,2);
	return result;
}

fixed ScifiFog(float3 worldPos)
{
	return clamp(
		pow(
			(((sin(worldPos.z / .1 + _Time.x * 20) + 1) / 2) *
				((sin(worldPos.z / .2 - _Time.x * 20) + 1) / 2) *
				((sin(worldPos.z / .15 + _Time.x * 14) + 1) / 2))
			+
			(((sin(worldPos.x / .2 - _Time.x * 17) + 1) / 2) *
				((sin(worldPos.x / .22 + _Time.x * 22) + 1) / 2) *
				((sin(worldPos.x / .17 - _Time.x * 19) + 1) / 2))
			, 1.5)
		, 0, .5);
}

//Add your custom effect here:
fixed Custom()
{
	return 1;
}

sampler2D _FowTex;
float4 _FowUV;
fixed4 _FowColor;

//fixed4 ApplyWarFog(fixed4 color, float3 worldPos) {
//	float2 uvFog = worldPos.xz/ _FowUV.xy+ _FowUV.zw;
//	return fixed4(uvFog, 0, 1);
//	color.rgb = lerp(color.rgb * _FowColor.rgb, color.rgb, tex2D(_FowTex, uvFog).rgb);
//	return color;
//}
#define WARFOG_COORDS(idx1) float2 warFogUV : TEXCOORD##idx1;

#define TRANSFER_WARFOG(a_appdata,ObjectVertex) a_appdata.warFogUV = mul(unity_ObjectToWorld, ObjectVertex).xz / _FowUV.xy + _FowUV.zw;

#define APPLY_WARFOG(a_Color,a_v2f) a_Color.rgb = lerp(a_Color.rgb * _FowColor.rgb, a_Color.rgb, tex2D(_FowTex, a_v2f.warFogUV).rgb);