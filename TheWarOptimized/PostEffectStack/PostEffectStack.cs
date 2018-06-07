//Author ShenJi 2018/Mar/28
using UnityEngine;
using System.Collections;
using System;

public class PostEffectStack : MonoBehaviour {

    public Shader PostEffectStackShader;
    private Material UberMat;
    private PostEffectComponment[] postEffectList;
    private void Start()
    {
        if (null == UberMat)
        {
            UberMat = new Material(PostEffectStackShader);
        }
    }
    void OnEnable()
    {
        postEffectList = GetComponents<PostEffectComponment>();
    }
    void OnDestroy()
    {
        DestroyImmediate(UberMat);
    }

    public void EnableEffect(PostEffectComponment comp)
    {
        comp.enabled = true;
        UberMat.EnableKeyword(comp.GetShaderMarco());
    }
    public void DisableEffect(PostEffectComponment comp)
    {
        comp.enabled = false;
        UberMat.DisableKeyword(comp.GetShaderMarco());
    }

    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {
        UberMat.SetTexture("_MainTex", source);

        foreach(PostEffectComponment effect in postEffectList)
        {
            if (effect.enabled)
            {
                effect.cameraRT = source;
                effect.uberMat = UberMat;
                effect.Prepare();
                UberMat.EnableKeyword(effect.GetShaderMarco());
            }
            else
            {
                UberMat.DisableKeyword(effect.GetShaderMarco());
            }
        }

        Graphics.Blit(source, destination, UberMat);

        foreach (PostEffectComponment effect in postEffectList)
        {
            if (effect.enabled)
            {
                effect.ReleasePerRender();
            }
        }
    }
}  