using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
public class PackTex : MonoBehaviour {
#if UNITY_EDITOR
    [MenuItem("Assets/PackTex")]
    public static void MenuItem_GenAlphaTexture()
    {
        object[] array = Selection.objects;
        for (int i = 0; i < array.Length; ++i)
        {
            string texPath = AssetDatabase.GetAssetPath(array[i] as Object);
            //ImportTexture(texPath, TextureImporterFormat.RGBA32, true);
            GenerateFourAlpha(texPath);
        }
    }
    // Use this for initialization
    public static void GenerateFourAlpha(string texPath)
    {
        //Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        //TGALoader.LoadTGA(texPath);
        //srcTex.LoadImage(File.ReadAllBytes(texPath));
        Texture2D packedAlphaTex = PackFourAlphaToRGBA16(texPath);
        WriteAndImportAlphaTexture(packedAlphaTex, texPath, TextureImporterFormat.ETC2_RGB4);
    }

    private static Texture2D PackFourAlphaToRGBA16(string fileName)
    {

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

        int targetWidth = (int)Mathf.Ceil(width / 2.0f);
        int targetHeight = (int)Mathf.Ceil(height / 2.0f);


        Color[] teamcolormask = source.GetPixels(0, targetHeight, targetWidth, targetHeight);
        Color[] emmask = source.GetPixels(targetWidth, targetHeight, targetWidth, targetHeight);
        Color[] spmask = source.GetPixels(0, 0, targetWidth, targetHeight);
        int pixelNum = targetWidth * targetHeight;
        Color[] alphaColors = new Color[pixelNum];
        for (int i = 0; i < pixelNum; ++i)
        {
            alphaColors[i].r = teamcolormask[i].a;
            alphaColors[i].g = emmask[i].a;
            alphaColors[i].b = spmask[i].a;
            alphaColors[i].a = 1;
        }

        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
        result.SetPixels(alphaColors);
        result.Apply();
        return result;
    }

    private static void WriteAndImportAlphaTexture(Texture2D tex, string a_path, TextureImporterFormat format)
    {
        string path = GenTextureName(a_path);
        WriteTextureToFile(tex, path);
        ImportTexture(path, format);
    }
    public static void ImportTexture(string path, TextureImporterFormat format, bool isSync = false)
    {
        TextureImporter AlphaTextureImporter = (TextureImporter)TextureImporter.GetAtPath(path); //AssetImporter.GetAtPath(path) as TextureImporter;
        AlphaTextureImporter.isReadable = false;
        AlphaTextureImporter.npotScale = TextureImporterNPOTScale.None;
        //AlphaTextureImporter.textureType = TextureImporterType.GUI;
        //AlphaTextureImporter.wrapMode = TextureWrapMode.Clamp;
        AlphaTextureImporter.mipmapEnabled = true;
        AlphaTextureImporter.alphaIsTransparency = false;

        TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
        settings.format = format;
        settings.name = "Android";
        //settings.maxTextureSize = 8192;
        settings.overridden = true;
        AlphaTextureImporter.SetPlatformTextureSettings(settings);

        TextureImporterPlatformSettings iossettings = new TextureImporterPlatformSettings();
        iossettings.format = format;
        iossettings.name = "iPhone";
        //iossettings.maxTextureSize = 8192;
        iossettings.overridden = true;
        AlphaTextureImporter.SetPlatformTextureSettings(iossettings);

        if (isSync)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
        else
        {
            AssetDatabase.ImportAsset(path);
        }
    }
    private static string GenTextureName(string path, string ext = "_Mask")
    {
        return Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + ext + Path.GetExtension(path);
    }
    private static void WriteTextureToFile(Texture2D tex, string path)
    {
        if (File.Exists(path)) File.Delete(path);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();
    }
#endif
}

