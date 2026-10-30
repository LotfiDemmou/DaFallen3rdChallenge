using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reusable health for the player (or any other object). Implements IDamageable, so the
/// mushroom's attack and your bullets can hurt anything that has this component.
///
/// SETUP (player)
/// 1. Add this to the player object, the one that has (or is a parent of) the Collider2D
///    the mushroom's attack overlaps. The collider's layer must be in the mushroom's Target Mask.
/// 2. Optional: add a Rigidbody2D to the player to get knockback.
/// 3. In your movement script, skip movement while health.IsKnockedBack is true, otherwise
///    your script overwrites the knockback velocity:
///        if (health.IsKnockedBack) return;
/// 4. Use the events to react: On Died, for example, can disable the Gun script
///    (Behaviour > enabled) and show a game-over screen.
/// </summary>
[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 5;
    public bool destroyOnDeath;                 // leave off for the player and use On Died instead
    public bool godMode;                        // debug: ignores all damage

    [Header("After a hit")]
    public float invulnerableTime = 1f;         // can't be hurt again for this long
    public float blinkRate = 8f;                // blinks per second while invulnerable
    [Range(0f, 1f)] public float blinkAlpha = 0.3f;
    public SpriteRenderer[] blinkRenderers;     // empty = every SpriteRenderer on this object and its children

    [Header("Knockback (needs a Rigidbody2D)")]
    public float knockbackSpeed = 6f;           // world units per second, 0 = off
    public float knockbackTime = 0.15f;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onHealed;
    public UnityEvent onDied;

    /// <summary>Fired whenever health changes: (current, max). The health bar listens to this.</summary>
    public event Action<int, int> Changed;

    public int Current { get; private set; }
    public int Max { get { return maxHealth; } }
    public float Normalized { get { return maxHealth > 0 ? (float)Current / maxHealth : 0f; } }
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get { return invulnT > 0f; } }
    public bool IsKnockedBack { get { return knockT > 0f; } }

    Rigidbody2D rb;
    Color[] baseColors;
    float invulnT, knockT;
    Vector2 knockDir;
    bool blinkApplied;

    void Awake()
    {
        Current = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (blinkRenderers == null || blinkRenderers.Length == 0)
            blinkRenderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[blinkRenderers.Length];
        for (int i = 0; i < blinkRenderers.Length; i++) baseColors[i] = blinkRenderers[i].color;
    }

    void Start()
    {
        Notify();       // lets a health bar show the starting value
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }

    // ------------------------------------------------------------ taking damage

    /// <summary>IDamageable: called by the mushroom's attack, bullets, traps, etc.</summary>
    public void TakeHit(Vector2 fromPos, int damage)
    {
        if (IsDead || godMode || IsInvulnerable) return;

        Damage(damage);
        if (IsDead) return;

        invulnT = invulnerableTime;

        if (rb && knockbackSpeed > 0f)
        {
            Vector2 away = (Vector2)transform.position - fromPos;
            knockDir = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up;
            knockT = knockbackTime;
        }
    }

    /// <summary>Subtracts health directly, ignoring invulnerability (use for poison, fall damage...).</summary>
    public void Damage(int amount)
    {
        if (IsDead || amount <= 0) return;
        Current = Mathf.Max(0, Current - amount);
        onDamaged.Invoke();
        Notify();
        if (Current == 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0 || Current >= maxHealth) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        onHealed.Invoke();
        Notify();
    }

    public void Kill()
    {
        if (!IsDead) Damage(Current);
    }

    /// <summary>Brings the object back to full health (for respawn / restart).</summary>
    public void Revive()
    {
        IsDead = false;
        Current = maxHealth;
        invulnT = knockT = 0f;
        ApplyBlink(false);
        Notify();
    }

    void Die()
    {
        IsDead = true;
        invulnT = 0f;
        ApplyBlink(false);
        onDied.Invoke();
        if (destroyOnDeath) Destroy(gameObject);
    }

    // ------------------------------------------------------------ feedback

    void Update()
    {
        if (invulnT > 0f)
        {
            invulnT -= Time.deltaTime;
            bool faded = Mathf.FloorToInt(Time.time * blinkRate * 2f) % 2 == 0;
            ApplyBlink(invulnT > 0f && faded);
        }
        else if (blinkApplied)
        {
            ApplyBlink(false);
        }
    }

    void FixedUpdate()
    {
        if (knockT <= 0f) return;
        knockT -= Time.fixedDeltaTime;
        SetVel(knockT > 0f ? knockDir * knockbackSpeed : Vector2.zero);
    }

    void ApplyBlink(bool faded)
    {
        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            if (!blinkRenderers[i]) continue;
            Color c = baseColors[i];
            if (faded) c.a *= blinkAlpha;
            blinkRenderers[i].color = c;
        }
        blinkApplied = faded;
    }

    void SetVel(Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }

    void Notify()
    {
        if (Changed != null) Changed(Current, maxHealth);
    }
}
