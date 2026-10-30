using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixCato
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string CatoPng = "Assets/Art/Character/cato-export.png";
    const string GeneratedDir = "Assets/Art/Characters";

    public static string Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Remove any leftover generated Cato objects (sprite from my generated folder).
        int removedGenerated = 0;
        var gos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in gos)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;
            string p = AssetDatabase.GetAssetPath(sr.sprite);
            if (p.StartsWith(GeneratedDir + "/"))
            {
                Object.DestroyImmediate(go);
                removedGenerated++;
            }
        }

        bool removedAssets = AssetDatabase.DeleteAsset(GeneratedDir);

        // Crisp pixel-art import.
        var importer = (TextureImporter)AssetImporter.GetAtPath(CatoPng);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.None;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        // Find the real Cato: object whose sprite is cato-export.png.
        GameObject cato = null;
        gos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in gos)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null &&
                AssetDatabase.GetAssetPath(sr.sprite) == CatoPng)
            {
                cato = go;
                break;
            }
        }

        if (cato == null)
            return "ERROR: real Cato (sprite " + CatoPng + ") not found; removedGenerated=" + removedGenerated;

        cato.name = "Cato";
        cato.tag = "Player";
        cato.transform.localScale = Vector3.one;

        var renderer = cato.GetComponent<SpriteRenderer>();
        renderer.color = new Color(0.80f, 0.70f, 1.00f, 1f); // light purple filter
        renderer.sortingOrder = 10;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return string.Format(
            "Real Cato fixed: '{0}', tag={1}, scale={2}, PPU=32 Point/Uncompressed, tint={3}, sortingOrder=10. removedGenerated={4}, removedAssets={5}.",
            cato.name, cato.tag, cato.transform.localScale, renderer.color, removedGenerated, removedAssets);
    }
}
