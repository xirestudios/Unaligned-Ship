using UnityEngine;
using System.Collections;

public class DoorLockKey : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private Transform doorTransform; // The door's transform
    [SerializeField] private float openDuration = 1.5f; // Time to open the door
    [SerializeField] private Vector3 openRotation = new Vector3(0, 90, 0); // Open angle (Y-axis by default)

    [Header("Lock Settings")]
    [SerializeField] private string requiredKeyTag = "Key"; // Only keys with this tag can unlock
    [SerializeField] private Collider keyHoleCollider; // The keyhole's trigger collider
    [SerializeField] private Transform keySnapPosition; // Where the key should stick (assign in inspector)

    [Header("Key Settings")]
    [SerializeField] private bool disableKeyPhysics = true; // Freeze key rigidbody after insertion

    private bool doorIsOpen = false;
    private Quaternion initialDoorRotation;
    private Quaternion targetDoorRotation;
    private GameObject insertedKey; // Reference to the inserted key

    private void Start()
    {
        if (doorTransform != null)
        {
            initialDoorRotation = doorTransform.rotation;
            targetDoorRotation = Quaternion.Euler(openRotation) * initialDoorRotation;
        }

        if (keyHoleCollider == null)
        {
            Debug.LogError("Keyhole collider not assigned!");
        }
        else if (!keyHoleCollider.isTrigger)
        {
            Debug.LogWarning("Keyhole collider should be a trigger for best results.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object has the correct key tag
        if (other.CompareTag(requiredKeyTag) && !doorIsOpen)
        {
            insertedKey = other.gameObject;
            StickKeyInLock();
            OpenDoor();
        }
    }

    private void StickKeyInLock()
    {
        if (insertedKey == null || keySnapPosition == null) return;

        // Snap key to the keyhole position
        insertedKey.transform.position = keySnapPosition.position;
        insertedKey.transform.rotation = keySnapPosition.rotation;

        // Disable physics (optional)
        if (disableKeyPhysics)
        {
            Rigidbody keyRb = insertedKey.GetComponent<Rigidbody>();
            if (keyRb != null)
            {
                keyRb.isKinematic = true;
                keyRb.velocity = Vector3.zero;
            }
        }

        // Disable collider to prevent re-triggering
        Collider keyCollider = insertedKey.GetComponent<Collider>();
        if (keyCollider != null)
        {
            keyCollider.enabled = false;
        }
    }

    private void OpenDoor()
    {
        if (doorTransform == null)
        {
            Debug.LogError("Door transform not assigned!");
            return;
        }

        StartCoroutine(RotateDoor());
        doorIsOpen = true;
    }

    private IEnumerator RotateDoor()
    {
        float elapsedTime = 0f;
        Quaternion startRotation = doorTransform.rotation;

        while (elapsedTime < openDuration)
        {
            doorTransform.rotation = Quaternion.Slerp(startRotation, targetDoorRotation, elapsedTime / openDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        doorTransform.rotation = targetDoorRotation; // Ensure perfect final rotation
    }
}