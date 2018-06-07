//#define halfAlpha
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class TextureCompression
{
    private static Texture2D PackFourAlphaToRGBA16(Texture2D source)
    {
        int targetWidth = (int)Mathf.Ceil(source.width / 2.0f);
        int targetHeight = (int)Mathf.Ceil(source.height / 2.0f);
        int pixelNum = targetWidth * targetHeight;
        Color[] alphaColors = new Color[pixelNum];

        var texw = source.width;
        var texh = source.height;
        var pixels = source.GetPixels();
        var offs = 0;
        var k1Per15 = 1.0f / 15.0f;
        var k1Per16 = 1.0f / 16.0f;
        var k3Per16 = 3.0f / 16.0f;
        var k5Per16 = 5.0f / 16.0f;
        var k7Per16 = 7.0f / 16.0f;
        for (var y = 0; y < texh; y++)
        {
            for (var x = 0; x < texw; x++)
            {
                float a = pixels[offs].a;
                var a2 = Mathf.Clamp01(Mathf.Floor(a * 16) * k1Per15);
                var ae = a - a2;
                pixels[offs].a = a2;
                alphaColors[y % targetHeight * targetWidth + x % targetWidth][x / targetWidth + y / targetHeight * 2] = pixels[offs].a;

                var n1 = offs + 1;
                var n2 = offs + texw - 1;
                var n3 = offs + texw;
                var n4 = offs + texw + 1;

                if (x < texw - 1)
                {
                    pixels[n1].a += ae * k7Per16;
                }

                if (y < texh - 1)
                {
                    pixels[n3].a += ae * k5Per16;
                    if (x > 0)
                    {
                        pixels[n2].a += ae * k3Per16;
                    }
                    if (x < texw - 1)
                    {
                        pixels[n4].a += ae * k1Per16;
                    }
                }
                offs++;
            }
        }

        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
        result.SetPixels(alphaColors);
        result.Apply();
        return result;
    }
    private static void WriteAndImportAlphaTexture(Texture2D tex,string a_path, TextureImporterFormat format)
    {
        string path = GenTextureName(a_path);
        WriteTextureToFile(tex, path);
        ImportTexture(path, format);
    }

    private static void WriteTextureToFile(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();
    }
    public static void ImportTexture(string path, TextureImporterFormat format,bool isSync = false)
    {
        TextureImporter AlphaTextureImporter = (TextureImporter)TextureImporter.GetAtPath(path); //AssetImporter.GetAtPath(path) as TextureImporter;
        AlphaTextureImporter.isReadable = false;
        AlphaTextureImporter.npotScale = TextureImporterNPOTScale.None;
        AlphaTextureImporter.textureType = TextureImporterType.GUI;
        AlphaTextureImporter.wrapMode = TextureWrapMode.Clamp;
        AlphaTextureImporter.mipmapEnabled = false;
        AlphaTextureImporter.alphaIsTransparency = false;
        AlphaTextureImporter.textureCompression = TextureImporterCompression.Uncompressed; 

        TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
        settings.format = format;
        settings.name = "Android";
        settings.maxTextureSize = 8192;
        settings.overridden = true;
        AlphaTextureImporter.SetPlatformTextureSettings(settings);

        TextureImporterPlatformSettings iossettings = new TextureImporterPlatformSettings();
        iossettings.format = format;
        iossettings.name = "iPhone";
        iossettings.maxTextureSize = 8192;
        iossettings.overridden = true;
        AlphaTextureImporter.SetPlatformTextureSettings(iossettings);

        if (isSync)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        }
        else
        {
            AssetDatabase.ImportAsset(path);
        }
        //AssetDatabase.Refresh();
    }

    private static string GenTextureName(string path,string ext = "Alpha")
    {
        return Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + ext + Path.GetExtension(path);
    }

    //[MenuItem("Assets/New Dither RGB565 + Alpha4")]
    public static void MenuItem_GenAlphaTexture()
    {
        object[] array = Selection.objects;
        for (int i = 0; i < array.Length; ++i)
        {
            string texPath = AssetDatabase.GetAssetPath(array[i] as Object);
            //ImportTexture(texPath, TextureImporterFormat.RGBA32, true);
            GenerateFourAlpha(texPath);
            GenerateDitherRGB565(texPath);
        }
    }
    //[MenuItem("Assets/New RGB565 NoDither")]
    public static void MenuItem_Gen565Texture()
    {
        object[] array = Selection.objects;
        for (int i = 0; i < array.Length; ++i)
        {
            string texPath = AssetDatabase.GetAssetPath(array[i] as Object);
            //ImportTexture(texPath, TextureImporterFormat.RGBA32, true);
            GenerateRGB565(texPath);
        }
    }

    public static void GenerateFourAlpha(string texPath)
    {
        Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        srcTex.LoadImage(File.ReadAllBytes(texPath));
        Texture2D packedAlphaTex = PackFourAlphaToRGBA16(srcTex);
        WriteAndImportAlphaTexture(packedAlphaTex, texPath, TextureImporterFormat.RGBA16);
    }

    struct FTFontInfo
    {
        public void FontInfo(bool b)
        {
            bHasFont = b;
        }
        public int x;
        public int y;
        public int w;
        public int h;
        public bool bHasFont;
    }
    static FTFontInfo FontInfo;
    public static void GenerateDitherRGB565(string texPath)
    {
        Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        srcTex.LoadImage(File.ReadAllBytes(texPath));
        Texture2D RGB565Tex;
        FontInfo.bHasFont = false;
        if (texPath.Contains("_UIAtlas"))
        {
            string txtPath = texPath.Replace(".png", ".txt");
            LitJson.JsonData jd = LitJson.JsonMapper.ToObject(File.ReadAllText(txtPath));
            jd = jd["frames"]["FTFont.png"]["frame"];
            FontInfo.bHasFont = true;
            FontInfo.x = System.Convert.ToInt32(jd["x"].ToString());
            FontInfo.y = System.Convert.ToInt32(jd["y"].ToString());
            FontInfo.w = System.Convert.ToInt32(jd["w"].ToString());
            FontInfo.h = System.Convert.ToInt32(jd["h"].ToString());
        }
        RGB565Tex = CompressRGB565(srcTex);

        //WriteAndImportAlphaTexture(packed565Tex, texPath, TextureImporterFormat.RGBA16);
        string path = GenTextureName(texPath,"");
        WriteTextureToFile(RGB565Tex, path);
        ImportTexture(path, TextureImporterFormat.RGB16);
    }

    static void GenerateRGB565(string texPath)
    {
        Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        srcTex.LoadImage(File.ReadAllBytes(texPath));
        Texture2D RGB565Tex = srcTex;
        string path = GenTextureName(texPath, "");
        WriteTextureToFile(RGB565Tex, path);
        ImportTexture(path, TextureImporterFormat.RGB16);
    }
    static Texture2D CompressRGB565(Texture2D texture)
    {
        var texw = texture.width;
        var texh = texture.height;
        if (FontInfo.bHasFont)
        {
            for (int i = 0; i < FontInfo.w; i++)
            {
                for (int j = 0; j < FontInfo.h; j++)
                {
                    texture.SetPixel(FontInfo.x + i, (texture.height - (FontInfo.y + j)), new Color(1, 1, 1));
                }
            }
        }
        var pixels = texture.GetPixels();
       
        Color[] RGB565Colors = new Color[pixels.Length];
        var offs = 0;

        var k1Per31 = 1.0f / 31.0f;

        var k1Per32 = 1.0f / 32.0f;
        var k5Per32 = 5.0f / 32.0f;
        var k11Per32 = 11.0f / 32.0f;
        var k15Per32 = 15.0f / 32.0f;

        var k1Per63 = 1.0f / 63.0f;

        var k3Per64 = 3.0f / 64.0f;
        var k11Per64 = 11.0f / 64.0f;
        var k21Per64 = 21.0f / 64.0f;
        var k29Per64 = 29.0f / 64.0f;

        var k_r = 32; //R&B压缩到5位，所以取2的5次方
        var k_g = 64; //G压缩到6位，所以取2的6次方

        for (var y = 0; y < texh; y++)
        {
            for (var x = 0; x < texw; x++)
            {
                float r = pixels[offs].r;
                float g = pixels[offs].g;
                float b = pixels[offs].b;

                var r2 = Mathf.Clamp01(Mathf.Floor(r * k_r) * k1Per31);
                var g2 = Mathf.Clamp01(Mathf.Floor(g * k_g) * k1Per63);
                var b2 = Mathf.Clamp01(Mathf.Floor(b * k_r) * k1Per31);

                var re = r - r2;
                var ge = g - g2;
                var be = b - b2;

                var n1 = offs + 1;
                var n2 = offs + texw - 1;
                var n3 = offs + texw;
                var n4 = offs + texw + 1;

                if (x < texw - 1)
                {
                    pixels[n1].r += re * k15Per32;
                    pixels[n1].g += ge * k29Per64;
                    pixels[n1].b += be * k15Per32;
                }

                if (y < texh - 1)
                {
                    pixels[n3].r += re * k11Per32;
                    pixels[n3].g += ge * k21Per64;
                    pixels[n3].b += be * k11Per32;

                    if (x > 0)
                    {
                        pixels[n2].r += re * k5Per32;
                        pixels[n2].g += ge * k11Per64;
                        pixels[n2].b += be * k5Per32;
                    }

                    if (x < texw - 1)
                    {
                        pixels[n4].r += re * k1Per32;
                        pixels[n4].g += ge * k3Per64;
                        pixels[n4].b += be * k1Per32;
                    }
                }

                pixels[offs].r = r2;
                pixels[offs].g = g2;
                pixels[offs].b = b2;

                RGB565Colors[offs].r = pixels[offs].r;
                RGB565Colors[offs].g = pixels[offs].g;
                RGB565Colors[offs].b = pixels[offs].b;
                offs++;
            }
        }

        Texture2D result = new Texture2D(texture.width, texture.height, texture.format, false);
        result.SetPixels(RGB565Colors);
        result.Apply();
        return result;
    }


    void OnPostprocessTexture(Texture2D texture)
    {
        //if (Path.GetFileName(assetPath).Equals("Things.png"))
        //{
        //    CompressRGB565(texture);
        //    GenerateFourAlpha(assetPath);
        //}
    }
    void OnPreprocessTexture()
    {
        //var AlphaTextureImporter = (assetImporter as TextureImporter);
    }
}