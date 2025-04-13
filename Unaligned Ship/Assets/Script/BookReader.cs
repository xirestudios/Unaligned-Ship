using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class BookReader : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI interactionText; // The text that shows "Read enchanted writings"
    [SerializeField] private GameObject bookPanel; // The panel that appears when near book

    [Header("Book Content")]
    [TextArea(3, 10)]
    [SerializeField] private string bookText = "Enter your book content here...";

    [Header("Settings")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    
    private bool playerInRange = false;
    private bool isReading = false;

    private void Start()
    {
        // Initialize all UI as hidden
        if (interactionText != null) interactionText.gameObject.SetActive(false);
        if (bookPanel != null) bookPanel.SetActive(false);
        
        // Ensure collider is trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            ToggleReading();
        }
    }

    private void ToggleReading()
    {
        isReading = !isReading;

        if (isReading)
        {
            // Show book content in the interaction text
            interactionText.text = bookText;
        }
        else
        {
            // Revert to original prompt
            interactionText.text = "Read enchanted writings";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            // Show UI elements
            if (interactionText != null)
            {
                interactionText.text = "Read enchanted writings";
                interactionText.gameObject.SetActive(true);
            }
            
            if (bookPanel != null) bookPanel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            isReading = false;
            
            // Hide all UI
            if (interactionText != null) interactionText.gameObject.SetActive(false);
            if (bookPanel != null) bookPanel.SetActive(false);
        }
    }
}