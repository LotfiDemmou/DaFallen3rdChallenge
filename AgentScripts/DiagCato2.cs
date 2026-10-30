using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DiagCato2
{
    public static string Info()
    {
        var cato = GameObject.Find("Cato");
        if (cato == null) return "cato null";
        var cc = cato.GetComponent<CatoController>();
        if (cc == null) return "controller missing";
        var t = cc.GetType();
        object Get(string name)
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return f != null ? f.GetValue(cc) : "<missing " + name + ">";
        }
        int loaded = SceneManager.sceneCount;
        var active = SceneManager.GetActiveScene();
        var catoScene = cato.scene;
        return "isPlaying=" + Application.isPlaying
            + " time=" + Time.time.ToString("F2")
            + " dt=" + Time.deltaTime.ToString("F4")
            + " scenes=" + loaded
            + " active=" + active.name + "(" + active.path + ",loaded=" + active.isLoaded + ")"
            + " catoScene=" + catoScene.name + "(" + catoScene.path + ",loaded=" + catoScene.isLoaded + ")"
            + " awakeRan(rb)=" + (Get("rb") != null)
            + " animator=" + Get("animator")?.ToString()
            + " spriteRenderer=" + Get("spriteRenderer")?.ToString()
            + " moveInput=" + Get("moveInput");
    }
}
