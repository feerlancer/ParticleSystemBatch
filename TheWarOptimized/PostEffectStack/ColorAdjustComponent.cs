
using System;
using UnityEngine;
public class ColorAdjustComponent : PostEffectComponment
{
    //Color Adjust
    [Range(0.0f, 3.0f)]
    public float Brightness = 1.0f;
    [Range(0.0f, 3.0f)]
    public float Saturation = 1.0f;
    [Range(0.0f, 3.0f)]
    public float Contrast = 1.0f;
    [Range(-1.0f, 1.0f)]
    public float R = 0.0f;
    [Range(-1.0f, 1.0f)]
    public float G = 0.0f;
    [Range(-1.0f, 1.0f)]
    public float B = 0.0f;

    public override string GetShaderMarco()
    {
        return "COLOR_ADJUST";
    }
    public override void Prepare()
    {
        uberMat.SetFloat("_Color_Adjust_Brightness", Brightness);
        uberMat.SetFloat("_Color_Adjust_Contrast", Contrast);
        uberMat.SetFloat("_Color_Adjust_Saturation", Saturation);
        uberMat.SetFloat("_Color_Adjust_R", R);
        uberMat.SetFloat("_Color_Adjust_G", G);
        uberMat.SetFloat("_Color_Adjust_B", B);
    }
}
