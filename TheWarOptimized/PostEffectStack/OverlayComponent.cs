
using System;
using UnityEngine;
public class OverlayComponent : PostEffectComponment
{
    public float Intensity = 1.0f;
    public Texture2D OverlayTex = null;
    public override string GetShaderMarco()
    {
        return "OVERLAY";
    }
    public override void Prepare()
    {
        uberMat.SetFloat("_Overlay_Intensity", Intensity);
        uberMat.SetTexture("_OverlayTex", OverlayTex);
    }
}
