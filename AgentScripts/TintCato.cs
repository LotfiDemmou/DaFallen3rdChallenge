using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TintCato
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string SrcPng = "Assets/Art/Character/cato-export.png";
    const string OutPng = "Assets/Art/Character/cato-purple.png";
    const float Blend = 0.55f;
    static readonly Color Purple = new Color(0.78f, 0.62f, 1.00f, 1f);

    public static string Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var srcImporter = (TextureImporter)AssetImporter.GetAtPath(SrcPng);
        bool wasReadable = srcImporter.isReadable;
        srcImporter.isReadable = true;
        srcImporter.SaveAndReimport();

        var src = AssetDatabase.LoadAssetAtPath<Texture2D>(SrcPng);
        if (src == null) return "ERROR: could not load " + SrcPng;

        int w = src.width, h = src.height;
        var px = src.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            var c = px[i];
            if (c.a <= 0f) continue;
            px[i] = new Color(
                Mathf.Lerp(c.r, Purple.r, Blend),
                Mathf.Lerp(c.g, Purple.g, Blend),
                Mathf.Lerp(c.b, Purple.b, Blend),
                c.a);
        }

        var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        outTex.filterMode = FilterMode.Point;
        outTex.wrapMode = TextureWrapMode.Clamp;
        outTex.SetPixels(px);
        outTex.Apply();
        File.WriteAllBytes(OutPng, outTex.EncodeToPNG());
        Object.DestroyImmediate(outTex);

        srcImporter.isReadable = wasReadable;
        srcImporter.SaveAndReimport();

        AssetDatabase.ImportAsset(OutPng, ImportAssetOptions.ForceUpdate);
        var outImporter = (TextureImporter)AssetImporter.GetAtPath(OutPng);
        outImporter.textureType = TextureImporterType.Sprite;
        outImporter.spriteImportMode = SpriteImportMode.Single;
        outImporter.spritePixelsPerUnit = 32f;
        outImporter.filterMode = FilterMode.Point;
        outImporter.textureCompression = TextureImporterCompression.Uncompressed;
        outImporter.mipmapEnabled = false;
        outImporter.alphaIsTransparency = true;
        outImporter.npotScale = TextureImporterNPOTScale.None;
        var st = new TextureImporterSettings();
        outImporter.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        outImporter.SetTextureSettings(st);
        outImporter.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OutPng);
        if (sprite == null) return "ERROR: could not load generated sprite";

        GameObject cato = null;
        var gos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in gos)
        {
            if (go.name == "Cato") { cato = go; break; }
        }
        if (cato == null) return "ERROR: Cato not found";

        cato.tag = "Player";
        cato.transform.localScale = Vector3.one;
        var sr = cato.GetComponent<SpriteRenderer>();
        if (sr == null) sr = cato.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = 10;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return string.Format("Cato now uses {0} ({1}x{2}, PPU 32, Point/Uncompressed), blended {3:P0} toward light purple.", OutPng, w, h, Blend);
    }
}
