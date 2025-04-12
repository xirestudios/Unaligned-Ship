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
    private float lastChokeTime;
    private bool playerInRange;
    private int hitCount = 0;
    private PlayerHealth playerHealth;
    private bool isChoking = false;

    void Start()
    {
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
        // Check player distance
        playerInRange = Vector3.Distance(transform.position, player.position) <= detectionRange;

        // Don't attack if currently choking
        if (isChoking) return;

        // Trigger attack when player is in range and cooldown is ready
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

    void Attack()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger(attackTriggerName);
    }

    void ChokeAttack()
    {
        isChoking = true;
        lastAttackTime = Time.time;
        lastChokeTime = Time.time;
        animator.SetTrigger(chokeTriggerName);
        
        // Activate choke camera
        if (chokeCamera != null)
            chokeCamera.SetActive(true);
            
        // Invoke choke event
        onChoke.Invoke();
        
        // Apply choke damage
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(chokeDamage);
        }
        
        // Reset hit counter
        hitCount = 0;
    }

    // Call this method from the animation event when choke animation ends
    public void EndChoke()
    {
        isChoking = false;
        
        // Deactivate choke camera
        if (chokeCamera != null)
            chokeCamera.SetActive(false);
            
        // Invoke choke end event
        onChokeEnd.Invoke();
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