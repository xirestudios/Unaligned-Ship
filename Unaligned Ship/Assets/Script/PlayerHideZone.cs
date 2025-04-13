using UnityEngine;

public class PlayerHideZone : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Drag the player GameObject here")]
    public GameObject playerObject; // Assign in inspector
    public static event System.Action<bool> OnPlayerHideStatusChanged;
    public bool isPlayerHiding = false;

    [Header("Layer Settings")]
    [SerializeField] private string playerLayer = "Player";
    [SerializeField] private string hideLayer = "HidePlayer";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.gameObject.layer = LayerMask.NameToLayer(hideLayer);
            OnPlayerHideStatusChanged?.Invoke(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.gameObject.layer = LayerMask.NameToLayer(playerLayer);
            OnPlayerHideStatusChanged?.Invoke(false);
        }
    }

    public bool IsPlayerHiding()
    {
        return isPlayerHiding;
    }

    // Safety check
    private void OnValidate()
    {
        if (playerObject != null && !playerObject.CompareTag("Player"))
        {
            Debug.LogWarning("Assigned object is not tagged as Player!", this);
        }
    }
}