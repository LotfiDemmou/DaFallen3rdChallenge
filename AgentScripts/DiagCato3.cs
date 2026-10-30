using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public static class DiagCato3
{
    public static string PressD()
    {
        var kb = Keyboard.current;
        if (kb == null) return "kb null";
        InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.D));
        InputSystem.Update();
        return "dPressed=" + kb.dKey.isPressed;
    }

    public static string SetFlipTrue()
    {
        var cato = GameObject.Find("Cato");
        var sr = cato.GetComponentInChildren<SpriteRenderer>();
        sr.flipX = true;
        int count = cato.GetComponents<CatoController>().Length;
        return "flipX=" + sr.flipX + " controllers=" + count;
    }

    public static string SetMoveInput()
    {
        var cato = GameObject.Find("Cato");
        var cc = cato.GetComponent<CatoController>();
        var f = cc.GetType().GetField("moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
        f.SetValue(cc, new Vector2(1f, 1f));
        return "set moveInput=" + f.GetValue(cc);
    }

    public static string Check()
    {
        var cato = GameObject.Find("Cato");
        var cc = cato.GetComponent<CatoController>();
        var t = cc.GetType();
        var mi = t.GetField("moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
        var anim = cato.GetComponentInChildren<Animator>();
        var sr = cato.GetComponentInChildren<SpriteRenderer>();
        var rb = cato.GetComponent<Rigidbody2D>();
        var kb = Keyboard.current;
        var st = anim != null ? anim.GetCurrentAnimatorStateInfo(0) : default;
        return "dPressed=" + (kb != null && kb.dKey.isPressed)
            + " moveInput=" + mi.GetValue(cc)
            + " animMoving=" + (anim != null && anim.GetBool("IsMoving"))
            + " flipX=" + (sr != null && sr.flipX)
            + " vel=" + (rb != null ? rb.linearVelocity.ToString() : "?")
            + " pos=" + cato.transform.position
            + " normalizedTime=" + st.normalizedTime.ToString("F3")
            + " frame=" + Time.frameCount;
    }
}
