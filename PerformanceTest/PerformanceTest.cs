using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class PerformanceTest : MonoBehaviour
{
    public List<GameObject> BuildingPrefabs = new List<GameObject>();
    public List<GameObject> EffectsPrefabs = new List<GameObject>();
    public List<GameObject> UnitPrefabs = new List<GameObject>();
    public List<GameObject> HeroPrefabs = new List<GameObject>();
    public List<GameObject> WeaponsPrefabs = new List<GameObject>();

    //private int HeroCount = 4;
    //private int WeaponCount = 4;
    //private int BuildingCount = 15;
    //private int EffectsCount = 110;
    //private int UnitsCount = 150;

    //private int HeroCount = 4;
    //private int WeaponCount = 4;
    //private int BuildingCount = 12;
    //private int EffectsCount = 30;
    //private int UnitsCount = 20;
    private int HeroCount = 0;
    private int WeaponCount = 0;
    private int BuildingCount = 0;
    private int EffectsCount = 100;
    private int UnitsCount = 0;
    string prefixPath = "Assets/GameResources/Game/";

    int countPerRow = 10;

    private Transform buildingRoot = null;
    private Transform effectsRoot = null;
    private Transform heroRoot = null;
    private Transform unitsRoot = null;
    private Transform weaponsRoot = null;
    private GameObject mainCam = null;
    bool isShowUI = true;
    private GameObject effectObj = null;

    GameObject UnitWithOutline;
    bool isRTT = false;

    float widgetWidth;
    float widgetHeight;
    float gap;
    //Declare these in your class
    int m_frameCounter = 0;
    float m_timeCounter = 0.0f;
    float m_lastFramerate = 0.0f;
    public float m_refreshTime = 0.5f;

    // Update is called once per frame
    void Update()
    {
        if (m_timeCounter < m_refreshTime)
        {
            m_timeCounter += Time.deltaTime;
            m_frameCounter++;
        }
        else
        {
            //This code will break if you set your m_refreshTime to 0, which makes no sense.
            m_lastFramerate = (float)m_frameCounter / m_timeCounter;
            m_frameCounter = 0;
            m_timeCounter = 0.0f;
        }
    }


    void Start()
    {
        float sWidth = 1920;
        float sHeight = 1080;
        float ratio = sWidth / 2560f;
        Screen.SetResolution((int)sWidth, (int)sHeight, true);
        widgetWidth = ratio * 230;
        widgetHeight = ratio * 180;
        gap = ratio * 10;

        buildingRoot = GameObject.Find("BuildingRoot").transform;
        effectsRoot = GameObject.Find("EffectsRoot").transform;
        //effectsRoot.position = new Vector3(0, 0, 80);
        heroRoot = GameObject.Find("HeroRoot").transform;
        unitsRoot = GameObject.Find("UnitsRoot").transform;
        weaponsRoot = GameObject.Find("WeaponsRoot").transform;

        //init
        mainCam = GameObject.Find("Camera-Main");
        effectObj = GameObject.Find("EffectsRoot");
        //StartCoroutine(GenerateObjects(EffectsPrefabs, effectsRoot, EffectsCount, 10, 7));
        //StartCoroutine(GenerateObjects(UnitPrefabs, unitsRoot, UnitsCount,10));
        StartCoroutine(GenerateObjects(EffectsPrefabs, effectsRoot, EffectsCount, 4, 7));
        StartCoroutine(GenerateObjects(UnitPrefabs, unitsRoot, UnitsCount));
        StartCoroutine(GenerateObjects(BuildingPrefabs, buildingRoot, BuildingCount, 20, 10, 20));
        StartCoroutine(GenerateObjects(HeroPrefabs, heroRoot, HeroCount, 20, 10, 20));
        StartCoroutine(GenerateObjects(WeaponsPrefabs, weaponsRoot, WeaponCount, 20, 10, 20));
    }

    Rect GetWidgetRectBottom(int Row, int column)
    {
        return new Rect(column * (widgetWidth+gap), Screen.height - (widgetHeight+gap) * (Row+1), widgetWidth, widgetHeight);
    }
    Rect GetWidgetRectTop(int Row,int column)
    {
        return new Rect(column * (widgetWidth+gap), (widgetHeight + gap) * Row, widgetWidth, widgetHeight);
    }
    int MSAA = 0;
    void SwitchMSAA()
    {
        if(0==MSAA)
        {
            MSAA = 2;
        }
        else
        {
            MSAA *= 2;
            if (MSAA > 8)MSAA = 0;
        }
        QualitySettings.antiAliasing = MSAA;
    }
    void OnGUI()
    {
        GUIStyle fontStyle = new GUIStyle();
        fontStyle.normal.background = null;    //设置背景填充  
        fontStyle.normal.textColor = new Color(1, 1, 0);   //设置字体颜色  
        fontStyle.fontSize = 40;       //字体大小  
        GUI.Label(new Rect(0, 50, 1000, 100), "Accurate FPS: " + m_lastFramerate, fontStyle);
        GUI.Label(new Rect(0, 100, 1000, 100), "Avg FPS: " + 1.0f / Time.deltaTime, fontStyle);

        if (GUI.Button(GetWidgetRectTop(0, 3), !effectObj.GetActive() ? "Effect ON" : "Effect OFF"))
        {
            effectObj.SetActive(!effectObj.GetActive());
        }

        //if (GUI.Button(GetWidgetRectTop(0, 3), isStackOn ? "Stack ON" : "Stack OFF"))
        //{
        //    SwitchScript(mainCam.GetComponent<PostEffectStack>(), ref isStackOn);
        //}
        //QuickPostEffectStackWidget(0, 4, mainCam.GetComponent<BloomComponent>(), ref isStackBloomOn);
        //QuickPostEffectStackWidget(0, 5, mainCam.GetComponent<ColorAdjustComponent>(), ref isStackColorAdjustOn);
        //QuickPostEffectStackWidget(0, 6, mainCam.GetComponent<OverlayComponent>(), ref isStackOverlayOn);

        //if (GUI.Button(GetWidgetRectBottom(0, 0), "迷雾" ))
        //{
        //    mainCam.GetComponent<TheWarFog>().UpdateFogMask(1, 3, TheWarFog.FogState.Visible, 0);
        //}

        //if (GUI.Button(GetWidgetRectBottom(0, 1), "原始迷雾"))
        //{
        //    mainCam.GetComponent<TheWarFogBAK>().CreateFogMask();
        //}
        //if (GUI.Button(GetWidgetRectBottom(1, 1), mainCam.GetComponent<TheWarFogBAK>().m_Update ? "更新开" : "guan"))
        //{
        //    mainCam.GetComponent<TheWarFogBAK>().m_Update = !mainCam.GetComponent<TheWarFogBAK>().m_Update;
        //}

        //if (GUI.Button(GetWidgetRectBottom(0, 0), ShadowOn ? "阴影 开" : "阴影 关"))
        //{
        //    SwitchShadow();
        //}

        //if (GUI.Button(GetWidgetRectTop(0, 1), "生成军队"))
        //{
        //    StartCoroutine(GenerateUnits(UnitPrefabs));
        //}
        //if (GUI.Button(GetWidgetRectTop(1, 1), "清除军队"))
        //{
        //    ClearUnits();
        //}

    }

    void SwitchScript(MonoBehaviour x,ref bool isXOn)
    {
        isXOn = !isXOn;
        x.enabled = isXOn;
    }



    List<string> GetPrefabNamesInFolder(string folderName)
    {
        List<string> fileNames = new List<string>();
        if (Directory.Exists(folderName))
        {
            DirectoryInfo direction = new DirectoryInfo(folderName);
            FileInfo[] files = direction.GetFiles("*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Name.EndsWith(".meta"))
                {
                    continue;
                }
                if (files[i].Name.EndsWith(".prefab"))
                {
                    fileNames.Add(files[i].Name);
                }
            }
        }
        return fileNames;
    }

    void LoadPrefabs(List<GameObject> resultList, string folderName, List<string> prefabNames)
    {
#if UNITY_EDITOR
        resultList.Clear();
        for (int i = 0; i < prefabNames.Count; ++i)
        {
            string path = folderName + "/" + prefabNames[i];
            Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (obj == null) return;
            GameObject go = obj as GameObject;
            if (go == null) return;
            resultList.Add(go);
        }
#endif
    }

    void ClearUnits()
    {
        foreach (Transform item in unitsRoot.transform)
        {
            Destroy(item.gameObject);
        }
    }
    void replaceMeshRenderShader(Transform TransformRoot,string postFix,bool optimized)
    {
        Renderer[] renders = TransformRoot.GetComponentsInChildren<Renderer>();
        foreach (var render in renders)
        {
            foreach (var mat in  render.materials)
            {
                string shaderName = mat.shader.name;
                if(optimized)
                {
                    if (!shaderName.Contains(postFix))
                        shaderName += postFix;
                }
                else
                {
                    shaderName = shaderName.Replace(postFix, "");
                }
                var shader1 = Shader.Find(shaderName);
                if (shader1)
                    mat.shader = shader1;
                else
                    Debug.LogError("no shader " + shaderName);
            }
        }
        //if (TransformRoot.GetComponent<Renderer>())
        //{
        //    foreach (var mat in TransformRoot.GetComponent<Renderer>().materials)
        //    {
        //        mat.shader = Shader.Find(shaderName);
        //    }
        //}

        //foreach (Transform item in TransformRoot)
        //{
        //    if(item.GetComponent<Renderer>())
        //    {
        //        foreach (var mat in item.GetComponent<Renderer>().materials)
        //        {
        //            mat.shader = Shader.Find(shaderName);
        //        }
        //    }
        //    replaceMeshRenderShader(item, shaderName, iterCount);
        //}
    }

    IEnumerator GenerateObjects(List<GameObject> gos,Transform root,int a_Ocount,float gapX = 2.5f, float gapY =7, float startX = 0,float a_width = 50)
    {
        //float xx = Random.Range(0, 100000) / 10000f;
        int index = 0;
        if (gos.Count != 0)
        {
            int a_countPerROw = Mathf.CeilToInt((a_width - startX) / gapX);
            int row = Mathf.CeilToInt((float)a_Ocount / a_countPerROw);
            int count = 0;
            for (int i = 0; i < row; ++i)
            {
                for (int j = 0; j < a_countPerROw; ++j)
                {
                    if (count >= a_Ocount)
                        break;
                    //TODO
#if true
                    string assetpath = AssetDatabase.GetAssetPath(gos[index]);
                    assetpath = assetpath.Replace("PrefabFx", "BatchedFx");
                    var ttt = AssetDatabase.LoadAssetAtPath<GameObject>(assetpath);
                    GameObject go = Instantiate(ttt);
#else
                    GameObject go = Instantiate(gos[index]);
#endif
                    index++;
                    if (index >= gos.Count) index = 0;
                    //TODO
                    //index = 0;
                    go.transform.parent = root.transform;
                    go.transform.localPosition = new Vector3(startX+ gapX * j, 0, -i * gapY);
                    ParticleSystem[] ParticleSystems = go.transform.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in ParticleSystems)
                    {
                        ps.loop = true;
                    }
                        count++;
                    yield return null;
                }
            }
        }
        yield return null;
    }

}
