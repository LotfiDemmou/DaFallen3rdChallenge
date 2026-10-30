using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies on a circle around the player (top-down worlds).
///
/// SETUP: put this on an empty GameObject, assign the enemy prefab (a prefab with
/// MushroomEnemy on it), and tag your player "Player" (or assign Player).
/// Spawn distance is in world units: sprite pixels / Pixels Per Unit
/// (e.g. 192 px at 16 PPU = 12 units).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("What to spawn")]
    public MushroomEnemy enemyPrefab;
    public Transform player;                    // defaults to the object tagged "Player"

    [Header("Ring around the player")]
    public float spawnDistance = 12f;           // radius of the circle (world units)
    public float distanceJitter = 0f;           // +/- random variation of the radius
    public bool keepOffScreen = true;           // skip points the main camera can see
    public LayerMask blockedMask;               // walls/obstacles: spawn points touching these are skipped
    public float clearRadius = 0.5f;            // how much free space a spawn point needs
    public int maxTries = 12;                   // attempts to find a valid point per spawn

    [Header("Timing")]
    public float startDelay = 1f;
    public float spawnInterval = 3f;            // seconds between waves
    public int perWave = 1;                     // enemies per wave
    public int maxAlive = 8;                    // cap on living enemies
    public float rampMultiplier = 0.97f;        // interval *= this after each wave (1 = no ramp)
    public float minInterval = 0.6f;

    [Header("Spawned enemies")]
    public bool endlessChase = true;            // they never give up and walk home

    readonly List<MushroomEnemy> alive = new List<MushroomEnemy>();
    Camera cam;
    float timer, interval;
    bool warned;

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        cam = Camera.main;
        interval = spawnInterval;
        timer = startDelay;
    }

    void Update()
    {
        if (!player || !enemyPrefab) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        alive.RemoveAll(e => !e);                       // drop destroyed enemies
        int n = Mathf.Min(perWave, maxAlive - alive.Count);
        for (int i = 0; i < n; i++) SpawnOne();

        interval = Mathf.Max(minInterval, interval * rampMultiplier);
        timer = interval;
    }

    /// <summary>Spawns one enemy on the ring. Returns null if no valid point was found.</summary>
    public MushroomEnemy SpawnOne()
    {
        Vector2 pos;
        if (!TryFindSpawnPoint(out pos)) return null;

        MushroomEnemy e = Instantiate(enemyPrefab, pos, Quaternion.identity);
        e.BeginChase(player, endlessChase);
        alive.Add(e);
        return e;
    }

    bool TryFindSpawnPoint(out Vector2 pos)
    {
        Vector2 center = player.position;
        for (int i = 0; i < maxTries; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float r = Mathf.Max(0.1f, spawnDistance + Random.Range(-distanceJitter, distanceJitter));
            pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;

            if (keepOffScreen && OnScreen(pos)) continue;
            if (blockedMask.value != 0 && Physics2D.OverlapCircle(pos, clearRadius, blockedMask)) continue;
            return true;
        }

        if (!warned)
        {
            warned = true;
            Debug.LogWarning("EnemySpawner: no valid spawn point found. If the camera can see the whole " +
                             "ring, increase Spawn Distance or turn off Keep Off Screen.", this);
        }
        pos = default(Vector2);
        return false;
    }

    bool OnScreen(Vector2 pos)
    {
        if (!cam) return false;
        const float margin = 0.05f;                     // also avoid points just outside the edge
        Vector3 v = cam.WorldToViewportPoint(pos);
        return v.x > -margin && v.x < 1f + margin && v.y > -margin && v.y < 1f + margin;
    }

    void OnDrawGizmosSelected()
    {
        if (!player) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position, spawnDistance);
        if (distanceJitter > 0f)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawWireSphere(player.position, spawnDistance + distanceJitter);
            Gizmos.DrawWireSphere(player.position, Mathf.Max(0.1f, spawnDistance - distanceJitter));
        }
    }
}
