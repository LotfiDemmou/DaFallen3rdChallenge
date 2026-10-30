using UnityEngine;

public static class PreviewWalk
{
    public static string Enter()
    {
        var cato = GameObject.Find("Cato");
        var cc = cato.GetComponent<CatoController>();
        cc.enabled = false;
        var rb = cato.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        cato.transform.position = new Vector3(0.5f, -0.01f, 0f);
        var sr = cato.GetComponentInChildren<SpriteRenderer>();
        sr.flipX = false;
        var anim = cato.GetComponentInChildren<Animator>();
        anim.SetBool("IsMoving", true);
        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 3f;
        cam.transform.position = new Vector3(0.5f, 0.2f, -10f);
        return "controller disabled, walk state set, cam=" + cam.transform.position + " size=" + cam.orthographicSize;
    }

    public static string Exit()
    {
        var cato = GameObject.Find("Cato");
        var cc = cato.GetComponent<CatoController>();
        cc.enabled = true;
        var anim = cato.GetComponentInChildren<Animator>();
        anim.SetBool("IsMoving", false);
        return "controller re-enabled";
    }
}
