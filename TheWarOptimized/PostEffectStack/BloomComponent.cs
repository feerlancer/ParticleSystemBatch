
using System;
using UnityEngine;
[RequireComponent (typeof(Camera))]
public class BloomComponent : PostEffectComponment
{
    public enum Resolution
	{
        Low = 0,
        High = 1,
    }

    public enum BlurType
	{
        Standard = 0,
        Sgx = 1,
    }

    [Range(0.0f, 1.5f)]
    public float threshold = 0.25f;
    [Range(0.0f, 2.5f)]
    public float intensity = 0.75f;

    [Range(0.25f, 5.5f)]
    public float blurSize = 1.0f;

    Resolution resolution = Resolution.Low;

    [Range(1, 4)]
    public int blurIterations = 1;

    public BlurType blurType= BlurType.Standard;

    public Shader fastBloomShader = null;
    private Material fastBloomMaterial = null;
    private RenderTexture ResultRT = null;

    public override string GetShaderMarco()
    {
        return "BLOOM";
    }
    public bool CheckResources ()
	{
        if (!CheckSupport(false)) return false;
        fastBloomMaterial = CheckShaderAndCreateMaterial (fastBloomShader, fastBloomMaterial);
        if (null == fastBloomMaterial) return false;
        return true;
    }

//      void OnDisable ()
	//{
//          if (fastBloomMaterial)
//              DestroyImmediate (fastBloomMaterial);
//      }

    public override void Prepare()
    {
        //threshold = 0.37f;
        //intensity = 0.84f;
        //blurSize = 4.1f;
        //blurIterations = 1;

        if (CheckResources() == false)
        {
            return;
        }
        var source = cameraRT;
        int divider = resolution == Resolution.Low ? 4 : 2;
        float widthMod = resolution == Resolution.Low ? 0.5f : 1.0f;
            
        fastBloomMaterial.SetVector ("_Parameter", new Vector4 (blurSize * widthMod, 0.0f, threshold, intensity));
        source.filterMode = FilterMode.Bilinear;

        var rtW= source.width/divider;
        var rtH= source.height/divider;

        // downsample
        RenderTexture rt = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
        rt.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, rt, fastBloomMaterial,6);
        //Graphics.Blit(source, rt, fastBloomMaterial, isBin?1:6);
        var passOffs = blurType == BlurType.Standard ? 0 : 2;

        for(int i = 0; i < blurIterations; i++)
		{
            fastBloomMaterial.SetVector ("_Parameter", new Vector4 (blurSize * widthMod + (i*1.0f), 0.0f, threshold, intensity));

            // vertical blur
            RenderTexture rt2 = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
            rt2.filterMode = FilterMode.Bilinear;
            Graphics.Blit (rt, rt2, fastBloomMaterial, 2 + passOffs);
            RenderTexture.ReleaseTemporary (rt);
            rt = rt2;

            // horizontal blur
            rt2 = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
            rt2.filterMode = FilterMode.Bilinear;
            Graphics.Blit (rt, rt2, fastBloomMaterial, 3 + passOffs);
            RenderTexture.ReleaseTemporary (rt);
            rt = rt2;
        }
        ResultRT = rt;
        uberMat.SetTexture("_BloomTex",rt);
    }

    public override void ReleasePerRender()
    {
        RenderTexture.ReleaseTemporary(ResultRT);
        ResultRT = null;
    }
}
