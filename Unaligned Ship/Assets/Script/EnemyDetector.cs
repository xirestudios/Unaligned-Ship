using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Required for URP

public class EnemyDetector : MonoBehaviour
{
    [Header("References")]
    public Volume globalVolume; // URP Volume component instead of PostProcessVolume
    public Transform player;
    public string enemyTag = "Enemy";

    [Header("Vignette Settings")]
    public float maxIntensity = 0.3f;
    public float minIntensity = 0.05f;
    public float maxDistance = 10f;
    public float minDistance = 2f;
    public float lerpSpeed = 2f;

    [Header("Detection Settings")]
    public float detectionRadius = 15f;
    public LayerMask enemyLayer;

    private Vignette vignette;
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;

    void Start()
    {
        // Get the Vignette effect from the URP Volume
        if (globalVolume.profile.TryGet(out vignette))
        {
            // Initialize vignette settings
            vignette.intensity.Override(0f);
            vignette.color.Override(Color.black);
            vignette.center.Override(new Vector2(0.5f, 0.5f));
            vignette.smoothness.Override(1f);
            vignette.rounded.Override(true);
        }
        else
        {
            Debug.LogError("Vignette effect not found in the Volume profile!");
        }
    }

    void Update()
    {
        if (vignette == null) return;

        Collider[] enemiesInRange = Physics.OverlapSphere(player.position, detectionRadius, enemyLayer);
        
        targetIntensity = enemiesInRange.Length == 0 ? 0f : CalculateIntensity(FindClosestEnemyDistance(enemiesInRange));

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, lerpSpeed * Time.deltaTime);
        vignette.intensity.Override(currentIntensity);
    }

    float FindClosestEnemyDistance(Collider[] enemies)
    {
        float closestDistance = float.MaxValue;
        foreach (Collider enemy in enemies)
        {
            float distance = Vector3.Distance(player.position, enemy.transform.position);
            closestDistance = Mathf.Min(distance, closestDistance);
        }
        return closestDistance;
    }

    float CalculateIntensity(float distance)
    {
        if (distance <= minDistance) return maxIntensity;
        if (distance >= maxDistance) return minIntensity;
        return Mathf.Lerp(maxIntensity, minIntensity, (distance - minDistance) / (maxDistance - minDistance));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, detectionRadius);
    }
}