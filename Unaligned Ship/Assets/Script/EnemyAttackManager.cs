using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAttackManager : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 3f;
    public float attackCooldown = 2f;
    public LayerMask playerLayer;

    [Header("Animation Settings")]
    public string attackTriggerName = "Attack";

    private Transform player;
    private Animator animator;
    private float lastAttackTime;
    private bool playerInRange;

    void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastAttackTime = -attackCooldown; // Allow immediate first attack
    }

    void Update()
    {
        // Check player distance
        playerInRange = Vector3.Distance(transform.position, player.position) <= detectionRange;

        // Trigger attack when player is in range and cooldown is ready
        if (playerInRange && Time.time - lastAttackTime >= attackCooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger(attackTriggerName);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}