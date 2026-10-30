using UnityEngine;

public static class PlayState
{
    public static string Run()
    {
        var cato = GameObject.Find("Cato");
        return "isPlaying=" + Application.isPlaying
            + " runInBackground=" + Application.runInBackground
            + " frame=" + Time.frameCount
            + " time=" + Time.time.ToString("F2")
            + (cato != null ? " catoPos=" + cato.transform.position : "");
    }
}
