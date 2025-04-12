using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class AdvancedObjectDragger : MonoBehaviour
{
    [Header("Drag Settings")]
    public float maxGrabDistance = 10f;
    public float holdDistance = 2f;
    public float lerpSpeed = 10f;
    public float rotationSpeed = 5f;
    public LayerMask grabLayer;
    public float resetThreshold = 10f; // Distance below initial position that triggers reset
    public float resetDelay = 3f; // Time before resetting after falling

    private Rigidbody grabbedObject;
    private bool isHolding;
    private Vector3 holdOffset;
    private float currentHoldDistance;
    private Camera mainCamera;
    private Mouse mouse;

    // Variables for position reset functionality
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool needsReset;
    private float fallTimer;
    private bool wasBeingHeld;

    void Awake()
    {
        mainCamera = Camera.main;
        mouse = Mouse.current;
        
        // Store initial position and rotation
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Update()
    {
        HandleInput();
        MoveHeldObject();
        CheckForReset();
    }

    void HandleInput()
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryGrabObject();
        }

        if (mouse.leftButton.wasReleasedThisFrame && isHolding)
        {
            ReleaseObject();
        }

        if (isHolding)
        {
            currentHoldDistance += mouse.scroll.ReadValue().y * 0.5f;
            currentHoldDistance = Mathf.Clamp(currentHoldDistance, 0.5f, maxGrabDistance);
        }
    }

    void TryGrabObject()
    {
        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, grabLayer))
        {
            if (hit.collider.attachedRigidbody != null)
            {
                grabbedObject = hit.collider.attachedRigidbody;
                grabbedObject.isKinematic = false;
                
                holdOffset = grabbedObject.transform.InverseTransformPoint(hit.point);
                currentHoldDistance = Vector3.Distance(hit.point, mainCamera.transform.position);
                
                isHolding = true;
                wasBeingHeld = true;
                
                // Reset the fall timer when grabbing
                needsReset = false;
                fallTimer = 0f;
            }
        }
    }

    void MoveHeldObject()
    {
        if (!isHolding || grabbedObject == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        
        Vector3 targetPosition = ray.GetPoint(currentHoldDistance);
        Vector3 worldOffset = grabbedObject.transform.TransformDirection(holdOffset);
        
        Vector3 desiredPosition = targetPosition - worldOffset;
        grabbedObject.velocity = (desiredPosition - grabbedObject.position) * lerpSpeed;
        
        Quaternion targetRotation = Quaternion.LookRotation(grabbedObject.position - mainCamera.transform.position);
        grabbedObject.rotation = Quaternion.Slerp(grabbedObject.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void ReleaseObject()
    {
        if (grabbedObject != null)
        {
            grabbedObject.velocity *= 1.2f;
            grabbedObject = null;
        }
        isHolding = false;
    }

    void CheckForReset()
    {
        // Only check if the object was being held and then released
        if (wasBeingHeld && !isHolding)
        {
            // Check if the object has fallen below the threshold
            if (transform.position.y < initialPosition.y - resetThreshold)
            {
                needsReset = true;
                fallTimer += Time.deltaTime;
                
                // Reset after the delay time has passed
                if (fallTimer >= resetDelay)
                {
                    ResetObject();
                }
            }
            else
            {
                needsReset = false;
                fallTimer = 0f;
            }
        }
    }

    void ResetObject()
    {
        // Reset position and rotation
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        // Reset physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Reset tracking variables
        needsReset = false;
        fallTimer = 0f;
        wasBeingHeld = false;
    }

    // Visualize the reset threshold in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 resetPosition = Application.isPlaying ? initialPosition : transform.position;
        Gizmos.DrawLine(resetPosition, resetPosition + Vector3.down * resetThreshold);
        Gizmos.DrawWireSphere(resetPosition + Vector3.down * resetThreshold, 0.2f);
    }
}