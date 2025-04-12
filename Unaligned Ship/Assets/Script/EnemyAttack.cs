using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 1;
    
    private PlayerHealth playerHealth;

    void Start()
    {
        // Get reference to player's health component
        playerHealth = GameObject.FindWithTag("Player")?.GetComponent<PlayerHealth>();
        
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth component not found on Player!", this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only proceed if this is the player and we have a health reference
        if (other.CompareTag("Player") && playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Debug.Log($"Hit player! Damage dealt: {damage}");
        }
    }

    void OnDrawGizmos()
    {
        // Visualize the attack collider in red
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position + col.center, col.size);
        }
    }
}