using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Top-down gun. Attach this to the gun object (a child of the player).
///
/// It aims at the mouse, orbits the player at a fixed radius, fires bullets with fire rate,
/// spread (set Pellets > 1 for a shotgun), magazine + reserve ammo, reload, and recoil.
///
/// CONTROLS: hold/click Left Mouse to fire, R to reload.
/// Works with both the old Input Manager and the new Input System package.
///
/// SETUP
/// 1. Player (empty or sprite)
///      +-- Gun  (SpriteRenderer with the gun sprite drawn pointing RIGHT, plus this script)
///            +-- Muzzle (empty child at the tip of the barrel)
/// 2. Make a bullet prefab (see Bullet.cs) and assign it, plus the Muzzle.
/// 3. Hit Mask = the layers bullets can hit: your Enemy layer and your walls. NOT the player.
///    Enemies need a Collider2D on that layer and a script implementing IDamageable
///    (MushroomEnemy already does).
/// 4. Draw the gun above the player: give its SpriteRenderer a higher Order in Layer.
/// </summary>
public class Gun : MonoBehaviour
{
    [Header("Projectile")]
    public Bullet bulletPrefab;
    public Transform muzzle;            // empty child at the barrel tip; defaults to this transform
    public LayerMask hitMask;           // enemies + walls (NOT the player)
    public float bulletSpeed = 16f;     // world units per second
    public int damage = 1;
    public float range = 10f;           // world units before the bullet disappears

    [Header("Firing")]
    public bool automatic = true;       // hold to keep firing; off = one shot per click
    public float fireRate = 6f;         // shots per second
    public int pellets = 1;             // bullets per shot; e.g. 6 for a shotgun
    public float spread = 2f;           // total cone in degrees (e.g. 25 for a shotgun)

    [Header("Ammo")]
    public int magazineSize = 12;
    public int reserveAmmo = 60;        // spare ammo; -1 = infinite
    public float reloadTime = 1.2f;
    public bool autoReload = true;      // reload automatically when the magazine runs dry

    [Header("Aiming")]
    public Camera cam;                  // defaults to Camera.main
    public Transform orbitCenter;       // usually the player; defaults to this object's parent
    public float orbitRadius = 0.6f;    // how far from that centre the gun floats
    public float recoilDistance = 0.12f;
    public float recoilRecover = 1.5f;  // units per second

    [Header("Events (hook up sounds, muzzle flash, UI)")]
    public UnityEvent onFire;
    public UnityEvent onEmpty;
    public UnityEvent onReloadStart;
    public UnityEvent onReloadDone;

    [Header("Debug")]
    public bool showDebugHud = true;

    public int AmmoInMag { get; private set; }
    public int ReserveAmmo { get { return reserveAmmo; } }
    public bool IsReloading { get; private set; }
    public Vector2 AimDirection { get; private set; }

    float nextFireTime;
    float reloadEnd;
    float recoil;

    void Awake()
    {
        if (!cam) cam = Camera.main;
#if UNITY_2023_1_OR_NEWER
        if (!cam) cam = FindFirstObjectByType<Camera>();
#else
        if (!cam) cam = FindObjectOfType<Camera>();
#endif
        if (!cam)
            Debug.LogWarning("Gun: no camera found, so it can't aim. Tag your camera 'MainCamera' or assign Cam.", this);

        if (!orbitCenter) orbitCenter = transform.parent;
        if (!orbitCenter)
            Debug.LogWarning("Gun: it is not a child of the player and Orbit Center is empty, so it will rotate but not move around the player.", this);

        if (!muzzle) muzzle = transform;
        AmmoInMag = magazineSize;
        AimDirection = Vector2.right;
    }

    void Update()
    {
        recoil = Mathf.MoveTowards(recoil, 0f, recoilRecover * Time.deltaTime);
        Aim();

        if (IsReloading && Time.time >= reloadEnd) FinishReload();
        if (ReloadPressed()) StartReload();

        bool wantsFire = automatic ? FireHeld() : FirePressed();
        if (wantsFire) TryFire();
    }

    // ------------------------------------------------------------ aiming

    void Aim()
    {
        if (!cam) return;

        Vector2 mouse = MouseScreen();
        float z = Mathf.Abs(cam.transform.position.z);
        Vector2 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, z));
        Vector2 origin = orbitCenter ? (Vector2)orbitCenter.position : (Vector2)transform.position;

        Vector2 d = world - origin;
        if (d.sqrMagnitude < 0.0001f) return;
        AimDirection = d.normalized;

        float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (orbitCenter)
        {
            Vector2 p = origin + AimDirection * (orbitRadius - recoil);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }

        // keep the sprite upright when aiming left
        Vector3 s = transform.localScale;
        s.y = Mathf.Abs(s.y) * (AimDirection.x < 0f ? -1f : 1f);
        transform.localScale = s;
    }

    // ------------------------------------------------------------ firing

    void TryFire()
    {
        if (IsReloading || Time.time < nextFireTime || !bulletPrefab) return;

        if (AmmoInMag <= 0)
        {
            nextFireTime = Time.time + 0.25f;       // throttle the empty click
            if (autoReload && CanReload()) StartReload();
            else onEmpty.Invoke();
            return;
        }

        Shoot();
    }

    void Shoot()
    {
        float baseAngle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i < Mathf.Max(1, pellets); i++)
        {
            float a = baseAngle + Random.Range(-spread * 0.5f, spread * 0.5f);
            float rad = a * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            Bullet b = Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0f, 0f, a));
            b.Init(dir, bulletSpeed, damage, range, hitMask);
        }

        AmmoInMag--;
        nextFireTime = Time.time + 1f / fireRate;
        recoil = recoilDistance;
        onFire.Invoke();
    }

    // ------------------------------------------------------------ reloading

    bool CanReload()
    {
        return !IsReloading && AmmoInMag < magazineSize && reserveAmmo != 0;
    }

    public void StartReload()
    {
        if (!CanReload()) return;
        IsReloading = true;
        reloadEnd = Time.time + reloadTime;
        onReloadStart.Invoke();
    }

    void FinishReload()
    {
        int needed = magazineSize - AmmoInMag;
        int take = reserveAmmo < 0 ? needed : Mathf.Min(needed, reserveAmmo);
        AmmoInMag += take;
        if (reserveAmmo >= 0) reserveAmmo -= take;
        IsReloading = false;
        onReloadDone.Invoke();
    }

    /// <summary>Call from an ammo pickup.</summary>
    public void AddAmmo(int amount)
    {
        if (reserveAmmo >= 0) reserveAmmo += amount;
    }

    // ------------------------------------------------------------ input (old + new Input System)

    static Vector2 MouseScreen()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    static bool FireHeld()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    static bool FirePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    static bool ReloadPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    // ------------------------------------------------------------ debug

    void OnGUI()
    {
        if (!showDebugHud) return;
        string reserve = reserveAmmo < 0 ? "inf" : reserveAmmo.ToString();
        string text = IsReloading ? "Reloading..." : AmmoInMag + " / " + reserve;
        GUI.Label(new Rect(10f, 10f, 300f, 24f), text);
    }
}
