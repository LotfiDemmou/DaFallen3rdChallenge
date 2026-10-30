using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CatoController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer spriteRenderer;

    Rigidbody2D rb;
    Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector2 input = Vector2.zero;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;

        moveInput = input.sqrMagnitude > 1f ? input.normalized : input;

        if (animator != null)
            animator.SetBool("IsMoving", moveInput.sqrMagnitude > 0.001f);

        if (spriteRenderer != null)
        {
            if (moveInput.x < -0.01f) spriteRenderer.flipX = true;
            else if (moveInput.x > 0.01f) spriteRenderer.flipX = false;
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = moveInput * moveSpeed;
    }
}
