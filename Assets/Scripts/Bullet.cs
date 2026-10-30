using UnityEngine;

/// <summary>
/// Projectile spawned by Gun. Moves in a straight line and raycasts each step, so even very
/// fast bullets can't tunnel through thin walls.
///
/// PREFAB: an empty GameObject with a SpriteRenderer (Unity's built-in Square sprite, scaled
/// to something small, works fine) and this script. The sprite should point RIGHT.
/// No collider or Rigidbody is needed.
/// </summary>
public class Bullet : MonoBehaviour
{
    Vector2 dir;
    float speed;
    int damage;
    float remaining;
    LayerMask hitMask;

    public void Init(Vector2 direction, float bulletSpeed, int bulletDamage, float range, LayerMask mask)
    {
        dir = direction.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        remaining = range;
        hitMask = mask;
    }

    void Update()
    {
        float step = Mathf.Min(speed * Time.deltaTime, remaining);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, step, hitMask);

        if (hit.collider)
        {
            var victim = hit.collider.GetComponentInParent<IDamageable>();
            // "from" is a point just behind the impact, so knockback pushes along the bullet's path
            if (victim != null) victim.TakeHit(hit.point - dir, damage);
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(dir * step);
        remaining -= step;
        if (remaining <= 0f) Destroy(gameObject);
    }
}
