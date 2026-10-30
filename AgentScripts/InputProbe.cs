using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class InputProbe
{
    public static string IgnoreFocus()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        return "backgroundBehavior=" + InputSystem.settings.backgroundBehavior;
    }

    public static string RestoreFocusBehavior()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices;
        return "backgroundBehavior=" + InputSystem.settings.backgroundBehavior;
    }

    public static string PressAndReport()
    {
        var kb = Keyboard.current;
        if (kb == null) return "Keyboard.current is NULL";
        InputSystem.QueueStateEvent(kb, new KeyboardState(Key.D));
        InputSystem.Update();
        return "after queue: dPressed=" + kb.dKey.isPressed + " " + Snapshot();
    }

    public static string Release()
    {
        var kb = Keyboard.current;
        if (kb == null) return "Keyboard.current is NULL";
        InputSystem.QueueStateEvent(kb, new KeyboardState());
        InputSystem.Update();
        return "released";
    }

    public static string Report()
    {
        var kb = Keyboard.current;
        return (kb == null ? "kb=null" : "dPressed=" + kb.dKey.isPressed) + " " + Snapshot();
    }

    static string Snapshot()
    {
        var cato = GameObject.Find("Cato");
        if (cato == null) return "cato=null";
        var anim = cato.GetComponentInChildren<Animator>();
        var rb = cato.GetComponent<Rigidbody2D>();
        var cc = cato.GetComponent<CatoController>();
        bool animMoving = anim != null && anim.GetBool("IsMoving");
        var state = anim != null ? anim.GetCurrentAnimatorStateInfo(0) : default;
        string stateName = anim != null ? (state.IsName("Walk") ? "Walk" : state.IsName("Idle") ? "Idle" : "other") : "noAnim";
        return "animMoving=" + animMoving
            + " state=" + stateName
            + " vel=" + (rb != null ? rb.linearVelocity.ToString() : "noRb")
            + " pos=" + cato.transform.position
            + " controllerEnabled=" + (cc != null && cc.enabled)
            + " rbSimulated=" + (rb != null && rb.simulated);
    }
}
