using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class PixelMapBuilder
{
    const int Size = 64;
    const int TilePx = 16;
    const string ArtDir = "Assets/Art/Tiles";
    const string GameScene = "Assets/Scenes/SampleScene.unity";

    static readonly string[] TileNames = { "Water", "Ash", "Moss", "Mushrooms", "Dirt" };
    static readonly string[] ObsoleteNames = { "Forest", "Grass", "Sand" };

    public static string Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != GameScene)
            scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);

        EnsureFolder("Assets/Art");
        EnsureFolder(ArtDir);

        var tileAssets = new Dictionary<string, Tile>
        {
            { "Water",     MakeTile("Water",     MakeWater()) },
            { "Ash",       MakeTile("Ash",       MakeAsh()) },
            { "Moss",      MakeTile("Moss",      MakeMoss()) },
            { "Mushrooms", MakeTile("Mushrooms", MakeMushrooms()) },
            { "Dirt",      MakeTile("Dirt",      MakeDirt()) },
        };

        foreach (var obsolete in ObsoleteNames)
        {
            AssetDatabase.DeleteAsset(ArtDir + "/" + obsolete + ".png");
            AssetDatabase.DeleteAsset(ArtDir + "/" + obsolete + ".asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var existing = GameObject.Find("PixelMap Grid");
        if (existing != null) Object.DestroyImmediate(existing);

        var gridGo = new GameObject("PixelMap Grid");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        var tmGo = new GameObject("Tilemap");
        tmGo.transform.SetParent(gridGo.transform, false);
        var tilemap = tmGo.AddComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        tmGo.AddComponent<TilemapRenderer>();

        int half = Size / 2;
        int water = 0, ash = 0, moss = 0, mushrooms = 0, dirt = 0;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                string kind = PickTile(x, y);
                switch (kind)
                {
                    case "Water": water++; break;
                    case "Ash": ash++; break;
                    case "Mushrooms": mushrooms++; break;
                    case "Dirt": dirt++; break;
                    default: moss++; break;
                }
                tilemap.SetTile(new Vector3Int(x - half, y - half, 0), tileAssets[kind]);
            }
        }

        tilemap.RefreshAllTiles();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = Size * 0.5f + 1f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.03f, 0.01f, 0.06f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return string.Format(
            "Built {0}x{1} purple pixel map on '{2}'. Tiles - moss:{3} mushrooms:{4} dirt:{5} ash:{6} water:{7}",
            Size, Size, scene.name, moss, mushrooms, dirt, ash, water);
    }

    static string PickTile(int x, int y)
    {
        float edge = Mathf.Min(Mathf.Min(x, Size - 1 - x), Mathf.Min(y, Size - 1 - y));
        if (edge < 1.5f) return "Water";

        float edgeFactor = Mathf.Clamp01(edge / 7f);
        float elevation = Mathf.PerlinNoise(x * 0.085f + 11.3f, y * 0.085f + 23.7f)
                          * (0.34f + 0.66f * edgeFactor);

        bool coast = edge < 8f;
        if (coast && elevation < 0.235f) return "Water";
        if (coast && elevation < 0.285f) return "Ash";

        float forestNoise = Mathf.PerlinNoise(x * 0.16f + 91.2f, y * 0.16f + 47.5f);
        if (forestNoise > 0.615f) return "Mushrooms";

        float pathNoise = Mathf.PerlinNoise(x * 0.11f + 210.4f, y * 0.11f + 133.9f);
        if (Mathf.Abs(pathNoise - 0.5f) < 0.028f) return "Dirt";

        return "Moss";
    }

    static Tile MakeTile(string name, Color[] pixels)
    {
        var tex = new Texture2D(TilePx, TilePx, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(pixels);
        tex.Apply();

        string pngPath = ArtDir + "/" + name + ".png";
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = TilePx;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;

        string assetPath = ArtDir + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(tile, assetPath);
        return tile;
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

    static Color[] MakeWater()
    {
        var px = new Color[TilePx * TilePx];
        var rng = new System.Random(101);
        for (int y = 0; y < TilePx; y++)
        for (int x = 0; x < TilePx; x++)
        {
            float v = 0.02f * (float)rng.NextDouble();
            bool wave = ((y + (x / 4)) % 5) == 0 && x % 8 < 5;
            Color c = wave ? new Color(0.52f, 0.18f, 0.88f) : new Color(0.30f, 0.06f, 0.58f);
            c += new Color(v, v * 0.4f, v);
            px[y * TilePx + x] = c;
        }
        return px;
    }

    static Color[] MakeAsh()
    {
        var px = new Color[TilePx * TilePx];
        var rng = new System.Random(202);
        for (int y = 0; y < TilePx; y++)
        for (int x = 0; x < TilePx; x++)
        {
            float v = 0.04f * (float)rng.NextDouble();
            Color c = new Color(0.10f, 0.085f, 0.13f) + new Color(v, v * 0.7f, v);
            if (rng.NextDouble() > 0.90) c *= 0.70f;
            px[y * TilePx + x] = c;
        }
        return px;
    }

    static Color[] MakeMoss()
    {
        var px = new Color[TilePx * TilePx];
        var rng = new System.Random(303);
        for (int y = 0; y < TilePx; y++)
        for (int x = 0; x < TilePx; x++)
        {
            float v = 0.035f * (float)rng.NextDouble();
            Color c = new Color(0.045f, 0.02f, 0.075f) + new Color(v, v * 0.4f, v);
            if (rng.NextDouble() > 0.92) c = new Color(0.15f, 0.06f, 0.24f);
            px[y * TilePx + x] = c;
        }
        return px;
    }

    static Color[] MakeMushrooms()
    {
        var px = new Color[TilePx * TilePx];
        var rng = new System.Random(404);
        for (int y = 0; y < TilePx; y++)
        for (int x = 0; x < TilePx; x++)
        {
            float v = 0.02f * (float)rng.NextDouble();
            px[y * TilePx + x] = new Color(0.03f, 0.015f, 0.05f) + new Color(v, v * 0.4f, v);
        }

        DrawMushroom(px, 4, 5, 3.0f, 21);
        DrawMushroom(px, 11, 4, 2.4f, 22);
        DrawMushroom(px, 7, 11, 2.7f, 23);
        DrawMushroom(px, 12, 12, 1.9f, 24);
        return px;
    }

    static Color[] MakeDirt()
    {
        var px = new Color[TilePx * TilePx];
        var rng = new System.Random(505);
        for (int y = 0; y < TilePx; y++)
        for (int x = 0; x < TilePx; x++)
        {
            float v = 0.03f * (float)rng.NextDouble();
            Color c = new Color(0.035f, 0.03f, 0.045f) + new Color(v, v * 0.4f, v);
            if (rng.NextDouble() > 0.92) c = new Color(0.14f, 0.07f, 0.20f);
            else if (rng.NextDouble() > 0.85) c = new Color(0.09f, 0.08f, 0.11f);
            px[y * TilePx + x] = c;
        }
        return px;
    }

    static void DrawMushroom(Color[] px, int cx, int cy, float capRadius, int seed)
    {
        var rng = new System.Random(seed);
        Color stem = new Color(0.45f, 0.40f, 0.55f);
        Color cap = new Color(0.40f, 0.14f, 0.62f);
        Color spot = new Color(0.62f, 0.34f, 0.82f);

        int stemTop = cy - 1;
        for (int y = stemTop; y >= cy - 3; y--)
            SetPx(px, cx, y, stem * (0.85f + 0.15f * (float)rng.NextDouble()));

        for (int y = 0; y < TilePx; y++)
        for (int x = 0; x < TilePx; x++)
        {
            float dx = (x - cx + 0.5f) / capRadius;
            float dy = (y - cy + 0.5f) / (capRadius * 0.75f);
            if (dx * dx + dy * dy <= 1f && y >= cy - 1)
            {
                Color c = cap * (0.85f + 0.2f * (float)rng.NextDouble());
                if (rng.NextDouble() > 0.82) c = spot;
                px[y * TilePx + x] = c;
            }
        }
    }

    static void SetPx(Color[] px, int x, int y, Color c)
    {
        if (x < 0 || x >= TilePx || y < 0 || y >= TilePx) return;
        px[y * TilePx + x] = c;
    }
}
