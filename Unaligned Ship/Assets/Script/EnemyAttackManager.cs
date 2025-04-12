using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class EnemyAttackManager : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 3f;
    public float attackCooldown = 2f;
    public LayerMask playerLayer;

    [Header("Animation Settings")]
    public string attackTriggerName = "Attack";
    public string chokeTriggerName = "Choke";

    [Header("Choke Settings")]
    public GameObject chokeCamera;
    public UnityEvent onChoke;
    public UnityEvent onChokeEnd;
    public int chokeDamage = 2;
    public int hitsBeforeChoke = 2;
    public float chokeCooldown = 5f;

    private Transform player;
    private Animator animator;
    private float lastAttackTime;
    private float chokeStartTime;
    private float lastChokeTime;
    private bool playerInRange;
    private int hitCount = 0;
    private PlayerHealth playerHealth;
    private bool isChoking = false;
    private EnemyPatrol enemyPatrol;

    void Start()
    {
        enemyPatrol = GetComponent<EnemyPatrol>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerHealth = player.GetComponent<PlayerHealth>();
        lastAttackTime = -attackCooldown; // Allow immediate first attack
        lastChokeTime = -chokeCooldown; // Allow immediate first choke
        
        if (chokeCamera != null)
            chokeCamera.SetActive(false);
    }

    void Update()
    {
        // Don't attack if in patrol mode and not in combat
        if (enemyPatrol != null && !enemyPatrol.IsInCombat)
            return;

        // Check player distance
        playerInRange = Vector3.Distance(transform.position, player.position) <= detectionRange;

        // If we're choking, check if animation finished
        if (isChoking && Time.time - chokeStartTime > 10f) // 10 second timeout
        {
            Debug.LogWarning("Choke timeout - forcing end");
            EndChoke();
        }

        // Normal attack logic
        if (playerInRange && Time.time - lastAttackTime >= attackCooldown)
        {
            if (hitCount >= hitsBeforeChoke && Time.time - lastChokeTime >= chokeCooldown)
            {
                ChokeAttack();
            }
            else
            {
                Attack();
            }
        }

        
    }

    bool IsChokeAnimationPlaying()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsTag("Choke") || 
               animator.GetAnimatorTransitionInfo(0).anyState;
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger(attackTriggerName);
    }

    void ChokeAttack()
    {
        chokeStartTime = Time.time;
        isChoking = true;
        lastAttackTime = Time.time;
        lastChokeTime = Time.time;
        animator.ResetTrigger(attackTriggerName); // Clear any attack triggers
        animator.SetTrigger(chokeTriggerName);
        
        if (chokeCamera != null)
            chokeCamera.SetActive(true);
            
        onChoke.Invoke();
        
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(chokeDamage);
        }
        
        hitCount = 0;
        
        Debug.Log("Choke started");
    }


    public void EndChoke()
    {
        if (!isChoking) return; // Already ended
        
        isChoking = false;
        animator.ResetTrigger(chokeTriggerName); // Clear choke trigger
        
        if (chokeCamera != null)
            chokeCamera.SetActive(false);
            
        onChokeEnd.Invoke();
        
        Debug.Log("Choke ended");
    }

    // Call this method from the animation event when a normal attack hits
    public void RegisterHit()
    {
        hitCount++;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}