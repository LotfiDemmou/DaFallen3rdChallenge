using UnityEditor;

public static class PreviewCleanup
{
    public static string DeletePreview()
    {
        bool ok = AssetDatabase.DeleteAsset("Assets/Temp");
        AssetDatabase.Refresh();
        return ok ? "deleted Assets/Temp" : "Assets/Temp not found";
    }
}
