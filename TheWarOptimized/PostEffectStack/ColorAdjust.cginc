#ifndef __COLOR_ADJUST__
#define __COLOR_ADJUST__

#include "UnityCG.cginc"

fixed _Color_Adjust_Brightness;  
fixed _Color_Adjust_Saturation;  
fixed _Color_Adjust_Contrast; 
fixed _Color_Adjust_R;
fixed _Color_Adjust_G;
fixed _Color_Adjust_B;

fixed4 ColorAdjust(fixed4 renderTex)
{  
    fixed3 finalColor = renderTex * _Color_Adjust_Brightness;  
    fixed gray = 0.2125 * renderTex.r + 0.7154 * renderTex.g + 0.0721 * renderTex.b;  
    fixed3 grayColor = fixed3(gray, gray, gray);  
    finalColor = lerp(grayColor, finalColor, _Color_Adjust_Saturation);  
    fixed3 avgColor = fixed3(0.5, 0.5, 0.5);  
    finalColor = lerp(avgColor, finalColor, _Color_Adjust_Contrast);  

    return fixed4(finalColor.r + _Color_Adjust_R,finalColor.g+ _Color_Adjust_G,finalColor.b+ _Color_Adjust_B, renderTex.a);  
}
#endif 