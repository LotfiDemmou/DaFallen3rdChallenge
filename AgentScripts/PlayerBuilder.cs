using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayerBuilder
{
    const int S = 64;
    const int PPU = 40;
    const string Dir = "Assets/Art/Characters";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    static readonly Color Fur      = new Color(0.30f, 0.11f, 0.48f);
    static readonly Color FurShade = new Color(0.16f, 0.06f, 0.27f);
    static readonly Color FurLight = new Color(0.48f, 0.22f, 0.68f);
    static readonly Color Eye      = new Color(0.92f, 0.28f, 0.96f);
    static readonly Color EyeCore  = new Color(1.00f, 0.90f, 1.00f);
    static readonly Color InnerEar = new Color(0.66f, 0.26f, 0.80f);
    static readonly Color Nose     = new Color(0.95f, 0.45f, 0.74f);
    static readonly Color Outline  = new Color(0.015f, 0.008f, 0.03f);

    public static string Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnsureFolder("Assets/Art");
        EnsureFolder(Dir);

        var sprite = MakeSprite("Cato", MakeCato());

        var existing = GameObject.Find("Cato");
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject("Cato");
        go.tag = "Player";
        go.transform.position = new Vector3(0f, 0f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.42f;
        col.offset = new Vector2(0f, -0.15f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return string.Format(
            "Added '{0}' (tag: {1}) at {2} with a {3}x{4} sprite at {5} PPU ({6} units tall).",
            go.name, go.tag, go.transform.position, S, S, PPU, (float)S / PPU);
    }

    static Sprite MakeSprite(string name, Color[] pixels)
    {
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(pixels);
        tex.Apply();

        string pngPath = Dir + "/" + name + ".png";
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static Color[] MakeCato()
    {
        var px = new Color[S * S];
        var mask = new bool[S * S];

        // tail
        FillEllipse(mask, px, 49, 22, 4.0f, 4.0f, FurShade);
        FillEllipse(mask, px, 54, 27, 3.6f, 3.6f, Fur);
        FillEllipse(mask, px, 56, 33, 3.2f, 3.2f, Fur);
        FillEllipse(mask, px, 56, 39, 2.8f, 2.8f, FurLight);

        // body + belly
        FillEllipse(mask, px, 32, 20, 13f, 12f, Fur);
        FillEllipse(mask, px, 32, 17, 8f, 8f, FurLight);

        // paws
        FillEllipse(mask, px, 23, 8, 5f, 4f, FurLight);
        FillEllipse(mask, px, 41, 8, 5f, 4f, FurLight);
        FillEllipse(mask, px, 23, 7, 3f, 2.2f, FurShade);
        FillEllipse(mask, px, 41, 7, 3f, 2.2f, FurShade);

        // ears
        FillTriangle(mask, px, 17, 45, 25, 49, 16, 60, Fur);
        FillTriangle(mask, px, 47, 45, 39, 49, 48, 60, Fur);
        FillTriangle(mask, px, 19, 48, 23, 50, 18, 57, InnerEar);
        FillTriangle(mask, px, 45, 48, 41, 50, 46, 57, InnerEar);

        // head
        FillEllipse(mask, px, 32, 38, 16f, 13f, Fur);

        // head shading / forehead marking
        FillEllipse(mask, px, 32, 44, 9f, 4f, FurLight);
        FillTriangle(mask, px, 32, 46, 29, 51, 35, 51, FurShade);

        // eyes
        FillEllipse(mask, px, 25, 39, 3.4f, 4.0f, Outline);
        FillEllipse(mask, px, 39, 39, 3.4f, 4.0f, Outline);
        FillEllipse(mask, px, 25, 39, 2.6f, 3.2f, Eye);
        FillEllipse(mask, px, 39, 39, 2.6f, 3.2f, Eye);
        FillEllipse(mask, px, 25, 40, 1.1f, 1.6f, EyeCore);
        FillEllipse(mask, px, 39, 40, 1.1f, 1.6f, EyeCore);

        // nose + mouth + whiskers
        FillTriangle(mask, px, 30, 34, 34, 34, 32, 32, Nose);
        FillRect(mask, px, 31, 30, 1, 1, FurShade);
        FillRect(mask, px, 33, 30, 1, 1, FurShade);
        FillRect(mask, px, 13, 34, 8, 1, FurLight);
        FillRect(mask, px, 13, 31, 8, 1, FurLight);
        FillRect(mask, px, 43, 34, 8, 1, FurLight);
        FillRect(mask, px, 43, 31, 8, 1, FurLight);

        // chest marking
        FillTriangle(mask, px, 32, 27, 28, 22, 36, 22, FurShade);

        OutlinePass(mask, px);
        return px;
    }

    static void FillEllipse(bool[] m, Color[] px, float cx, float cy, float rx, float ry, Color c)
    {
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float dx = (x + 0.5f - cx) / rx;
            float dy = (y + 0.5f - cy) / ry;
            if (dx * dx + dy * dy <= 1f)
            {
                int i = y * S + x;
                m[i] = true;
                px[i] = c;
            }
        }
    }

    static void FillRect(bool[] m, Color[] px, int x0, int y0, int w, int h, Color c)
    {
        for (int y = y0; y < y0 + h; y++)
        for (int x = x0; x < x0 + w; x++)
        {
            if (x < 0 || x >= S || y < 0 || y >= S) continue;
            int i = y * S + x;
            m[i] = true;
            px[i] = c;
        }
    }

    static void FillTriangle(bool[] m, Color[] px,
        float ax, float ay, float bx, float by, float cx, float cy, Color c)
    {
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float pxc = x + 0.5f, pyc = y + 0.5f;
            float d1 = Sign(pxc, pyc, ax, ay, bx, by);
            float d2 = Sign(pxc, pyc, bx, by, cx, cy);
            float d3 = Sign(pxc, pyc, cx, cy, ax, ay);
            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            if (!(hasNeg && hasPos))
            {
                int i = y * S + x;
                m[i] = true;
                px[i] = c;
            }
        }
    }

    static float Sign(float px, float py, float ax, float ay, float bx, float by)
    {
        return (px - bx) * (ay - by) - (ax - bx) * (py - by);
    }

    static void OutlinePass(bool[] m, Color[] px)
    {
        var add = new bool[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            if (m[y * S + x]) continue;
            bool near = false;
            for (int oy = -1; oy <= 1 && !near; oy++)
            for (int ox = -1; ox <= 1; ox++)
            {
                int nx = x + ox, ny = y + oy;
                if (nx < 0 || nx >= S || ny < 0 || ny >= S) continue;
                if (m[ny * S + nx]) { near = true; break; }
            }
            if (near) add[y * S + x] = true;
        }

        for (int i = 0; i < add.Length; i++)
        {
            if (!add[i]) continue;
            m[i] = true;
            px[i] = Outline;
        }
    }
}
