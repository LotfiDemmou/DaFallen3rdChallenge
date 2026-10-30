using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class SettingsProbe
{
    public static string Run()
    {
        var s = InputSystem.settings;
        var p = typeof(InputSettings).GetProperty("editorInputBehaviorInPlayMode");
        string val = p != null ? p.GetValue(s)?.ToString() : "<no property>";
        string values = p != null ? string.Join(",", Enum.GetNames(p.PropertyType)) : "";
        return "isFocused=" + Application.isFocused
            + " background=" + s.backgroundBehavior
            + " editorInput=" + val
            + " values=" + values;
    }

    public static string SetAllToGameView()
    {
        var s = InputSystem.settings;
        var p = typeof(InputSettings).GetProperty("editorInputBehaviorInPlayMode");
        if (p == null) return "no property";
        var t = p.PropertyType;
        object target = null;
        foreach (var name in Enum.GetNames(t))
        {
            if (name.StartsWith("AllDeviceInput"))
            {
                target = Enum.Parse(t, name);
                break;
            }
        }
        if (target == null) return "no AllDeviceInput value in " + string.Join(",", Enum.GetNames(t));
        p.SetValue(s, target);
        s.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        return "set editorInput=" + p.GetValue(s) + " background=" + s.backgroundBehavior;
    }
}
