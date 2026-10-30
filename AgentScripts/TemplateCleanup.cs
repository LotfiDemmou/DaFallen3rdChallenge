using UnityEditor.SceneManagement;
using UnityEngine;

public static class TemplateCleanup
{
    const string TemplateScene = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
    const string GameScene = "Assets/Scenes/SampleScene.unity";

    public static string RemoveMap()
    {
        var scene = EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single);
        var go = GameObject.Find("PixelMap Grid");
        if (go == null)
        {
            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            return "template already clean";
        }

        Object.DestroyImmediate(go);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        return "removed from " + scene.name + " and reopened " + GameScene;
    }
}
