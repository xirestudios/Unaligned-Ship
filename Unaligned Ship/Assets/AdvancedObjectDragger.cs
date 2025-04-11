using UnityEngine;
using UnityEngine.InputSystem; // Required for new Input System

[RequireComponent(typeof(Rigidbody))]
public class AdvancedObjectDragger : MonoBehaviour
{
    [Header("Drag Settings")]
    public float maxGrabDistance = 10f;
    public float holdDistance = 2f;
    public float lerpSpeed = 10f;
    public float rotationSpeed = 5f;
    public LayerMask grabLayer;

    private Rigidbody grabbedObject;
    private bool isHolding;
    private Vector3 holdOffset;
    private float currentHoldDistance;
    private Camera mainCamera;
    private Mouse mouse; // New Input System reference

    void Awake()
    {
        mainCamera = Camera.main;
        mouse = Mouse.current; // Initialize mouse reference
    }

    void Update()
    {
        HandleInput();
        MoveHeldObject();
    }

    void HandleInput()
    {
        // New Input System: Check for left mouse button
        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryGrabObject();
        }

        if (mouse.leftButton.wasReleasedThisFrame && isHolding)
        {
            ReleaseObject();
        }

        // Mouse wheel adjustment
        if (isHolding)
        {
            currentHoldDistance += mouse.scroll.ReadValue().y * 0.5f;
            currentHoldDistance = Mathf.Clamp(currentHoldDistance, 0.5f, maxGrabDistance);
        }
    }

    void TryGrabObject()
    {
        // New Input System: Get mouse position
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
            }
        }
    }

    void MoveHeldObject()
    {
        if (!isHolding || grabbedObject == null) return;

        // New Input System: Get mouse position
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
}