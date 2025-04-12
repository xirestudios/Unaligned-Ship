using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 10f;
    public float chaseRadius = 4f;
    public LayerMask playerLayer;

    [Header("Patrol Settings")]
    public float patrolRadius = 15f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 initialPosition;
    private Transform playerTarget;
    private bool isMoving;
    private float waitTimer; // Added missing variable
    private const string WALK_ANIM_PARAM = "isWalking";

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        initialPosition = transform.position;
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        waitTimer = 0; // Initialize timer
    }

    void Update()
    {
        DetectPlayer();
        HandleMovement();
    }

    void DetectPlayer()
{
    // Only detect objects on Player layer (not HidePlayer)
    Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, 1 << LayerMask.NameToLayer("Player"));
    
    if (hits.Length > 0)
    {
        playerTarget = hits[0].transform;
    }
    else if (playerTarget != null && 
             (Vector3.Distance(transform.position, playerTarget.position) > chaseRadius || 
              playerTarget.gameObject.layer == LayerMask.NameToLayer("HidePlayer")))
    {
        playerTarget = null;
    }
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
        agent.SetDestination(playerTarget.position);
        UpdateAnimation(true);
    }

    void Patrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            UpdateAnimation(false);
            
            if (waitTimer <= 0) 
            {
                SetNewRandomDestination();
                waitTimer = Random.Range(minWaitTime, maxWaitTime); // Reset timer
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
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(initialPosition, patrolRadius);
    }
}