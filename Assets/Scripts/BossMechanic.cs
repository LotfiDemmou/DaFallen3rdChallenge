
using UnityEngine;
using UnityEngine.AI;

public class MushroomBoss : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    private NavMeshAgent agent;

    [Header("Boss Stats")]
    public float health = 300f;
    public float moveSpeed = 2f;

    [Header("Combat")]
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    public float attackDamage = 20f;

    private float nextAttackTime;
    private bool isDead;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > attackRange)
        {
            ChasePlayer();
        }
        else
        {
            AttackPlayer();
        }
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);

        animator.SetBool("IsWalking", true);
    }

    void AttackPlayer()
    {
        agent.isStopped = true;
        animator.SetBool("IsWalking", false);

        // Face the player
        Vector3 direction = player.position - transform.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 5f
            );
        }

        if (Time.time >= nextAttackTime)
        {
            animator.SetTrigger("Attack");
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // Called through an Animation Event
    public void DealDamage()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= attackRange + 0.5f)
        {
            IDamageable target = player.GetComponent<IDamageable>();

            if (target != null);
                //target.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;

        animator.SetTrigger("Hurt");

        if (health <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;

        agent.isStopped = true;
        agent.enabled = false;

        animator.SetTrigger("Death");
    }
}
