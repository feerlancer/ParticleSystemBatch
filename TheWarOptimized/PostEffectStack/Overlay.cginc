#ifndef __OVERLAY__
#define __OVERLAY__

half4 OverlayAddSub (v2f i){
	half4 toAdd = tex2D(_Overlay, i.uv[0]) * _Intensity;
	return tex2D(_MainTex, i.uv[1]) + toAdd;
}

half4 OverlayMultiply (v2f i){
	half4 toBlend = tex2D(_Overlay, i.uv[0]) * _Intensity;
	return tex2D(_MainTex, i.uv[1]) * toBlend;
}	
		
half4 OverlayScreen (v2f i){
	half4 toBlend =  (tex2D(_Overlay, i.uv[0]) * _Intensity);
	return 1-(1-toBlend)*(1-(tex2D(_MainTex, i.uv[1])));
}

half4 OverlayOverlay (v2f i){
	half4 m = (tex2D(_Overlay, i.uv[0]));// * 255.0;
	half4 color = (tex2D(_MainTex, i.uv[1]));//* 255.0;

	// overlay blend mode
	//color.rgb = (color.rgb/255.0) * (color.rgb + ((2*m.rgb)/( 255.0 )) * (255.0-color.rgb));
	//color.rgb /= 255.0; 
		
	/*
if (Target > ½) R = 1 - (1-2x(Target-½)) x (1-Blend)
if (Target <= ½) R = (2xTarget) x Blend		
	*/
	
	float3 check = step(half3(0.5,0.5,0.5), color.rgb);
	float3 result = 0;
	
		result =  check * (half3(1,1,1) - ( (half3(1,1,1) - 2*(color.rgb-0.5)) *  (1-m.rgb))); 
		result += (1-check) * (2*color.rgb) * m.rgb;
	
	return half4(lerp(color.rgb, result.rgb, (_Intensity)), color.a);
}

half4 OverlayAlphaBlend (v2f i){
	half4 toAdd = tex2D(_Overlay, i.uv[0]) ;
	return lerp(tex2D(_MainTex, i.uv[1]), toAdd, toAdd.a * _Intensity);
}	