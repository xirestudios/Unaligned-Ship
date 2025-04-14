using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyMannequinPatrol : MonoBehaviour
{
    [Header("Mode Settings")]
    public bool isMannequin = true;
    public KeyCode toggleKey = KeyCode.M;

    [Header("Detection Settings")]
    public float detectionRadius = 10f;
    public float chaseRadius = 15f;
    [Range(0, 360)] public float fovAngle = 120f;
    public float eyeHeight = 1.5f;
    public LayerMask playerLayer;
    public LayerMask obstructionLayers;

    [Header("Mannequin Behavior")]
    public float visibilityCheckInterval = 0.1f;
    public float playerLookAngleThreshold = 30f;

    [Header("Patrol Settings")]
    public float patrolRadius = 20f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 initialPosition;
    private Transform player;
    private bool isMoving;
    private float waitTimer;
    private float visibilityCheckTimer;
    private bool isVisibleToPlayer;
    private const string WALK_ANIM_PARAM = "isWalking";

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        initialPosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
        visibilityCheckTimer = visibilityCheckInterval;
    }

    void Update()
    {
        // Toggle mode with keyboard input
        if (Input.GetKeyDown(toggleKey))
        {
            isMannequin = !isMannequin;
            Debug.Log($"Switched to {(isMannequin ? "Mannequin" : "Normal")} mode");
        }

        if (isMannequin)
        {
            CheckPlayerVisibility();
        }
        HandleMovement();
    }

    void CheckPlayerVisibility()
    {
        visibilityCheckTimer -= Time.deltaTime;
        if (visibilityCheckTimer <= 0)
        {
            visibilityCheckTimer = visibilityCheckInterval;
            
            // Check if mannequin is on screen
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
            bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

            // Check if player is looking at mannequin
            Vector3 directionToMannequin = (transform.position - player.position).normalized;
            float angleToMannequin = Vector3.Angle(player.forward, directionToMannequin);
            bool playerLooking = angleToMannequin < playerLookAngleThreshold;

            // Check line of sight
            bool hasLineOfSight = false;
            if (onScreen || playerLooking)
            {
                Vector3 playerEyePosition = player.position + Vector3.up * 1.6f;
                Vector3 mannequinEyePosition = transform.position + Vector3.up * eyeHeight;
                hasLineOfSight = !Physics.Linecast(playerEyePosition, mannequinEyePosition, obstructionLayers);
            }

            isVisibleToPlayer = (onScreen || playerLooking) && hasLineOfSight;
        }
    }

    void HandleMovement()
    {
        // Freeze behavior when in mannequin mode and player is looking
        if (isMannequin && isVisibleToPlayer)
        {
            agent.isStopped = true;
            UpdateAnimation(false);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Chase behavior
        if (distanceToPlayer < chaseRadius && distanceToPlayer > agent.stoppingDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            UpdateAnimation(true);
        }
        // Patrol behavior
        else
        {
            agent.isStopped = false;
            Patrol();
        }
    }

    void Patrol()
    {
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            UpdateAnimation(false);

            if (waitTimer <= 0)
            {
                SetNewRandomDestination();
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
            else
            {
                waitTimer -= Time.deltaTime;
            }
        }
        else
        {
            UpdateAnimation(true);
        }
    }

    void SetNewRandomDestination()
    {
        Vector3 randomPoint = initialPosition + Random.insideUnitSphere * patrolRadius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void UpdateAnimation(bool moving)
    {
        if (isMoving != moving)
        {
            isMoving = moving;
            animator.SetBool(WALK_ANIM_PARAM, moving);
            
            if (!moving)
            {
                animator.Update(0f); // Force animation update
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection sphere
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, detectionRadius);

        // Draw FOV cone
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 forward = transform.forward * detectionRadius;
        Quaternion leftRot = Quaternion.AngleAxis(-fovAngle / 2, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(fovAngle / 2, Vector3.up);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(eyePosition, eyePosition + leftRot * forward);
        Gizmos.DrawLine(eyePosition, eyePosition + rightRot * forward);

        // Draw patrol area
        Gizmos.color = new Color(0, 0, 1, 0.1f);
        Gizmos.DrawSphere(initialPosition, patrolRadius);
    }

    // Public method to toggle mode programmatically
    public void ToggleMannequinMode(bool enableMannequin)
    {
        isMannequin = enableMannequin;
    }
}