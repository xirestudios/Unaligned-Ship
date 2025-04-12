using UnityEngine;

public class PlayerHideZone : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Drag the player GameObject here")]
    public GameObject playerObject; // Assign in inspector

    [Header("Layer Settings")]
    [SerializeField] private string playerLayer = "Player";
    [SerializeField] private string hideLayer = "HidePlayer";

    private void OnTriggerEnter(Collider other)
    {
        // Only affect the specific player object we assigned
        if (other.gameObject == playerObject)
        {
            playerObject.layer = LayerMask.NameToLayer(hideLayer);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Only affect the specific player object we assigned
        if (other.gameObject == playerObject)
        {
            playerObject.layer = LayerMask.NameToLayer(playerLayer);
        }
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