using UnityEngine;
using TMPro;
using System.Collections;

public class DoorLockGatcha : MonoBehaviour
{
    [Header("Gatcha Settings")]
    [SerializeField] private int numberRequirement = 950; // The required number to open the door
    [SerializeField] private float rollDuration = 2f; // How long the number rolls before stopping
    [SerializeField] private float rollSpeed = 0.05f; // How fast the numbers change during roll

    [Header("References")]
    [SerializeField] private TextMeshPro displayText; // Reference to the TMP text display
    [SerializeField] private Collider interactionCollider; // Collider to interact with

    [Header("Door Settings")]
    [SerializeField] private Transform doorTransform; // Reference to the door's transform
    [SerializeField] private float openDuration = 1.5f; // How long the door opening animation takes
    [SerializeField] private Vector3 openRotation = new Vector3(0, 90, 0); // Rotation when door is 

    private bool doorIsOpen = false;
    private Quaternion initialDoorRotation;
    private Quaternion targetDoorRotation;

    private bool isRolling = false;
    private bool isUnlocked = false;

    private void Start()
    {
        if (doorTransform != null)
        {
            initialDoorRotation = doorTransform.rotation;
            targetDoorRotation = Quaternion.Euler(openRotation) * initialDoorRotation;
        }
    }

    private void Update()
    {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0) && !isRolling && !isUnlocked)
        {
            // Raycast to check if we hit the interaction collider
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider == interactionCollider)
            {
                StartCoroutine(RollNumbers());
            }
        }
    }

    private IEnumerator RollNumbers()
    {
        isRolling = true;
        float elapsedTime = 0f;

        // Roll animation
        while (elapsedTime < rollDuration)
        {
            // Generate random number and format with spaces
            int randomNum = Random.Range(0, 1000);
            displayText.text = FormatNumberWithSpaces(randomNum);

            elapsedTime += rollSpeed;
            yield return new WaitForSeconds(rollSpeed);
        }

        // Final number
        int finalNumber = Random.Range(0, 1000);
        displayText.text = FormatNumberWithSpaces(finalNumber);

        // Check if number meets requirement
        if (finalNumber >= numberRequirement)
        {
            OpenDoor();
            isUnlocked = true;
        }

        isRolling = false;
    }

    private string FormatNumberWithSpaces(int number)
    {
        // Ensure number is 3 digits
        string numString = number.ToString("000");
        // Insert spaces between digits
        return $"{numString[0]} {numString[1]} {numString[2]}";
    }

    private void OpenDoor()
    {
        if (doorTransform == null)
        {
            Debug.LogError("Door transform reference not set!");
            return;
        }

        if (!doorIsOpen)
        {
            Debug.Log("Door unlocked! Number requirement met.");
            StartCoroutine(RotateDoor());
            doorIsOpen = true;
        }
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

        // Ensure final rotation is exact
        doorTransform.rotation = targetDoorRotation;
    }
}