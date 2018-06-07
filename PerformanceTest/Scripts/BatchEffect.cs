using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BatchEffect : EditorWindow {
    //static List<string> shaderNameList = 
    //    new List<string>
    //    {
    //        "MT/Particles/Additive",
    //        "MT/Particles/Alpha Blended",
    //        "HeroShader/Effect/Particle-Add(Simple)",
    //        "HeroShader/Effect/ParticleAdditive",
    //        "HeroShader/Effect/Alpha Blended",
    //        "HeroShader/Effect/Alpha BlendedColor"
    //    };

    static List<GameObject> PrefabGameObjects;
    static Dictionary<string, List<string>> txPathsMap;
    static Dictionary<string, List<ParticleBatchParam>> ParticleBatchParamMap;
    static Dictionary<string, List<Texture2D>> texturesMap;

    struct ParticleBatchParam
    {
        public ParticleSystemRenderer PSRender { get; set; }
        public int RectIdx { get; set; }
        public ParticleSystem.TextureSheetAnimationModule TexAniModule { get; set; }
        public Material material { get; set; }
    }

    private static void Reset()
    {
        PrefabGameObjects = new List<GameObject>();
        PrefabGameObjects = new List<GameObject>();

        ParticleBatchParamMap = new Dictionary<string, List<ParticleBatchParam>>();
        texturesMap = new Dictionary<string, List<Texture2D>>();
        txPathsMap = new Dictionary<string, List<string>>();
        //foreach (string shaderName in shaderNameList)
        //{
        //    ParticleBatchParamMap.Add(shaderName, new List<ParticleBatchParam>());
        //    texturesMap.Add(shaderName, new List<Texture2D>());
        //    txPathsMap.Add(shaderName, new List<string>());
        //}
    }

    private static void DoCompressTexture(Texture2D tex)
    {

        if (tex.width >= 512 || tex.height >= 512)
        {
            CompressTexture(tex, 2);
        }
        else if (tex.width >= 64 || tex.height >= 64)
        {
            CompressTexture(tex, 1);
        }
        else
        {
        }
    }
    //private static void CompressTexture(Texture2D tex, int Div = 1)
    //{
    //    int resultWidth = tex.width >> Div;
    //    int resultHeight = tex.height >> Div;
    //    RenderTexture RT = RenderTexture.GetTemporary(resultWidth, resultHeight, 0, RenderTextureFormat.BGRA32);
    //    tex.filterMode = FilterMode.Bilinear;
    //    Graphics.Blit(tex, RT);
    //    RenderTexture.active = RT;
    //    Texture2D result = new Texture2D(resultWidth, resultHeight, TextureFormat.RGBA32, false);
    //    result.ReadPixels(new Rect(0, 0, resultWidth, resultHeight), 0, 0);
    //    result.Apply();
    //    RenderTexture.ReleaseTemporary(RT);
    //    return result;
    //}
    private static void CompressTexture(Texture2D tex, int Div = 1)
    {
        int newW = tex.width >> Div;
        int newH = tex.height >> Div;
        int Sq = newW > newH ? newW : newH;
        TextureScale.Bilinear(tex, Sq, Sq);
    }
    private static Texture2D LoadTGA(string fileName)
    {
        Debug.Log("TGA read " + fileName);
        var TGAStream = File.OpenRead(fileName);
        BinaryReader r = new BinaryReader(TGAStream);
        // Skip some header info we don't care about.
        // Even if we did care, we have to move the stream seek point to the beginning,
        // as the previous method in the workflow left it at the end.
        r.BaseStream.Seek(12, SeekOrigin.Begin);

        short width = r.ReadInt16();
        short height = r.ReadInt16();
        int bitDepth = r.ReadByte();

        // Skip a byte of header information we don't care about.
        r.BaseStream.Seek(1, SeekOrigin.Current);

        Texture2D tex = new Texture2D(width, height);
        Color32[] pulledColors = new Color32[width * height];

        if (bitDepth == 32)
        {
            for (int i = 0; i < width * height; i++)
            {
                byte red = r.ReadByte();
                byte green = r.ReadByte();
                byte blue = r.ReadByte();
                byte alpha = r.ReadByte();

                pulledColors[i] = new Color32(blue, green, red, alpha);
            }
        }
        else if (bitDepth == 24)
        {
            for (int i = 0; i < width * height; i++)
            {
                byte red = r.ReadByte();
                byte green = r.ReadByte();
                byte blue = r.ReadByte();

                pulledColors[i] = new Color32(blue, green, red, 1);
            }
        }
        else
        {
            throw new System.Exception("TGA texture had non 32/24 bit depth.");
        }
        Texture2D source = new Texture2D(width, height);
        source.SetPixels32(pulledColors);
        return source;
    }

    public static Texture2D LoadImage(string texPath)
    {
        string postfix = Path.GetExtension(texPath).ToLower();
        if (".tga" == postfix)
        {
            return LoadTGA(texPath);
        }
        else if (".png" == postfix || ".jpg" == postfix)
        {
            Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            srcTex.LoadImage(File.ReadAllBytes(texPath));
            return srcTex;
        }
        else
        {
            Loger.LogError("非法图片类型" + postfix + " " + texPath);
            return null;
        }

    }

    static Texture2D UnityLoadImage(Texture2D tx)
    {
        string txPath = AssetDatabase.GetAssetPath(tx);
        //Debug.Log("Loading " + txPath);

        string postfix = Path.GetExtension(txPath).ToLower();
        if (".dds" == postfix)
        {
            Debug.LogError("DDS not support! " + txPath);
            return null;
        }

        if (null == tx)
        {
            return null;
        }
        var TT = TextureImporter.GetAtPath(txPath);
        if (null == TT)
        {
            Debug.LogError("非法图片 " + txPath);
            return null;
        }
        TextureImporter ti = (TextureImporter)TT;
        if (null == ti)
        {
            Debug.LogError("非法图片 " + txPath);
            return null;
        }
        //Debug.Log("loading "+txPath);
        bool changed = false;
        if (false == ti.isReadable)
        {
            //Debug.Log(txPath + " changed");
            changed = true;
            ti.isReadable = true;
            AssetDatabase.ImportAsset(txPath);
        }

        Texture2D tx2D = new Texture2D(tx.width, tx.height, TextureFormat.RGBA32, false);
        tx2D.SetPixels32(tx.GetPixels32());
        //if (changed)
        //{
        //    ti.isReadable = false;
        //    AssetDatabase.ImportAsset(txPath);
        //}
        return tx2D;
    }

    [ExecuteInEditMode]
    [MenuItem("Custom/CheckParticle")]
    private static void CheckParticle()
    {
        string genPath = Application.dataPath + "/Game/Resources/PrefabFx";
        string[] filesPath = Directory.GetFiles(genPath, "*.prefab", SearchOption.AllDirectories);
        List<string> prefabNames = new List<string>();
        for (int i = 0; i < filesPath.Length; i++)
        {
            filesPath[i] = filesPath[i].Substring(filesPath[i].IndexOf("Assets"));
            GameObject _prefab = AssetDatabase.LoadAssetAtPath(filesPath[i], typeof(GameObject)) as GameObject;
            GameObject prefabGameobject = PrefabUtility.InstantiatePrefab(_prefab) as GameObject;
            Transform[] children = prefabGameobject.transform.ChildsToArray();
            CheckData(children, prefabGameobject, prefabNames);
            DestroyImmediate(prefabGameobject);
        }
        Debug.Log("Count:" + prefabNames.Count);
    }
    private static void CheckData(Transform[] children, GameObject prefabGameobject, List<string> prefabNames)
    {
        for (int idxChild = 0; idxChild < children.Length; ++idxChild)
        {
            Transform[] subChildren = children[idxChild].ChildsToArray();
            if (subChildren.Length > 0)
            {
                CheckData(subChildren, prefabGameobject, prefabNames);
            }
            var PS = children[idxChild].GetComponent<ParticleSystem>();
            //if (
            //    PS
            //    && PS.textureSheetAnimation.enabled == true
            //    && PS.textureSheetAnimation.frameOverTime.mode==ParticleSystemCurveMode.Constant
            //    && (PS.textureSheetAnimation.animation == ParticleSystemAnimationType.WholeSheet)
            //    )
            ParticleSystemRenderer PSRenderer = children[idxChild].GetComponent<ParticleSystemRenderer>();
            if (
                null == PSRenderer
                || false == PSRenderer.enabled
                || PSRenderer.renderMode == ParticleSystemRenderMode.Mesh// mesh rendermode actually cannot be batched, but Unity doc didn't mention it.
                || PSRenderer.renderMode == ParticleSystemRenderMode.None
                || null == PSRenderer.sharedMaterial
                || false == checkMaterial(PSRenderer.sharedMaterial)
                )
            { continue; }

            //Texture
            Texture2D tx = PSRenderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            if (!(Get2Flag(tx.width) && Get2Flag(tx.height)))
            {

                string cname = prefabGameobject.name + " " + children[idxChild].name;
                if (!prefabNames.Contains(cname))
                {
                    prefabNames.Add(cname);
                    Debug.Log(cname + " " + tx.name + " " + tx.width + " " + tx.height);
                }
            }
        }
    }
    private static bool Get2Flag(int num)
    {
        if (num < 1) return false;
        return (num & num - 1) == 0;
    }
    private static bool checkMaterial(Material a_Mat)
    {
        if (null == a_Mat) return false;
        if (null == a_Mat.shader) return false;
        //if (
        //   shaderNameList.Contains(a_Mat.shader.name)
        //    )
        //{
        //    return true;
        //}
        return true;
    }
    private static bool checkPSRenderer(ParticleSystemRenderer a_PSRenderer)
    {
        if (null != a_PSRenderer
            && true == a_PSRenderer.enabled
            && a_PSRenderer.renderMode != ParticleSystemRenderMode.Mesh// mesh rendermode actually cannot be batched, but Unity doc didn't mention it.
            && a_PSRenderer.renderMode != ParticleSystemRenderMode.None)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private static bool checkTexture(Texture2D a_Tex)
    {
        //if(a_Tex.width != a_Tex.height)
        //{
        //    Debug.LogError("N-Square Tex! " + a_Tex.width + " " + a_Tex.height + " " + a_Tex.name);
        //    return false;
        //}
        if (Get2Flag(a_Tex.width) && Get2Flag(a_Tex.height))
        {
            //TODO
            if (a_Tex.width >= 2048 || a_Tex.height >= 2048)
            {
                Debug.LogError("Too Big! " + a_Tex.width + " " + a_Tex.height + " " + a_Tex.name);
                return false;
            }
            return true;
        }
        else
        {
            Debug.Log("NPOT Tex! " + a_Tex.name);
            return false;
        }
    }
    private static bool checkAniModuel(ParticleSystem.TextureSheetAnimationModule texAni, string prefabName, string objName)
    {
        if (false == texAni.enabled)
        {
            return true;
        }
        else
        {
            if (texAni.startFrame.mode == ParticleSystemCurveMode.Constant
                && 0 == texAni.startFrame.constant)
            {
                if (texAni.frameOverTime.mode == ParticleSystemCurveMode.Constant)
                {
                    if (texAni.animation == ParticleSystemAnimationType.SingleRow)
                    {
                        if (texAni.useRandomRow == false)
                        {
                            return true;
                        }
                    }
                    else if (texAni.animation == ParticleSystemAnimationType.WholeSheet)
                    {
                        return true;
                    }
                }
                else if (texAni.frameOverTime.mode == ParticleSystemCurveMode.Curve)
                {
                    if (texAni.animation == ParticleSystemAnimationType.WholeSheet)
                    {
                        return true;
                    }
                }
            }
        }
        //Debug.LogError("AniModuel invalid! "
        //    + "\n" + prefabName + " " + objName
        //    + "\n" + texAni.frameOverTime.mode.ToString()
        //    + "\n" + texAni.animation
        //    + "\nRandomRow " + texAni.useRandomRow
        //    + "\nstartFrameMode " + texAni.startFrame.mode
        //    + "\nstartFrame " + texAni.startFrame.constant
        //    );
        return false;
    }
    private static void CollectData(Transform[] children, GameObject prefabGameobject)
    {
        for (int idxChild = 0; idxChild < children.Length; ++idxChild)
        {
            Transform[] subChildren = children[idxChild].ChildsToArray();
            if (subChildren.Length > 0)
            {
                CollectData(subChildren, prefabGameobject);
            }
            ParticleSystem PS = children[idxChild].GetComponent<ParticleSystem>();
            ParticleSystem.TextureSheetAnimationModule TexAniModule = PS.textureSheetAnimation;
            ParticleSystemRenderer PSRenderer = children[idxChild].GetComponent<ParticleSystemRenderer>();
            if (
                false == checkPSRenderer(PSRenderer)
                || false == checkMaterial(PSRenderer.sharedMaterial)
                || false == checkAniModuel(TexAniModule, prefabGameobject.name, children[idxChild].name)
                )
            { continue; }

            //foreach (string shaderName in shaderNameList)
            //{
            //    ParticleBatchParamMap.Add(shaderName, new List<ParticleBatchParam>());
            //    texturesMap.Add(shaderName, new List<Texture2D>());
            //    txPathsMap.Add(shaderName, new List<string>());
            //}
            string shaderName = PSRenderer.sharedMaterial.shader.name;
            if (!ParticleBatchParamMap.ContainsKey(shaderName))
            {
                ParticleBatchParamMap.Add(shaderName, new List<ParticleBatchParam>());
                texturesMap.Add(shaderName, new List<Texture2D>());
                txPathsMap.Add(shaderName, new List<string>());
            }

            //Texture
            Texture2D tx = PSRenderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            if (!checkTexture(tx)) { continue; }
            string txPath = AssetDatabase.GetAssetPath(tx);

            ParticleBatchParam PBP = new ParticleBatchParam();

            int txIdx = txPathsMap[shaderName].IndexOf(txPath);
            if (txIdx >= 0)
            {
                PBP.RectIdx = txIdx;
            }
            //if (!txPathsMap[shaderName].Contains(txPath))
            else
            {
                Texture2D ldimg = UnityLoadImage(tx);
                if (null == ldimg)
                {
                    Debug.Log("Load fail : " + txPath);
                    continue;
                }
                txPathsMap[shaderName].Add(txPath);
                DoCompressTexture(ldimg);
                texturesMap[shaderName].Add(ldimg);

                PBP.RectIdx = texturesMap[shaderName].Count - 1;
            }

            PBP.PSRender = PSRenderer;
            PBP.TexAniModule = PS.textureSheetAnimation;
            PBP.material = PSRenderer.sharedMaterial;
            ParticleBatchParamMap[shaderName].Add(PBP);

            //Modify start color

            ParticleSystem.MainModule main = PS.main;
            var startColor = main.startColor;
            var tintCol = PSRenderer.sharedMaterial.GetColor("_TintColor");
            switch (startColor.mode)
            {
                case ParticleSystemGradientMode.Color:
                    startColor.color = startColor.color * tintCol;
                    break;
                case ParticleSystemGradientMode.TwoColors:
                    Color min = startColor.colorMin * tintCol;
                    Color max = startColor.colorMax * tintCol;
                    startColor.colorMin = min;
                    startColor.colorMax = max;
                    break;
                default:
                    Debug.LogError("col Mode false! " + startColor.mode.ToString() + " " + prefabGameobject.name + " / " + children[idxChild].name);
                    break;
            }
            main.startColor = startColor;
        }
    }
    [ExecuteInEditMode]
    [MenuItem("Custom/ATestBatchParticle")]
    private static void TestBatchParticle()
    {
        BatchParticleInternal(Application.dataPath + "/Game/Resources/ATestPrefabFx", "Assets/Game/Resources/ATestBatchedFx/");
    }

    [ExecuteInEditMode]
    [MenuItem("Custom/BatchParticle")]
    private static void BatchParticle()
    {
        if (
                EditorUtility.DisplayDialog(
                    "功能确认",
                    "此功能会遍历Game/Resources/PrefabFx下的全部prefab，确定要执行吗？",
                    "确定"
                )
            )
        {
            BatchParticleInternal(Application.dataPath + "/Game/Resources/PrefabFx", "Assets/Game/Resources/BatchedFx/");
        }
    }
    private static void BatchParticleInternal(string genPath, string BatchedFilePath)
    {
        Reset();
        DeleteLogFile(BatchedFilePath);

        string[] filesPath = Directory.GetFiles(genPath, "*.prefab", SearchOption.AllDirectories);

        for (int i = 0; i < filesPath.Length; i++)
        {
            filesPath[i] = filesPath[i].Substring(filesPath[i].IndexOf("Assets"));
            GameObject _prefab = AssetDatabase.LoadAssetAtPath(filesPath[i], typeof(GameObject)) as GameObject;
            GameObject prefabGameobject = PrefabUtility.InstantiatePrefab(_prefab) as GameObject;
            PrefabGameObjects.Add(prefabGameobject);
            Transform[] children = prefabGameobject.transform.ChildsToArray();
            CollectData(children, prefabGameobject);
        }

        int RenderOrderInLayer = 10;
        foreach (var item in ParticleBatchParamMap)
        {
            RenderOrderInLayer++;
            string shaderName = item.Key;
            List<ParticleBatchParam> ParticleBatchParamList = item.Value;
            if (ParticleBatchParamList.Count <= 0)
            {
                Debug.LogError("No Material use shader :" + shaderName);
                continue;
            }
            string mergedTexPath = BatchedFilePath + shaderName.Replace("/", "") + ".png";
            string mergedMatPath = BatchedFilePath + shaderName.Replace("/", "") + ".mat";
            Texture2D texture = new Texture2D(2, 2);
            Rect[] rects = texture.PackTextures(texturesMap[item.Key].ToArray(), 0, 2048);
            File.WriteAllBytes(mergedTexPath, texture.EncodeToPNG());
            AssetDatabase.Refresh();
            Texture2D MergedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(mergedTexPath);

            Material materialNew = new Material(ParticleBatchParamList[0].material.shader);
            materialNew.CopyPropertiesFromMaterial(ParticleBatchParamList[0].material);
            materialNew.SetTexture("_MainTex", MergedTex);
            materialNew.SetColor("_TintColor", new Color(1, 1, 1, 1));
            AssetDatabase.CreateAsset(materialNew, mergedMatPath);
            string logContent = shaderName + "\n";
            for (int xx = 0; xx < rects.Length; ++xx)
            {
                int newWidth = Mathf.RoundToInt(MergedTex.width * rects[xx].width);
                int newHeight = Mathf.RoundToInt(MergedTex.height * rects[xx].height);
                bool isWidthPOT = Get2Flag(newWidth);
                bool isHeightPOT = Get2Flag(newHeight);

                int newX = Mathf.RoundToInt(MergedTex.width * rects[xx].x);
                bool isAlignX = (0 == (newX % newWidth));
                int newY = Mathf.RoundToInt(MergedTex.height * rects[xx].y);
                bool isAlignY = (0 == (newY % newHeight));

                if (!(isWidthPOT&& isHeightPOT))
                {
                    Debug.LogError("NPOT final tex:"+txPathsMap[shaderName][xx]+ " "+newWidth+ " " + newHeight);
                }
                if (!(isAlignX && isAlignY))
                {
                    Debug.LogError("Not align tex:" + " Pos:" + newX + " " + newY + " Size:" + newWidth + " " + newHeight + " " + txPathsMap[shaderName][xx]);
                }
                logContent += (rects[xx] + " Pos:" + newX + " "+newY +" Size:"+ newWidth+ " " + newHeight + " "+txPathsMap[shaderName][xx] + "\n");
            }
            Log(BatchedFilePath, logContent);

            for (int Idx = 0; Idx < ParticleBatchParamList.Count; ++Idx)
            {
                //TODO not support startFrame !=0
                var PBP = ParticleBatchParamList[Idx];

                //Modify render order
                PBP.PSRender.sortingOrder = RenderOrderInLayer;

                Rect rect = rects[PBP.RectIdx];
                PBP.PSRender.sharedMaterial = materialNew;

                 //Modify ani
                ParticleSystem.TextureSheetAnimationModule texAni = PBP.TexAniModule;

                if (false == texAni.enabled)
                {
                    texAni.enabled = true;
                    var stFrame = new ParticleSystem.MinMaxCurve(0);
                    stFrame.mode = ParticleSystemCurveMode.Constant;
                    texAni.startFrame = stFrame;
                    texAni.numTilesX = Mathf.RoundToInt(1 / rect.width);
                    texAni.numTilesY = Mathf.RoundToInt(1 / rect.height);
                    texAni.animation = ParticleSystemAnimationType.SingleRow;
                    texAni.useRandomRow = false;
                    int rowIndex = Mathf.RoundToInt((1.0f - rect.y) * texAni.numTilesY) - 1;
                    texAni.rowIndex = rowIndex;
                    int columnIndex = Mathf.RoundToInt(rect.x * texAni.numTilesX);
                    texAni.frameOverTime = new ParticleSystem.MinMaxCurve(rect.x);
                }
                else
                {
                    int framXCount = texAni.numTilesX;
                    int framYCount = texAni.numTilesY;
                    int frameNum = framXCount * framYCount;
                    Keyframe[] keys = new Keyframe[frameNum + 1];
                    int bigTilesNumX = Mathf.RoundToInt(1 / rect.width);
                    int bigTilesNumY = Mathf.RoundToInt(1 / rect.height);
                    texAni.numTilesX = framXCount * bigTilesNumX;
                    texAni.numTilesY = framYCount * bigTilesNumY;
                    int totalTilesNum = texAni.numTilesX * texAni.numTilesY;
                    float deltaFrame = 1.0f / (totalTilesNum);
                    int fullTileIdxY = (Mathf.RoundToInt((1.0f - rect.y) * bigTilesNumY) - 1);
                    int fullTileIdxX = Mathf.RoundToInt(rect.x * texAni.numTilesX);
                    int firstFrameRowIndex = fullTileIdxY * framYCount;
                    int firstFrameColumnIndex = fullTileIdxX;
                    int firstframeId = (firstFrameRowIndex) * texAni.numTilesX + firstFrameColumnIndex;
                    float firstFrameVal = firstframeId / (float)totalTilesNum;

                    if (texAni.frameOverTime.mode == ParticleSystemCurveMode.Constant)
                    {
                        if (texAni.animation == ParticleSystemAnimationType.SingleRow)
                        {
                            if (texAni.useRandomRow == false)
                            {
                                texAni.rowIndex = fullTileIdxY + texAni.rowIndex;
                                float frameVal = rect.x + texAni.frameOverTime.constant * rect.width;
                                texAni.frameOverTime = new ParticleSystem.MinMaxCurve(frameVal);
                            }
                        }
                        else if (texAni.animation == ParticleSystemAnimationType.WholeSheet)
                        {
                            //var row = texAni.frameOverTime.constant / ((float)1 / framYCount);
                            //var column = texAni.frameOverTime.constant % ((float)1/framYCount);

                            int frameId = Mathf.FloorToInt(texAni.frameOverTime.constant * frameNum);//TODO
                            int row = frameId / framXCount;
                            int column = frameId % framYCount;
                            float frameVal = firstFrameVal + row / (float)texAni.numTilesY + column * deltaFrame;
                            texAni.frameOverTime = new ParticleSystem.MinMaxCurve(frameVal);
                        }
                    }
                    else if (texAni.frameOverTime.mode == ParticleSystemCurveMode.Curve)
                    {
                        if (texAni.animation == ParticleSystemAnimationType.WholeSheet)
                        {
                            keys[0] = new Keyframe(0, firstFrameVal);
                            int frameIdx = 0;
                            for (int frameY = 0; frameY < framYCount; ++frameY)
                            {
                                for (int frameX = 0; frameX < framXCount; ++frameX)
                                {
                                    float frameVal = deltaFrame + firstFrameVal + frameY / (float)texAni.numTilesY + frameX * deltaFrame;
                                    keys[frameIdx] = new Keyframe(frameIdx * 1.0f / frameNum, frameVal);
                                    ++frameIdx;
                                    if (frameIdx == frameNum)
                                    {
                                        keys[frameNum] = new Keyframe(1.0f, frameVal + deltaFrame);
                                    }
                                }
                            }
                            var curve = new AnimationCurve(keys);

                            for (int fg = 0; fg < framYCount; ++fg)
                            {
                                AnimationUtility.SetKeyLeftTangentMode(curve, fg * framXCount, AnimationUtility.TangentMode.Constant);
                            }

                            texAni.frameOverTime = new ParticleSystem.MinMaxCurve(1.0f, curve);
                        }
                    }
                }
            }
        }

        for (int idxBoj = 0; idxBoj < PrefabGameObjects.Count; ++idxBoj)
        {
            GameObject pObj = PrefabGameObjects[idxBoj];
            string path = filesPath[idxBoj];
            string folder = System.IO.Path.GetFileName(genPath);
            string[] tokens = path.Split(new[] { folder }, System.StringSplitOptions.None);
            string finalpath = BatchedFilePath + tokens[tokens.Length - 1].Replace("\\", "/");
            finalpath = finalpath.Replace("\\", "/");
            finalpath = finalpath.Replace("//", "/");
            string dirPath = Path.GetDirectoryName(Application.dataPath + finalpath.Replace("Assets/", "/"));
            Directory.CreateDirectory(dirPath);
            PrefabUtility.CreatePrefab(finalpath, pObj);
            DestroyImmediate(pObj);
        }

        //AssetDatabase.SaveAssets();
        //EditorUtility.DisplayDialog("成功", "HIT_DATA 添加完成！", "确定");
    }

    public static void Log(string path, string Content)
    {
        StreamWriter sw = new StreamWriter(path + "\\Log.txt", true);
        string fileTitle = "日志文件创建的时间:" + System.DateTime.Now.ToString();
        sw.WriteLine(fileTitle);
        //开始写入
        sw.WriteLine(Content);
        //清空缓冲区
        sw.Flush();
        //关闭流
        sw.Close();
    }
    public static void DeleteLogFile(string path)
    {
        File.Delete(path + "\\Log.txt");
    }
}
