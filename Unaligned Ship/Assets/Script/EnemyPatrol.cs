using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic; // Needed for List<>

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Base Detection Settings")]
    public float baseDetectionRadius = 10f;
    public float baseChaseRadius = 4f;
    [Range(0, 360)] public float baseFovAngle = 180f;
    public float eyeHeight = 1f;
    public LayerMask playerLayer;
    public LayerMask obstructionLayers;

    [Header("Player Input Modifiers")]
    public float runDetectionRadius = 15f; // Shift pressed
    public float sneakDetectionRadius = 6f; // CTRL pressed
    public float runFovAngle = 360f;
    public float sneakFovAngle = 90f;

    [Header("Patrol Settings")]
    public float patrolRadius = 15f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 initialPosition;
    private Transform playerTarget;
    private bool isMoving;
    private float waitTimer;
    private const string WALK_ANIM_PARAM = "isWalking";

    public bool IsInCombat { get; private set; }

    

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        initialPosition = transform.position;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    void Update()
    {
        DetectPlayer();
        HandleMovement();
    }

    void DetectPlayer()
    {
        if (playerTarget != null)
        {
            IsInCombat = true;
        }
        else
        {
            IsInCombat = false;
        }

        // Get current detection parameters based on input
        float currentRadius;
        float currentFov;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentRadius = runDetectionRadius;
            currentFov = runFovAngle;
        }
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            currentRadius = sneakDetectionRadius;
            currentFov = sneakFovAngle;
        }
        else
        {
            currentRadius = baseDetectionRadius;
            currentFov = baseFovAngle;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, playerLayer);
        
        foreach (Collider hit in hits)
        {
            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            // Skip FOV check if in 360-degree mode (runFovAngle)
            if (currentFov >= 360f || angleToTarget < currentFov / 2)
            {
                Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
                Vector3 targetPosition = hit.transform.position + Vector3.up * 0.5f;

                if (!Physics.Linecast(eyePosition, targetPosition, obstructionLayers))
                {
                    playerTarget = hit.transform;
                    break;
                }
            }
        }

        // Target loss conditions
        if (playerTarget != null && 
            (Vector3.Distance(transform.position, playerTarget.position) > baseChaseRadius ||
             (!Input.GetKey(KeyCode.LeftShift) && !IsTargetInFront(playerTarget, baseFovAngle))))
        {
            playerTarget = null;
        }
    }

    bool IsTargetInFront(Transform target, float fovAngle)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        return angleToTarget < fovAngle / 2;
    }

    void HandleMovement()
    {
        if (playerTarget != null)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void ChasePlayer()
    {
        if (playerTarget != null)
        {
            agent.SetDestination(playerTarget.position);
            UpdateAnimation(true);
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
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize current detection parameters
        float currentRadius = baseDetectionRadius;
        float currentFov = baseFovAngle;

        if (Application.isPlaying)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentRadius = runDetectionRadius;
                currentFov = runFovAngle;
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                currentRadius = sneakDetectionRadius;
                currentFov = sneakFovAngle;
            }
        }

        // Draw detection sphere
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, currentRadius);

        // Draw FOV cone (unless in 360 mode)
        if (currentFov < 360f)
        {
            Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
            Vector3 forward = transform.forward * currentRadius;
            Quaternion leftRot = Quaternion.AngleAxis(-currentFov / 2, Vector3.up);
            Quaternion rightRot = Quaternion.AngleAxis(currentFov / 2, Vector3.up);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(eyePosition, eyePosition + leftRot * forward);
            Gizmos.DrawLine(eyePosition, eyePosition + rightRot * forward);
        }

        // Draw patrol area
        Gizmos.color = new Color(0, 0, 1, 0.1f);
        Gizmos.DrawSphere(initialPosition, patrolRadius);
    }
}