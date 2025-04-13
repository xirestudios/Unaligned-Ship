using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class EnemyStun : MonoBehaviour
{
    [Header("Stun Settings")]
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float stunCooldown = 5f;
    [SerializeField] private float movementSlowFactor = 0.3f;
    [SerializeField] private string stunTriggerName = "Stunned";

    [Header("References")]
    [SerializeField] private EnemyPatrol enemyPatrol;
    [SerializeField] private EnemyAttackManager enemyAttackManager;

    private NavMeshAgent navAgent;
    private Animator animator;
    private float originalSpeed;
    private float lastStunTime = -10f; // Initialize to allow immediate first stun
    private bool isStunned = false;
    private bool isOnCooldown = false;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        originalSpeed = navAgent.speed;

        // Auto-get references if not set in inspector
        if (enemyPatrol == null) enemyPatrol = GetComponent<EnemyPatrol>();
        if (enemyAttackManager == null) enemyAttackManager = GetComponent<EnemyAttackManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Throwable"))
        {
            HandleStun();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Throwable"))
        {
            HandleStun();
        }
    }

    private void HandleStun()
    {
        // Check if we can be stunned (not already stunned and cooldown expired)
        if (isStunned || isOnCooldown) return;

        // Check cooldown
        if (Time.time - lastStunTime < stunCooldown)
        {
            Debug.Log($"Stun on cooldown: {stunCooldown - (Time.time - lastStunTime):F1}s remaining");
            return;
        }

        StartStun();
    }

    private void StartStun()
    {
        isStunned = true;
        lastStunTime = Time.time;

        // Slow down movement
        navAgent.speed = originalSpeed * movementSlowFactor;

        // Play stun animation
        if (!string.IsNullOrEmpty(stunTriggerName))
        {
            animator.SetTrigger(stunTriggerName);
        }

        // Disable enemy behaviors
        if (enemyPatrol != null)
        {
            enemyPatrol.enabled = false;
        }

        if (enemyAttackManager != null)
        {
            enemyAttackManager.enabled = false;
        }

        // Stop current movement
        if (navAgent.isActiveAndEnabled)
        {
            navAgent.isStopped = true;
        }

        Debug.Log("Enemy stunned!");

        // Start cooldown timer
        Invoke(nameof(EndStun), stunDuration);
    }

    private void EndStun()
    {
        if (!isStunned) return;

        isStunned = false;
        isOnCooldown = true;

        // Restore original speed
        navAgent.speed = originalSpeed;

        // Re-enable movement
        if (navAgent.isActiveAndEnabled)
        {
            navAgent.isStopped = false;
        }

        // Re-enable enemy behaviors
        if (enemyPatrol != null)
        {
            enemyPatrol.enabled = true;
        }

        if (enemyAttackManager != null)
        {
            enemyAttackManager.enabled = true;
        }

        Debug.Log("Enemy recovered from stun!");

        // Start cooldown
        Invoke(nameof(ResetCooldown), stunCooldown - stunDuration);
    }

    private void ResetCooldown()
    {
        isOnCooldown = false;
        Debug.Log("Stun cooldown ready");
    }

    // For debugging
    private void OnGUI()
    {
        if (isStunned)
        {
            GUI.Label(new Rect(10, 30, 200, 20), "ENEMY STUNNED!");
        }
        else if (isOnCooldown)
        {
            GUI.Label(new Rect(10, 30, 250, 20), $"Stun Cooldown: {stunCooldown - (Time.time - lastStunTime):F1}s");
        }
    }
}