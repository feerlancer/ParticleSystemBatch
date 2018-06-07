using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightingStencil : MonoBehaviour {

    //// Use this for initialization
    //void Start () {

    //}

    //// Update is called once per frame
    //void Update () {

    //}
    [Tooltip("轮廓色")]
    public Color outlineColor = Color.yellow;
    [Range(0.0f, 10.0f), Tooltip("轮廓宽度")]
    public float outlineWidth = 1.7f;

    private GameObject m_HighLightTarget;
    public GameObject HighLightTarget
    {
        get
        {
            return m_HighLightTarget;
        }
        set
        {
            if (m_HighLightTarget != value)
            {
                if (m_HighLightTarget)
                {
                    swapShader(m_HighLightTarget.GetComponentsInChildren<Renderer>(), false);
                }
                m_HighLightTarget = value;
                if (m_HighLightTarget)
                {
                    swapShader(m_HighLightTarget.GetComponentsInChildren<Renderer>(), true);
                }
            }
        }
    }

    private void swapShader(Renderer[] renderers,bool isOutline)
    {
        string postfix = "_OutlineStencil";
        for (int i = renderers.Length - 1; i >= 0; i--)
        {
            for (int j = renderers[i].materials.Length - 1; j >= 0; j--)
            {
                string shaderName = renderers[i].materials[j].shader.name;
                if (isOutline)
                {
                    if(false==shaderName.Contains(postfix))
                    {
                        shaderName += postfix;
                    }
                }
                else
                {
                    shaderName = shaderName.Replace(postfix, "");
                }
                Shader x = Shader.Find(shaderName);
                if(x)
                {
                    renderers[i].materials[j].shader = x;
                    renderers[i].materials[j].SetVector("_OutlineColor", outlineColor);
                    renderers[i].materials[j].SetFloat("_OutlineWidth", outlineWidth);
                }
                else
                {
                    Debug.LogError("Cannot find shader!: "+ shaderName);
                }
            }
              
        }
            
    }
}
