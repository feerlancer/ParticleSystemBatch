/*
 *      Author: Starking. 
 *      Version: 17.09.30
 */

using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;

#if UNITY_EDITOR

using UnityEditor;

public class ToolsFogColorBak
{
    [MenuItem("Tools/Mute Fog")]
    static void MuteFog()
    {
        Shader.SetGlobalTexture("_FowTex", null);
        Shader.SetGlobalColor("_FowColor", Color.white);
    }
}

[CustomEditor(typeof(TheWarFogBAK))]
public class TheWarFogEditorBak : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var m_t = target as TheWarFogBAK;

        //if (Application.isPlaying)
        //{
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
                m_t.UpdateFogMask(Random.Range(0, m_t.maskWidth), Random.Range(0, m_t.maskHeight), TheWarFogBAK.FogState.Visible);
            }

            if (GUILayout.Button("开启点(0, 0)"))
            {
                m_t.UpdateFogMask(0, 0, TheWarFogBAK.FogState.Visible,0);
            }
            if (GUILayout.Button("关闭点(0, 0)"))
            {
                m_t.UpdateFogMask(0, 0, TheWarFogBAK.FogState.Invisible);
            }
        if (GUILayout.Button("开启点(max, max)"))
            {
                m_t.UpdateFogMask(m_t.maskWidth - 1, m_t.maskHeight - 1, TheWarFogBAK.FogState.Visible);
            }

            if (GUILayout.Button("随机点半开"))
            {
                m_t.UpdateFogMask(Random.Range(0, m_t.maskWidth), Random.Range(0, m_t.maskHeight), TheWarFogBAK.FogState.Half);
            }

            if (GUILayout.Button("随机点关闭"))
            {
                m_t.UpdateFogMask(Random.Range(0, m_t.maskWidth), Random.Range(0, m_t.maskHeight), TheWarFogBAK.FogState.Invisible);
            }
        //}

        if (GUILayout.Button("改变雾色"))
        {
            m_t.SetGlobalFogColor();
        }
    }
}

#endif

public class TheWarFogBAK : MonoBehaviour
{
    private static Color m_FogBaseMask = new Color(1, 1, 1, 0);

    [Tooltip("迷雾色")]
    public Color fogColor = Color.black;
    [Tooltip("网格列数")]
    public int maskWidth = 32;
    [Tooltip("网格行数")]
    public int maskHeight = 32;
    //采样
    [Range(1, 12), Tooltip("采样精度")]
    public int upSampleLevel = 3;
    [Range(0, 8), Tooltip("模糊强度")]
    public int blur = 3;
    [Range(0.0f, 1.0f), Tooltip("模糊散度")]
    public float blurSpread = 0.6f;

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
    [Tooltip("模糊着色器")]
    public Shader blurShader = null;

    private Material m_Material = null;
    public Material material
    {
        get
        {
            if (m_Material == null)
            {
                if (!blurShader)
                    blurShader = Shader.Find("Hidden/BlurEffectConeTap");

                m_Material = new Material(blurShader);
                m_Material.hideFlags = HideFlags.DontSave;
            }
            return m_Material;
        }
    }

    //雾网格mask信息
    private byte[,] FogMaskMap { get; set; }
    public Texture2D FogMaskTexture { get; private set; }
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

    public static TheWarFogBAK instance;
    public void Awake()
    {
        this.maskWidth = LuaFramework.AppConst.mapCount;
        this.maskHeight = LuaFramework.AppConst.mapCount;
        this.gridWidth = LuaFramework.AppConst.mapSize;
        this.gridHeight = LuaFramework.AppConst.mapSize;
        //maskWidth = 100;
        //maskHeight = 100;
        //gridWidth = 1000;
        //gridHeight = 1000;
        if (!instance)
            instance = this;
        else
            Destroy(this);
    }

    public void Start()
    {
        //TODO
        //CreateFogMask();
    }

    private void OnDisable()
    {
        if (FogMaskRenderTexture) RenderTexture.ReleaseTemporary(FogMaskRenderTexture);
        FogMaskRenderTexture = null;
        if (material) Destroy(material);
    }

    #region 创建雾
    //快速创建雾mask
    public void CreateFogMask()
    {
        FogMaskMap = new byte[maskWidth, maskHeight];
        CreateFogMask(FogMaskMap);
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
    }

    //创建雾
    private void CreateFogMask(byte[,] fogMaskMap, int width, int height)
    {
        FogMaskMap = fogMaskMap;
        if (!FogMaskTexture || m_UpSample != 1 << upSampleLevel)
            CreateFogMaskTexture(width, height);
        else
            UpdateFogMaskTexture(width, height);
    }
    //TODO test
    private void OnPreRender()
    {
        if (m_Update)
        {
            UpdateFogMask(1, 3, FogState.Visible, 0);
            ApplyTextureAndSendToGlobal();
        }
    }

    #endregion

    #region 更新雾
    //给定数组更新雾
    public void UpdateFogMask(byte[,] fogMaskMap, float fadeTime = 0.3f)
    {
        if (fogMaskMap == null)
            return;//无效的mask图

        //尺寸不一或者无纹理时转为新建纹理
        if (!MatchSize(fogMaskMap) || !FogMaskTexture)
        {
            CreateFogMask(fogMaskMap);
            return;
        }

        if (!Created)
            return;//没有正确生成mask图

        if (fadeTime > 0)
        {
            //记录变化值起始
            byte[,] fogMapValueFrom = new byte[m_FogMaskWidth, m_FogMaskHeight];
            for (int y = 0; y < m_FogMaskHeight; y++)
            {
                for (int x = 0; x < m_FogMaskWidth; x++)
                {
                    fogMapValueFrom[x, y] = FogMaskMap[x, y];
                }
            }
            DOTween.To(delegate (float v)
            {
                for (int y = 0; y < m_FogMaskHeight; y++)
                {
                    for (int x = 0; x < m_FogMaskWidth; x++)
                    {
                        FogMaskMap[x, y] = (byte)Mathf.Lerp(fogMapValueFrom[x, y], fogMaskMap[x, y], v);
                        UpdatePixel(x, y, FogMaskMap[x, y]);
                    }
                }
                ApplyTextureAndSendToGlobal();
            }, 0, 1, fadeTime).SetEase(Ease.Linear);
        }
        else
        {
            FogMaskMap = fogMaskMap;
            UpdateFogMaskTexture(m_FogMaskWidth, m_FogMaskHeight);
        }
    }

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

        if (FogMaskMap[x, y] == colorTo)
            return;//状态相同则跳过

        if (fadeTime > 0)
        {
            byte colorFrom = FogMaskMap[x, y];
            DOTween.To(delegate (float v)
            {
                FogMaskMap[x, y] = (byte)v;
                UpdatePixel(x, y, v);
                ApplyTextureAndSendToGlobal();
            }, colorFrom, colorTo, fadeTime).SetEase(Ease.Linear);
        }
        else
        {
            FogMaskMap[x, y] = colorTo;
            UpdatePixel(x, y, colorTo);
            ApplyTextureAndSendToGlobal();
        }
    }

    //给定数组创建雾mask纹理
    private void CreateFogMaskTexture(int width, int height)
    {
        m_UpSample = 1 << upSampleLevel;
        Debug.Log("UpSample ==> " + m_UpSample);
        m_TexWidth = width * m_UpSample;
        m_TexHeight = height * m_UpSample;
        Debug.Log("TexSize ==> " + m_TexWidth + " x " + m_TexHeight);
        if (m_TexWidth > 4096 || m_TexHeight > 4096)
        {
            Debug.Log("texture is to big!");
            return;
        }

        if (FogMaskTexture) Destroy(FogMaskTexture);

        FogMaskTexture = new Texture2D(m_TexWidth, m_TexHeight, TextureFormat.RGB24, false);
        m_Created = true;
        UpdateFogMaskTexture(width, height);
    }

    //给定数组更新雾mask纹理
    private void UpdateFogMaskTexture(int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                UpdatePixel(x, y, FogMaskMap[x, y]);
            }
        }
        ApplyTextureAndSendToGlobal();
    }

    //应用纹理修改，模糊处理并发布到公共着色器通道
    private void ApplyTextureAndSendToGlobal()
    {
        FogMaskTexture.Apply(false);
        PostProcessingTexture();
        SetGlobalFogMap();
    }

    //重绘像素点
    private void UpdatePixel(int x, int y, float mapValue)
    {
        //单格采样宽度upsample * upsample
        for (int gy = 0; gy < m_UpSample; gy++)
        {
            for (int gx = 0; gx < m_UpSample; gx++)
            {
                FogMaskTexture.SetPixel(x * m_UpSample + gx, y * m_UpSample + gy, (mapValue / 255) * m_FogBaseMask);
            }
        }
    }

    #region 后处理模糊
    //进行全图模糊
    private void PostProcessingTexture()
    {
        RenderTexture buffer = RenderTexture.GetTemporary(m_TexWidth, m_TexHeight, 0);

        DownSample4x(FogMaskTexture, buffer);

        for (int i = 0; i < blur; i++)
        {
            RenderTexture buffer2 = RenderTexture.GetTemporary(m_TexWidth, m_TexHeight, 0);
            FourTapCone(buffer, buffer2, i);
            RenderTexture.ReleaseTemporary(buffer);
            buffer = buffer2;
        }

        if (!FogMaskRenderTexture)
            FogMaskRenderTexture = RenderTexture.GetTemporary(m_TexWidth, m_TexHeight, 0);

        Graphics.Blit(buffer, FogMaskRenderTexture);

        RenderTexture.ReleaseTemporary(buffer);
    }

    // 降采样迭代
    public void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
    {
        float off = 0.5f + iteration * blurSpread;
        Graphics.BlitMultiTap(source, dest, material,
            new Vector2(-off, -off),
            new Vector2(-off, off),
            new Vector2(off, off),
            new Vector2(off, -off)
            );
    }

    // 先将纹理降采样到1/4
    private void DownSample4x(Texture source, RenderTexture dest)
    {
        float off = 1.0f;
        Graphics.BlitMultiTap(source, dest, material,
            new Vector2(-off, -off),
            new Vector2(-off, off),
            new Vector2(off, off),
            new Vector2(off, -off)
            );
    }
    #endregion

    #endregion

    #region 高斯模糊（用了后处理这块可以不用了，先保留吧）

    private const float Math_E = 2.718281828459f;

    //快速存放的高斯分布值
    private static float[,] m_GaussianMap = new float[4, 4]
    {
        { 0.159f, 0.097f, 0.022f, 0.002f },
        { 0.097f, 0.059f, 0.013f, 0.001f },
        { 0.022f, 0.013f, 0.003f, 0.000f },
        { 0.002f, 0.001f, 0.000f, 0.000f }
    };
    public bool m_Update=false;

    private static float[,] GaussianMap
    {
        get
        {
            return m_GaussianMap;
        }
    }

    //高斯分布 x0 y0 为距离中心的距离，返回当前点使用到的中心权重，距离越远越使用不到
    private static float GetGaussianWeight(float xO, float yO)
    {
        return Mathf.Pow(Math_E, -(xO * xO + yO * yO) * 0.5f) / (2 * Mathf.PI);
    }

    //快速获得高斯权重，简化到4*4矩阵
    private static float FastGetWeight(int xDis, int yDis, int blurDis)
    {
        xDis = Mathf.Abs(xDis);
        yDis = Mathf.Abs(yDis);
        xDis = (int)(Mathf.InverseLerp(0, blurDis, xDis) * 3);
        yDis = (int)(Mathf.InverseLerp(0, blurDis, yDis) * 3);
        return GaussianMap[xDis, yDis];
    }

    //获得该点最终模糊值，由自身的外围矩阵依次运算并合成，例如4*4 依次对任一点进行该点的插值运算
    private float GaussianBlur(int px, int py, int blurLength)//给定当前纹理坐标点
    {
        float v = 0;
        for (int hDis = -blurLength; hDis <= blurLength; hDis++)//左下开始，上移
        {
            for (int wDis = -blurLength; wDis <= blurLength; wDis++)//右移
            {
                //获得距离x+w,y+h的点对x,y值的影响
                v += FastGetWeight(wDis, hDis, blurLength)
                    * FogMaskTexture.GetPixel(Mathf.Clamp(px + wDis, 0, m_TexWidth - 1), Mathf.Clamp(py + hDis, 0, m_TexHeight - 1)).r;
            }
        }
        return v;
    }
    #endregion

    #region 全局着色器变量更新
    //设置全局雾参数
    public void SetGlobalFogMap()
    {
        if (image) image.texture = FogMaskTexture;
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
