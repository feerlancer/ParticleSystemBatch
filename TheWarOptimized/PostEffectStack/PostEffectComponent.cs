using System;
using System.Collections.Generic;
using UnityEngine;

public class PostEffectComponment : MonoBehaviour
{
    private void Start()
    {
        
    }
    public virtual string GetShaderMarco()
    {
        Debug.Log("Need Implemention!!");
        return "";
    }
    public RenderTexture cameraRT { get; set; }
    public Material uberMat { get; set; }
    private List<Material> createdMaterials = new List<Material>();
    public virtual void Prepare()
    {

    }

    public virtual void ReleasePerRender()
    {

    }
    protected Material CheckShaderAndCreateMaterial(Shader s, Material m2Create)
    {
        if (!s)
        {
            Debug.Log("Missing shader in " + ToString());
            enabled = false;
            return null;
        }

        if (s.isSupported && m2Create && m2Create.shader == s)
            return m2Create;

        if (!s.isSupported)
        {
            Debug.Log("The shader " + s.ToString() + " on effect " + ToString() + " is not supported on this platform!");
            return null;
        }

        m2Create = new Material(s);
        createdMaterials.Add(m2Create);
        m2Create.hideFlags = HideFlags.DontSave;

        return m2Create;
    }
    protected bool CheckSupport(bool needDepth)
    {
        if (!SystemInfo.supportsImageEffects || !SystemInfo.supportsRenderTextures)
        {
            return false;
        }

        if (needDepth && !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
        {
            return false;
        }

        if (needDepth)
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

        return true;
    }

    void OnDestroy()
    {
        RemoveCreatedMaterials();
    }

    private void RemoveCreatedMaterials()
    {
        while (createdMaterials.Count > 0)
        {
            Material mat = createdMaterials[0];
            createdMaterials.RemoveAt(0);
#if UNITY_EDITOR
            DestroyImmediate(mat);
#else
                Destroy(mat);
#endif
        }
    }
}
