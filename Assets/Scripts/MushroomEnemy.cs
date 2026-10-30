using UnityEngine;

/// <summary>Implement this on your player (or anything the mushroom can hurt).</summary>
public interface IDamageable
{
    /// <param name="fromPos">World position of the attacker (use it for knockback direction).</param>
    void TakeHit(Vector2 fromPos, int damage);
}

/// <summary>
/// Mushroom enemy with a chasing mechanic (Unity 2D).
/// Works in a TOP-DOWN world (chases up/down/left/right) or a SIDE-SCROLLER (left/right only).
///
/// SETUP
/// 1. Import mushroom_sheet.png: Texture Type = Sprite (2D and UI), Sprite Mode = Multiple,
///    Pixels Per Unit = 16, Filter Mode = Point (no filter), Compression = None.
/// 2. Sprite Editor > Slice > Type: Grid By Cell Size 40 x 28, Pivot: Custom (0.3, 0.107)
///    (that puts the pivot between the feet, at ground level). Apply.
/// 3. Create an empty GameObject with: SpriteRenderer, Rigidbody2D (Freeze Rotation Z),
///    a small Collider2D for the body, and this script.
///    Top-down: use a small CircleCollider2D around the feet (not a tall capsule).
/// 4. Select the 10 sliced sprites and drag them onto the "Frames" label in the inspector.
/// 5. Tag your player "Player" (or assign Target), give it a Collider2D, put it on a layer
///    included in "Target Mask", and implement IDamageable on it.
/// 6. Top-down only: give the player's SpriteRenderer the same y-sorting
///    (sortingOrder = -y * 100), or set Project Settings > Graphics > Transparency Sort Mode
///    to Custom Axis (0, 1, 0) and turn off "Sort By Y" below.
///
/// The sheet faces right; the sprite is flipped with flipX when the player is on the left.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class MushroomEnemy : MonoBehaviour, IDamageable
{
    public enum State { Idle, Alert, Chase, Attack, Recover, Return }
    public enum WorldView { TopDown, SideScroller }

    [Header("World")]
    public WorldView view = WorldView.TopDown;
    public bool sortByY = true;         // top-down: sprites lower on screen draw in front

    [Header("Sprites: the 10 sliced frames of mushroom_sheet.png, in order")]
    public Sprite[] frames;

    [Header("Target")]
    public Transform target;            // defaults to the object tagged "Player"
    public LayerMask targetMask;        // layer(s) the attack can hit
    public int damage = 1;

    [Header("Tuning (sprite pixels, same numbers as the Python demo)")]
    public float pixelsPerUnit = 16f;   // must match the sprite's import setting
    public float sight = 110f;          // starts noticing the player inside this distance
    public float lose = 190f;           // gives up beyond this distance
    public float attackRange = 30f;     // starts the attack inside this distance
    public float chaseSpeed = 42f;      // px/s (keep slower than the player)
    public float returnSpeed = 26f;     // px/s walking home
    public float alertTime = 0.35f;     // reaction delay before chasing
    public float recoverTime = 0.7f;    // pause after an attack
    public float hitHeight = 26f;       // side-scroller only: hitbox height above the feet

    [Header("Optional")]
    public GameObject alertIcon;        // "!" shown while in the Alert state
    public bool endlessChase;           // never gives up (set by the spawner)

    [Header("Health")]
    public int maxHealth = 3;
    public float knockbackSpeed = 90f;  // px/s while staggered
    public float stunTime = 0.12f;      // staggered (and attack cancelled) for this long
    public Color hitColor = new Color(1f, 0.45f, 0.45f, 1f);
    public UnityEngine.Events.UnityEvent onDied;   // hook up score, drops, sounds

    // ---- sheet layout ----
    static readonly int[] IdleFrames = { 0, 1 };
    const int AttackStart = 2;          // attack frames are sheet indices 2..9
    const int AlertFrame = 2;           // braced, angry-eyed pose
    static readonly float[] AttackMs = { 140, 220, 100, 100, 130, 140, 140, 160 };

    // Attack frames that can hurt: attack-frame index -> reach away from the mushroom (px).
    // Index 4 = spore burst begins, 5 = cloud at full size.
    static bool TryGetReach(int idx, out float min, out float max)
    {
        min = 6f;
        switch (idx)
        {
            case 4: max = 26f; return true;
            case 5: max = 36f; return true;
        }
        max = 0f;
        return false;
    }

    public State CurrentState { get; private set; }
    bool IsTopDown { get { return view == WorldView.TopDown; } }

    SpriteRenderer sr;
    Rigidbody2D rb;
    float t;                    // time in current state
    int facing = 1;             // 1 = right, -1 = left (sprite flip only)
    Vector2 attackDir = Vector2.right;   // locked when the attack starts
    bool hitDone;
    Vector2 home;
    Vector2 dbgCenter, dbgSize;
    bool dbgActive;
    int health;
    bool warnedNoVictim;
    float stunT, flashT;
    Vector2 knockDir;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        if (IsTopDown) rb.gravityScale = 0f;
        home = transform.position;
        health = maxHealth;

        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        EnsureMask();
        Set(State.Idle);
    }

    /// <summary>Called by a spawner: aim at a target and skip the idle/alert phase.</summary>
    public void BeginChase(Transform newTarget, bool endless = true)
    {
        target = newTarget;
        EnsureMask();
        endlessChase = endless;
        Set(State.Chase);
    }

    /// <summary>IDamageable: called by bullets and anything else that can hurt it.</summary>
    public void TakeHit(Vector2 fromPos, int dmg)
    {
        health -= dmg;
        if (health <= 0)
        {
            if (onDied != null) onDied.Invoke();
            Destroy(gameObject);
            return;
        }

        Vector2 away = (Vector2)transform.position - fromPos;
        if (IsTopDown)
            knockDir = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.right;
        else
            knockDir = new Vector2(away.x < 0f ? -1f : 1f, 0f);

        stunT = stunTime;
        flashT = stunTime + 0.05f;
        Set(State.Chase);               // cancels an attack in progress and wakes it up
    }

    void Set(State s)
    {
        CurrentState = s;
        t = 0f;
        hitDone = false;
        if (s != State.Chase && s != State.Return) SetVel(Vector2.zero);
        if (alertIcon) alertIcon.SetActive(s == State.Alert);
    }

    void Update()
    {
        t += Time.deltaTime;
        dbgActive = false;

        if (stunT > 0f)
        {
            stunT -= Time.deltaTime;
            SetVel(knockDir * (knockbackSpeed / pixelsPerUnit));
        }
        else if (target)
        {
            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            float dist = (IsTopDown ? toTarget.magnitude : Mathf.Abs(toTarget.x)) * pixelsPerUnit;
            Tick(toTarget, dist);
        }
        else
        {
            SetVel(Vector2.zero);
        }

        sr.flipX = facing < 0;
        if (frames != null && frames.Length >= 10) sr.sprite = frames[FrameIndex()];

        sr.color = flashT > 0f ? hitColor : Color.white;    // brief tint when hit
        flashT -= Time.deltaTime;
    }

    void LateUpdate()
    {
        if (IsTopDown && sortByY) sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
    }

    void Tick(Vector2 toTarget, float dist)
    {
        switch (CurrentState)
        {
            case State.Idle:
                if (dist < sight) { Face(toTarget.x); Set(State.Alert); }
                break;

            case State.Alert:
                Face(toTarget.x);
                if (t >= alertTime) Set(State.Chase);
                break;

            case State.Chase:
                Face(toTarget.x);
                if (!endlessChase && dist > lose)
                {
                    Set(State.Return);
                }
                else if (dist <= attackRange)
                {
                    attackDir = IsTopDown ? toTarget.normalized : new Vector2(facing, 0f);
                    Set(State.Attack);
                }
                else
                {
                    Vector2 dir = IsTopDown ? toTarget.normalized : new Vector2(facing, 0f);
                    SetVel(dir * (chaseSpeed / pixelsPerUnit));
                }
                break;

            case State.Attack:
            {
                int idx = AttackIndex();
                if (idx < 0) { Set(State.Recover); break; }
                if (!hitDone && TryGetReach(idx, out float a, out float b)) CheckHit(a, b);
                break;
            }

            case State.Recover:
                if (t >= recoverTime) Set(endlessChase || dist < lose ? State.Chase : State.Return);
                break;

            case State.Return:
            {
                if (dist < sight) { Face(toTarget.x); Set(State.Alert); break; }
                Vector2 toHome = home - (Vector2)transform.position;
                float d = IsTopDown ? toHome.magnitude : Mathf.Abs(toHome.x);
                if (d < 0.05f) { Set(State.Idle); break; }
                Face(toHome.x);
                Vector2 dir = IsTopDown ? toHome.normalized : new Vector2(Mathf.Sign(toHome.x), 0f);
                SetVel(dir * (returnSpeed / pixelsPerUnit));
                break;
            }
        }
    }

    void CheckHit(float minPx, float maxPx)
    {
        float x0 = minPx / pixelsPerUnit, x1 = maxPx / pixelsPerUnit;
        Collider2D[] cols;

        if (IsTopDown)
        {
            // spore cloud: a circle in the direction the player was when the attack began
            float radius = (x1 - x0) * 0.5f;
            Vector2 center = (Vector2)transform.position + attackDir * ((x0 + x1) * 0.5f);
            dbgCenter = center;
            dbgSize = new Vector2(radius, radius);   // x = radius
            cols = Physics2D.OverlapCircleAll(center, radius, targetMask);
        }
        else
        {
            float h = hitHeight / pixelsPerUnit;
            dbgSize = new Vector2(x1 - x0, h);
            dbgCenter = new Vector2(transform.position.x + facing * (x0 + x1) * 0.5f,
                                    transform.position.y + h * 0.5f);
            cols = Physics2D.OverlapBoxAll(dbgCenter, dbgSize, 0f, targetMask);
        }
        dbgActive = true;

        foreach (Collider2D col in cols)
        {
            if (col.GetComponentInParent<MushroomEnemy>()) continue;    // enemies never hurt each other

            var victim = col.GetComponentInParent<IDamageable>();
            if (victim == null) victim = col.GetComponentInChildren<IDamageable>();
            if (victim == null)
            {
                if (!warnedNoVictim)
                {
                    warnedNoVictim = true;
                    Debug.LogWarning("MushroomEnemy: the attack touched '" + col.name +
                                     "' but it has no IDamageable. Add the Health script to the player.", col);
                }
                continue;
            }

            hitDone = true;
            victim.TakeHit(transform.position, damage);
            break;
        }
    }

    // If Target Mask was left empty, attack whatever layer the target is on.
    void EnsureMask()
    {
        if (targetMask.value == 0 && target) targetMask = 1 << target.gameObject.layer;
    }

    int AttackIndex()
    {
        float ms = t * 1000f, acc = 0f;
        for (int i = 0; i < AttackMs.Length; i++)
        {
            acc += AttackMs[i];
            if (ms < acc) return i;
        }
        return -1;      // finished
    }

    int FrameIndex()
    {
        switch (CurrentState)
        {
            case State.Attack:
                int i = AttackIndex();
                return AttackStart + (i < 0 ? AttackMs.Length - 1 : i);
            case State.Alert:
                return AlertFrame;
            default:
                float rate = CurrentState == State.Chase ? 0.12f
                           : CurrentState == State.Return ? 0.2f
                           : CurrentState == State.Recover ? 0.25f : 0.3f;
                return IdleFrames[(int)(t / rate) % 2];
        }
    }

    // Only flip when the target is clearly to one side, so it doesn't jitter
    // while the player is directly above or below.
    void Face(float dx)
    {
        if (Mathf.Abs(dx) > 0.05f) facing = dx > 0f ? 1 : -1;
    }

    void SetVel(Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = IsTopDown ? v : new Vector2(v.x, rb.linearVelocity.y);
#else
        rb.velocity = IsTopDown ? v : new Vector2(v.x, rb.velocity.y);
#endif
    }

    // Select the enemy in the Scene view to see ranges (yellow = sight, green = lose,
    // red = attack) and, while attacking in Play mode, the live hitbox.
    void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        DrawRange(p, sight, Color.yellow, -0.1f);
        DrawRange(p, lose, Color.green, -0.2f);
        DrawRange(p, attackRange, Color.red, -0.3f);
        if (dbgActive)
        {
            Gizmos.color = Color.red;
            if (IsTopDown) Gizmos.DrawWireSphere(dbgCenter, dbgSize.x);
            else Gizmos.DrawWireCube(dbgCenter, dbgSize);
        }
    }

    void DrawRange(Vector3 p, float px, Color c, float yOffset)
    {
        float r = px / pixelsPerUnit;
        Gizmos.color = c;
        if (view == WorldView.TopDown)
            Gizmos.DrawWireSphere(p, r);
        else
            Gizmos.DrawLine(new Vector3(p.x - r, p.y + yOffset, p.z), new Vector3(p.x + r, p.y + yOffset, p.z));
    }
}
