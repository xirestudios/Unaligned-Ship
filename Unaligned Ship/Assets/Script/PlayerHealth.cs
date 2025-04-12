using UnityEngine;
using System.Collections;


public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public float regenerationDelay = 15f;
    public float regenerationRate = 0.1f;

    [Header("Damage Feedback")]
    public GameObject damageIndicator;
    public float indicatorDuration = 1f;
    public float cameraShakeIntensity = 0.5f;
    public float cameraShakeDuration = 0.3f;

    public int currentHealth { get; private set; }
    private float timeSinceLastDamage;
    private bool isRegenerating;
    private Coroutine indicatorCoroutine;
    public GameObject mainCamera;
    private Vector3 originalCameraPos;

    public event System.Action<int> OnHealthChanged;
    public event System.Action OnDeath;

    void Start()
    {
        currentHealth = maxHealth;
        timeSinceLastDamage = 0f;
        isRegenerating = false;
        originalCameraPos = mainCamera.transform.localPosition;
        
        if (damageIndicator != null)
            damageIndicator.SetActive(false);
    }

    void Update()
    {
        if (currentHealth < maxHealth && !isRegenerating)
        {
            timeSinceLastDamage += Time.deltaTime;
            if (timeSinceLastDamage >= regenerationDelay)
            {
                StartCoroutine(RegenerateHealth());
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        timeSinceLastDamage = 0f;
        isRegenerating = false;
        StopAllCoroutines();
        // GetComponent<FirstPersonController>().PushPlayer(-transform.forward);

        // Visual feedback
        if (damageIndicator != null)
        {
            if (indicatorCoroutine != null)
                StopCoroutine(indicatorCoroutine);
            indicatorCoroutine = StartCoroutine(ShowDamageIndicator());
        }

        // Camera shake
        StartCoroutine(ShakeCamera());

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator ShowDamageIndicator()
    {
        damageIndicator.SetActive(true);
        yield return new WaitForSeconds(indicatorDuration);
        damageIndicator.SetActive(false);
    }

    IEnumerator ShakeCamera()
    {
        float elapsed = 0f;
        
        while (elapsed < cameraShakeDuration)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * cameraShakeIntensity;
            mainCamera.transform.localPosition = originalCameraPos + shakeOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.localPosition = originalCameraPos;
    }

    IEnumerator RegenerateHealth()
    {
        isRegenerating = true;
        while (currentHealth < maxHealth)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + 1);
            OnHealthChanged?.Invoke(currentHealth);
            yield return new WaitForSeconds(1f / regenerationRate);
        }
        isRegenerating = false;
    }

    void Die()
    {
        if (damageIndicator != null)
            damageIndicator.SetActive(false);
            
        OnDeath?.Invoke();
        // Your death logic here
    }
}