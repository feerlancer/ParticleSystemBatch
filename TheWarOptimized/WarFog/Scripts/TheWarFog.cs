/*
 *      Author: Starking. 
 *      Version: 17.09.30
 */

using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if UNITY_EDITOR

using UnityEditor;

public class ToolsFogColor
{
    [MenuItem("Tools/Mute Fog")]
    static void MuteFog()
    {
        Shader.SetGlobalTexture("_FowTex", null);
        Shader.SetGlobalColor("_FowColor", Color.white);
    }
}

[CustomEditor(typeof(TheWarFog))]
public class TheWarFogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var m_t = target as TheWarFog;

        if (Application.isPlaying)
        {
            if (m_t.Created)
            {
                string help = string.Format("Map Size({0}x{1})\nTexture Size({2}x{3})\nWorld Size({4}x{5})",
                     m_t.FogMaskWidth,
                     m_t.FogMaskHeight,
                    m_t.TexWidth,
                    m_t.TexHeight,
                    m_t.FogMaskWidth * m_t.gridWidth,
                    m_t.FogMaskHeight * m_t.gridHeight);

                EditorGUILayout.LabelField("", help, "helpbox");
            }

            if (GUILayout.Button("创建mask图"))
            {
                m_t.CreateFogMask();
            }

            if (GUILayout.Button("随机点开启"))
            {
                m_t.UpdateFogMask(Random.Range(0, m_t.maskWidth), Random.Range(0, m_t.maskHeight), TheWarFog.FogState.Visible,0);
            }

            if (GUILayout.Button("开启点(0, 0)"))
            {
                m_t.UpdateFogMask(1, 3, TheWarFog.FogState.Visible,0);
            }
            if (GUILayout.Button("关闭(0, 0)"))
            {
                m_t.UpdateFogMask(1, 3, TheWarFog.FogState.Invisible,0);
            }
            if (GUILayout.Button("开启点(0, 1)"))
            {
                m_t.UpdateFogMask(2, 3, TheWarFog.FogState.Visible);
            }
            if (GUILayout.Button("开启点(max, max)"))
            {
                m_t.UpdateFogMask(m_t.maskWidth - 1, m_t.maskHeight - 1, TheWarFog.FogState.Visible,0);
            }

            if (GUILayout.Button("随机点半开"))
            {
                m_t.UpdateFogMask(Random.Range(0, m_t.maskWidth), Random.Range(0, m_t.maskHeight), TheWarFog.FogState.Half,0);
            }

            if (GUILayout.Button("随机点关闭"))
            {
                m_t.UpdateFogMask(Random.Range(0, m_t.maskWidth), Random.Range(0, m_t.maskHeight), TheWarFog.FogState.Invisible);
            }
        }

        if (GUILayout.Button("改变雾色"))
        {
            m_t.SetGlobalFogColor();
        }
    }
}

#endif

public class TheWarFog : MonoBehaviour
{
    private static Color m_FogBaseMask = new Color(1, 1, 1,1);
    [Tooltip("迷雾色")]
    public Color fogColor = Color.black;
    [Tooltip("网格列数")]
    public int maskWidth = 32;
    [Tooltip("网格行数")]
    public int maskHeight = 32;

    //每格的长宽
    [Tooltip("单格宽度")]
    public int gridWidth = 20;
    [Tooltip("单格长度")]
    public int gridHeight = 20;
    [Tooltip("雾的中心在宽度范围内的偏移(0-1)")]
    public Vector2 fogCenterInWorld = new Vector2(0.5f, 0.5f);
    [Tooltip("网格从中心计算，否则以左下计算")]
    public bool gridInCenter = true;

    public UnityEngine.UI.RawImage image;
    [Tooltip("雾更新着色器")]
    public Shader fogUpdateShader = null;
    public Shader BlitShader = null;

    private Material m_FogUpdateMaterial = null;
    public Material fogUpdateMaterial
    {
        get
        {
            if (m_FogUpdateMaterial == null)
            {
                var s = fogUpdateShader;
                if(!s) s= Shader.Find("Hidden/PartFogRT");
                m_FogUpdateMaterial = new Material(s);
                m_FogUpdateMaterial.hideFlags = HideFlags.DontSave;
            }
            return m_FogUpdateMaterial;
        }
    }

    private CommandBuffer commandBuffer;
    private Texture2D UpdateFogTex;

    private Mesh m_PartUpdateRect;
    private Mesh partUpdateRect
    {
        get
        {
            if (m_PartUpdateRect == null)
            {
                m_PartUpdateRect = new Mesh();

                Vector3[] vertices = new Vector3[4];
                m_PartUpdateRect.vertices = vertices;

                int[] tri = new int[6];
                tri[0] = 0;
                tri[1] = 2;
                tri[2] = 1;
                tri[3] = 2;
                tri[4] = 3;
                tri[5] = 1;
                m_PartUpdateRect.triangles = tri;

                Vector2[] uv = new Vector2[4];
                uv[0] = new Vector2(0, 0);
                uv[1] = new Vector2(1, 0);
                uv[2] = new Vector2(0, 1);
                uv[3] = new Vector2(1, 1);
                m_PartUpdateRect.uv = uv;
            }
            return m_PartUpdateRect;
        }
    }
    //雾网格mask信息
    private byte[,] FogMaskMap { get; set; }
    public Texture2D FogMaskTextureForInit { get; private set; }
    private RenderTexture FogMaskRenderTexture { get; set; }
    private Vector4 m_GridSize;
    private int m_FogMaskWidth;
    public int FogMaskWidth { get { return m_FogMaskWidth; } }
    private int m_FogMaskHeight;
    public int FogMaskHeight { get { return m_FogMaskHeight; } }
    private int m_TexWidth;
    public int TexWidth { get { return m_TexWidth; } }
    private int m_TexHeight;
    public int TexHeight { get { return m_TexHeight; } }

    private int m_UpSample;
    public int UpSample { get { return m_UpSample; } }

    private bool m_Created;
    public bool Created
    {
        get { return m_Created; }
    }

    const byte INVISIBLE = 0;
    const byte HLAF = 160;
    const byte VISIBLE = 255;

    public enum FogState : byte
    {
        Invisible = INVISIBLE,
        Half = HLAF,
        Visible = VISIBLE
    }

    public static TheWarFog instance;

    public void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(this);
    }

    public void Init(int maskW, int maskH, int gridW, int gridH)
    {
        this.maskWidth = maskW;
        this.maskHeight = maskH;
        this.gridWidth = gridW;
        this.gridHeight = gridH;
        CreateFogMask();
    }

    public void Start()
    {
       // CreateFogMask();
    }
    public void CreateFogMask(int mapRowCount,int mapColumnCount)
    {
        ReleaseResource();
        this.maskWidth = mapRowCount;
        this.maskHeight = mapColumnCount;
        CreateFogMask();
    }
    void SetFogMask(int x, int y, byte value)
    {
        FogMaskMap[x, FogMaskMap.GetUpperBound(1)-y] = value;
        //FogMaskMap[x, y] = value;
    }
    byte GetFogMask(int x, int y)
    {
        return FogMaskMap[x, FogMaskMap.GetUpperBound(1) - y];
        //return FogMaskMap[x,y];
    }
    private void OnDestroy()
    {
        ReleaseResource();
    }
    public void ReleaseResource()
    {
        if (FogMaskRenderTexture) RenderTexture.ReleaseTemporary(FogMaskRenderTexture);
        FogMaskRenderTexture = null;
        if (m_FogUpdateMaterial) Destroy(m_FogUpdateMaterial);
        m_FogUpdateMaterial = null;
        if (null != UpdateFogTex) Destroy(UpdateFogTex);
        UpdateFogTex = null;
        if (null != commandBuffer) commandBuffer.Release();
        commandBuffer = null;
        if (FogMaskTextureForInit) Destroy(FogMaskTextureForInit);
        FogMaskTextureForInit = null;
    }

    #region 创建雾
    //快速创建雾mask
    public void CreateFogMask()
    {
        FogMaskMap = new byte[maskWidth, maskHeight];
        //TODO t4est
        //SetFogMask(0, 0, 255);
        //SetFogMask(1, 3, 255);
        //SetFogMask(1, 2, 255);
        //SetFogMask(2, 3, 255);
        //SetFogMask(3, 3, 255);
        CreateFogMask(FogMaskMap);
    }
    void drawFogTexToRT(int mapUnit_X,int mapUnit_Y)
    {
        float x = 2* ((float)mapUnit_X-1) / (float)m_FogMaskWidth-1;
        float y = 2* ((float)mapUnit_Y-1) / (float)m_FogMaskHeight-1;
        float x1 = 2*((float)mapUnit_X+2) / (float)m_FogMaskWidth-1;
        float y1 = 2*((float)mapUnit_Y+2) / (float)m_FogMaskHeight-1;
        //y = -y;
        //y1 = -y1;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x, y, 0);
        vertices[1] = new Vector3(x1, y, 0);
        vertices[2] = new Vector3(x, y1, 0);
        vertices[3] = new Vector3(x1, y1, 0);
        partUpdateRect.vertices = vertices;

        if (!UpdateFogTex)
        {
            UpdateFogTex = new Texture2D(3, 3, TextureFormat.RGB24, false);
            UpdateFogTex.filterMode = FilterMode.Point;
            UpdateFogTex.wrapMode = TextureWrapMode.Clamp;
        }

        for(int i = -1; i<=1;++i)
        {
            for (int j = -1; j <= 1; ++j)
            {
                var ti = (mapUnit_X +i)> 0 ? (mapUnit_X + i) : 0;
                ti = ti < m_FogMaskWidth ? ti : (m_FogMaskWidth-1);
                var tj = (mapUnit_Y+j) > 0 ? (mapUnit_Y + j) : 0;
                tj = tj < m_FogMaskHeight ? tj : (m_FogMaskHeight-1);
                //var mapValue = FogMaskMap[ti, tj];
                var mapValue = GetFogMask(ti, tj);
                UpdateFogTex.SetPixel(i+1, j+1, mapValue / 255f * m_FogBaseMask);
            }
        }
        UpdateFogTex.Apply(false);

        fogUpdateMaterial.SetTexture("_MainTex", UpdateFogTex);
        if (commandBuffer == null)
        {
            commandBuffer = new CommandBuffer();
            commandBuffer.SetRenderTarget(FogMaskRenderTexture);
            commandBuffer.DrawMesh(partUpdateRect, Matrix4x4.identity, fogUpdateMaterial);
        }
        Graphics.ExecuteCommandBuffer(commandBuffer);

    }

    //给定数组创建雾
    public void CreateFogMask(byte[,] fogMaskMap)
    {
        if (fogMaskMap == null)
            return;//无效的mask图

        if (!GetMapSize(fogMaskMap, out m_FogMaskWidth, out m_FogMaskHeight))
            return;//尺寸无效

        CreateFogMask(fogMaskMap, m_FogMaskWidth, m_FogMaskHeight);

        int worldWidth = m_FogMaskWidth * gridWidth;//在世界的宽度X轴
        int worldHeight = m_FogMaskHeight * gridHeight;//在世界的纵深Z轴
        float fogCenterPerGirdX = fogCenterInWorld.x;
        float fogCenterPerGirdY = fogCenterInWorld.y;
        if (gridInCenter)
        {
            fogCenterPerGirdX -= gridWidth * 0.5f / worldWidth;
            fogCenterPerGirdY -= gridHeight * 0.5f / worldHeight;
        }

        m_GridSize.Set(worldWidth, worldHeight, 0.5f - fogCenterPerGirdX, 0.5f - fogCenterPerGirdY);

        SetGlobalFogSize();
        SetGlobalFogColor();

        FogMaskTextureForInit.Apply(false);
        PostProcessingTexture();
        SetGlobalFogMap();
    }

    //创建雾
    private void CreateFogMask(byte[,] fogMaskMap, int width, int height)
    {
        FogMaskMap = fogMaskMap;
        //if (!FogMaskTextureForInit)
        CreateFogMaskTexture(width, height);
    }
    #endregion

    #region 更新雾
    //单独更新某格雾
    public void UpdateFogMask(int x, int y, FogState state, float fadeTime = 0.3f)
    {
        if (FogMaskMap == null)
            return;//无效的mask图

        if (!Created)
            return;//没有正确生成mask图

        if (x >= m_FogMaskWidth || y >= m_FogMaskHeight)
            return;//越界

        byte colorTo = (byte)state;
        //TODO
        if (FogMaskMap[x, y] == colorTo)
            return;//状态相同则跳过

        if (fadeTime > 0)
        {
            //byte colorFrom = FogMaskMap[x, y];
            byte colorFrom = GetFogMask(x, y);
            DOTween.To(delegate (float v)
            {
                //FogMaskMap[x, y] = (byte)v;
                SetFogMask(x, y,(byte)v);
                UpdatePixel(x, y, v);
                ApplyTextureAndSendToGlobal();
            }, colorFrom, colorTo, fadeTime).SetEase(Ease.Linear);
        }
        else
        {
            //FogMaskMap[x, y] = colorTo;
            SetFogMask(x, y, colorTo);
            UpdatePixel(x, y, colorTo);
            ApplyTextureAndSendToGlobal();
        }
    }

    //给定数组创建雾mask纹理
    private void CreateFogMaskTexture(int width, int height)
    {
        m_TexWidth = width * 6;
        m_TexHeight = height * 6;

        Debug.Log("TexSize ==> " + m_TexWidth + " x " + m_TexHeight);
        if (FogMaskTextureForInit) Destroy(FogMaskTextureForInit);
        //TODO format
        //FogMaskTextureForInit = new Texture2D(m_TexWidth, m_TexHeight, TextureFormat.RGB24, false);
        FogMaskTextureForInit = new Texture2D(width, height, TextureFormat.RGB24, false);
        m_Created = true;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //var mapValue = FogMaskMap[x, y];
                var mapValue = GetFogMask(x, y);
                FogMaskTextureForInit.SetPixel(x, y, mapValue / 255f * m_FogBaseMask);
            }
        }
    }

    //应用纹理修改，模糊处理并发布到公共着色器通道
    private void ApplyTextureAndSendToGlobal()
    {
        SetGlobalFogMap();
    }

    //重绘像素点
    private void UpdatePixel(int x, int y, float mapValue)
    {
        drawFogTexToRT(x,y);
    }

    //TODO test
    //private void Update()
    //{
    //    //UpdateFogMask(1, 3, FogState.Invisible, 0);
    //    //UpdateFogMask(1, 2, FogState.Invisible, 0);
    //    //UpdateFogMask(2, 3, FogState.Invisible, 0);
    //    //UpdateFogMask(3, 3, FogState.Invisible, 0);

    //    UpdateFogMask(0, 0, FogState.Visible, 0);
    //    UpdateFogMask(1, 2, FogState.Visible, 0);
    //    UpdateFogMask(1, 3, FogState.Visible, 0);
    //    UpdateFogMask(2, 3, FogState.Visible, 0);
    //    UpdateFogMask(3, 3, FogState.Visible, 0);
    //    //UpdateFogMask(10, 10, FogState.Visible, 0);
    //    ApplyTextureAndSendToGlobal();
    //}
    private void PostProcessingTexture()
    {
        if (null==FogMaskRenderTexture)
        {
            FogMaskRenderTexture = RenderTexture.GetTemporary(m_TexWidth, m_TexHeight, 0);
            FogMaskRenderTexture.wrapMode = TextureWrapMode.Clamp;
            FogMaskRenderTexture.filterMode = FilterMode.Bilinear;
            FogMaskRenderTexture.useMipMap = false;
        }
        FogMaskTextureForInit.filterMode = FilterMode.Point;
        Graphics.Blit(FogMaskTextureForInit, FogMaskRenderTexture);
        Destroy(FogMaskTextureForInit);
        FogMaskTextureForInit = null;
    }
    #endregion


    #region 全局着色器变量更新
    //设置全局雾参数
    public void SetGlobalFogMap()
    {
        if (image) image.texture = FogMaskTextureForInit;
        if (FogMaskRenderTexture)
            Shader.SetGlobalTexture("_FowTex", FogMaskRenderTexture);
    }

    //设置全局雾尺寸
    public void SetGlobalFogSize()
    {
        Shader.SetGlobalVector("_FowUV", m_GridSize);
    }

    //设置全局雾颜色
    public void SetGlobalFogColor()
    {
        Shader.SetGlobalColor("_FowColor", fogColor);
    }
    #endregion

    #region 辅助方法
    //获得数组尺寸
    private bool GetMapSize(byte[,] fogMaskMap, out int width, out int height)
    {
        width = fogMaskMap.GetUpperBound(0) + 1;
        height = fogMaskMap.GetUpperBound(1) + 1;

        if (width < 2 || height < 2)
            return false; ;//无效的尺寸

        return true;
    }

    //新尺寸是否有变化
    private bool MatchSize(byte[,] newMap)
    {
        return newMap != null
            && newMap.GetUpperBound(0) + 1 == m_FogMaskWidth
            && newMap.GetUpperBound(1) + 1 == m_FogMaskHeight;
    }
    #endregion

}
