using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(Animator), typeof(EnemyPatrol))]
public class EnemyAttackManager : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    public LayerMask playerLayer;
    public LayerMask hidePlayerLayer;

    [Header("Animation Settings")]
    public string attackTriggerName = "Attack";
    public string chokeTriggerName = "Choke";
    public string groundAttackTriggerName = "GroundAttack";

    [Header("Choke Settings")]
    public GameObject chokeCamera;
    public UnityEvent onChoke;
    public UnityEvent onChokeEnd;
    public int chokeDamage = 2;
    public int hitsBeforeChoke = 2;
    public float chokeCooldown = 5f;

    [Header("Ground Attack Settings")]
    public UnityEvent onGroundAttack;
    public UnityEvent onGroundAttackEnd;
    public int groundAttackDamage = 1;
    public float groundAttackDuration = 3f;
    public float maxGroundAttackDistance = 5f;
    public float stationaryTimeThreshold = 3f; // Time player must be stationary to trigger ground attack
    public float groundAttackCooldown = 8f; // New cooldown variable
    private float lastGroundAttackTime = -10f; // Initialize to allow immediate first attack    

    private Transform player;
    private Animator animator;
    private EnemyPatrol enemyPatrol;
    private NavMeshAgent navAgent;
    private float lastAttackTime;
    private float chokeStartTime;
    private float lastChokeTime;
    private float groundAttackStartTime;
    private int hitCount = 0;
    private PlayerHealth playerHealth;
    private bool isChoking = false;
    private bool isGroundAttacking = false;
    private bool isPlayerHidingGlobally = false;
    private Vector3 lastPlayerPosition;
    private float playerStationaryTime = 0f;
    private bool wasPlayerHidingLastFrame = false;

    [Header("Hide Detection")]
    public PlayerHideZone playerHideZone; // Assign in inspector

    void Start()
    {
        animator = GetComponent<Animator>();
        enemyPatrol = GetComponent<EnemyPatrol>();
        navAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerHealth = player.GetComponent<PlayerHealth>();

        lastAttackTime = -attackCooldown;
        lastChokeTime = -chokeCooldown;
        lastPlayerPosition = player.position;
        Debug.Log($"Initial player position: {lastPlayerPosition}");

        if (chokeCamera != null)
            chokeCamera.SetActive(false);
    }
    private void OnEnable()
    {
        PlayerHideZone.OnPlayerHideStatusChanged += HandleHideStatusChange;
    }

    private void OnDisable()
    {
        PlayerHideZone.OnPlayerHideStatusChanged -= HandleHideStatusChange;
    }
    private void HandleHideStatusChange(bool isHiding)
    {
        isPlayerHidingGlobally = isHiding;
        Debug.Log($"Player hide status changed: {isHiding}");

        if (!isHiding)
        {
            // Reset tracking when player exits hiding
            playerStationaryTime = 0f;
        }
    }

    void Update()
{
    if (!enemyPatrol.IsInCombat)
    {
        // Debug.Log("Not in combat - skipping attack logic");
        return;
    }

    // Get the actual layer name for debugging
    string playerLayerName = LayerMask.LayerToName(player.gameObject.layer);
    bool isPlayerHiding = isPlayerHidingGlobally && playerLayerName == "HidePlayer"; // Combined check

    float distanceToPlayer = Vector3.Distance(transform.position, player.position);

    Debug.Log($"Update - Player layer: {playerLayerName}, " +
            $"Hiding: {isPlayerHiding}, " +
            $"Distance: {distanceToPlayer:F2}, " +
            $"Position: {player.position}");

    // Track player movement when hiding
    if (isPlayerHiding)
    {
        TrackPlayerStationaryTime();

        bool groundAttackOnCooldown = Time.time - lastGroundAttackTime < groundAttackCooldown;
        
        if (ShouldStartGroundAttack() && !groundAttackOnCooldown)
        {
            StartGroundAttack();
        }
        else if (groundAttackOnCooldown)
        {
            Debug.Log($"Ground attack on cooldown: {groundAttackCooldown - (Time.time - lastGroundAttackTime):F1}s remaining");
        }
    }
    else
    {
        playerStationaryTime = 0f;
    }

    // Timeout checks
    if (isChoking && Time.time - chokeStartTime > 10f)
    {
        EndChoke();
    }
    if (isGroundAttacking && Time.time - groundAttackStartTime > groundAttackDuration)
    {
        EndGroundAttack();
    }

    // Normal attack logic (only if player isn't hiding and not already attacking)
    if (!isPlayerHiding && !isChoking && !isGroundAttacking &&
        distanceToPlayer <= attackRange &&
        Time.time - lastAttackTime >= attackCooldown)
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

    private void TrackPlayerStationaryTime()
    {
        float distanceMoved = Vector3.Distance(player.position, lastPlayerPosition);

        if (distanceMoved > 0.1f) // Player moved significantly
        {
            playerStationaryTime = 0f;
            lastPlayerPosition = player.position;
            Debug.Log($"Player moved, resetting stationary timer");
        }
        else
        {
            playerStationaryTime += Time.deltaTime;
            Debug.Log($"Player stationary for: {playerStationaryTime:F1}s");
        }
    }
    private bool ShouldStartGroundAttack()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= maxGroundAttackDistance;
        bool stationaryEnough = playerStationaryTime >= stationaryTimeThreshold;
        bool canAttack = !isGroundAttacking && !isChoking;

        Debug.Log($"GroundAttack Check - InRange: {inRange}, Stationary: {stationaryEnough}, CanAttack: {canAttack}");
        return inRange && stationaryEnough && canAttack;
    }

    private bool IsPlayerActuallyHiding()
    {
        // Double verification method
        bool layerCheck = player.gameObject.layer == LayerMask.NameToLayer("HidePlayer");
        bool zoneCheck = IsPlayerInHideZone();

        Debug.Log($"Hide check - Layer: {layerCheck}, Zone: {zoneCheck}");
        return layerCheck && zoneCheck;
    }
    private bool IsPlayerInHideZone()
    {
        if (playerHideZone == null)
        {
            Debug.LogWarning("PlayerHideZone reference not set!");
            return false;
        }

        // This assumes your PlayerHideZone has a way to check active status
        // You might need to add this to your PlayerHideZone script:
        // public bool IsPlayerHiding() { return /* your logic */; }
        return playerHideZone.IsPlayerHiding();
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

        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(chokeTriggerName);

        if (chokeCamera != null)
            chokeCamera.SetActive(true);

        onChoke.Invoke();

        if (playerHealth != null)
            playerHealth.TakeDamage(chokeDamage);

        hitCount = 0;
    }

    void StartGroundAttack()
    {
        Debug.Log("Starting ground attack!");

        // Stop movement during ground attack
        navAgent.isStopped = true;

        groundAttackStartTime = Time.time;
        lastGroundAttackTime = Time.time; // Set the cooldown timer
        isGroundAttacking = true;
        lastAttackTime = Time.time;

        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(groundAttackTriggerName);

        onGroundAttack.Invoke();

        if (playerHealth != null)
            playerHealth.TakeDamage(groundAttackDamage);
    }

    void EndGroundAttack()
    {
        if (!isGroundAttacking) return;

        isGroundAttacking = false;
        navAgent.isStopped = false;
        playerStationaryTime = 0f;

        animator.ResetTrigger(groundAttackTriggerName);
        onGroundAttackEnd.Invoke();

        Debug.Log($"Ground attack completed. Cooldown started: {groundAttackCooldown}s");
    }

    public void EndChoke()
    {
        if (!isChoking) return;

        isChoking = false;
        animator.ResetTrigger(chokeTriggerName);

        if (chokeCamera != null)
            chokeCamera.SetActive(false);

        onChokeEnd.Invoke();
    }

    public void RegisterHit()
    {
        hitCount++;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxGroundAttackDistance);
    }
}