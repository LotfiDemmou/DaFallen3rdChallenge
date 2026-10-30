using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public static class DiagCato
{
    public static string Info()
    {
        var cato = GameObject.Find("Cato");
        if (cato == null) return "cato null";
        var cc = cato.GetComponent<CatoController>();
        if (cc == null) return "CatoController missing";
        var t = cc.GetType();
        var anim = cato.GetComponentInChildren<Animator>();
        return "type=" + t.FullName
            + " asm=" + t.Assembly.GetName().Name
            + " enabled=" + cc.enabled
            + " activeAndEnabled=" + cc.isActiveAndEnabled
            + " goActive=" + cato.activeInHierarchy
            + " hasUpdate=" + (t.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic) != null)
            + " hasFixed=" + (t.GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic) != null)
            + " animator=" + (anim != null ? anim.runtimeAnimatorController?.name : "null")
            + " animParams=" + (anim != null ? anim.parameterCount : 0)
            + " timeScale=" + Time.timeScale;
    }

    public static string PressAndForceUpdate()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.D));
            InputSystem.Update();
        }
        var cato = GameObject.Find("Cato");
        var cc = cato.GetComponent<CatoController>();
        var t = cc.GetType();
        var update = t.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        var fixedUpdate = t.GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        update?.Invoke(cc, null);
        fixedUpdate?.Invoke(cc, null);
        var mi = t.GetField("moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
        var anim = cato.GetComponentInChildren<Animator>();
        var rb = cato.GetComponent<Rigidbody2D>();
        return "moveInput=" + (mi != null ? mi.GetValue(cc).ToString() : "?")
            + " animMoving=" + (anim != null ? anim.GetBool("IsMoving").ToString() : "?")
            + " vel=" + (rb != null ? rb.linearVelocity.ToString() : "?")
            + " pos=" + cato.transform.position;
    }

    public static string Report()
    {
        var cato = GameObject.Find("Cato");
        var anim = cato.GetComponentInChildren<Animator>();
        var rb = cato.GetComponent<Rigidbody2D>();
        return "animMoving=" + (anim != null ? anim.GetBool("IsMoving").ToString() : "?")
            + " vel=" + (rb != null ? rb.linearVelocity.ToString() : "?")
            + " pos=" + cato.transform.position;
    }
}
