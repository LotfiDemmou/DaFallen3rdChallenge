using UnityEditor.SceneManagement;
using UnityEngine;

public static class PreviewCamera
{
    public static string Zoom()
    {
        var cam = Camera.main;
        if (cam == null) return "no main camera";
        cam.orthographic = true;
        cam.orthographicSize = 3.0f;
        cam.transform.position = new Vector3(0.5f, 0f, -10f);
        return "zoomed on Cato";
    }

    public static string Restore()
    {
        var cam = Camera.main;
        if (cam == null) return "no main camera";
        cam.orthographic = true;
        cam.orthographicSize = 33f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "camera restored and scene saved";
    }
}
