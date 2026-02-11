
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private Vector3 direction;
    private Camera Cam;
    [SerializeField] private float playerSpeed;
    private Animator PlrAnim;
    [SerializeField] private Transform foodInHand;  // The food item the player is holding
    [SerializeField] private Transform paperPlace;  // The position where the food will appear in hand
    public TextMeshProUGUI MoneyCounter;
    public static PlayerManager PlayerManagerInstance;
    private bool isCarryingFood = false;  // To track whether the player is carrying food

    private string currentHeldFoodTag;  // Track the tag of the food player is holding

    // List of valid food tags
    private List<string> validFoodTags = new List<string> { "food", "cupcake", "cake", "crepe" };

    void Start()
    {
        Cam = Camera.main;
        PlrAnim = GetComponent<Animator>();
        PlayerManagerInstance = this;
    }

    void Update()
    {
        // Move player based on mouse input
        if (Input.GetMouseButton(0))
        {
            Plane plane = new Plane(Vector3.up, transform.position);
            Ray ray = Cam.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out var distance))
                direction = ray.GetPoint(distance);

            transform.position = Vector3.MoveTowards(transform.position, new Vector3(direction.x, 0f, direction.z),
                playerSpeed * Time.deltaTime);

            var offset = direction - transform.position;

            if (offset.magnitude > 1f)
                transform.LookAt(direction);
        }

        // Set animations when mouse is pressed
        if (Input.GetMouseButtonDown(0))
        {
            if (isCarryingFood)
            {
                PlrAnim.SetBool("RunWithPapers", true);  // Enable RunWithPapers when carrying food
            }
            else
            {
                PlrAnim.SetBool("run", true);  // Regular running without food
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            PlrAnim.SetBool("run", false);

            if (isCarryingFood)
            {
                PlrAnim.SetBool("RunWithPapers", false);  // Disable RunWithPapers if stopped
                PlrAnim.SetBool("carry", true);  // Player is holding the food but not moving
            }
        }
    }

    // Trigger detection for picking up and dropping food
    private void OnTriggerEnter(Collider other)
    {
        // Pickup food from the shelf by detecting any of the valid food tags
        if (validFoodTags.Contains(other.tag) && !isCarryingFood)
        {
            Debug.Log("Food detected: " + other.name + " with tag: " + other.tag);  // Debug message

            var food = other.transform;  // Directly reference the food item
            foodInHand = food;
            food.parent = this.transform;  // Attach food to the player
            food.position = paperPlace.position;  // Set food position to paperPlace
            isCarryingFood = true;

            currentHeldFoodTag = other.tag;  // Store the tag of the food the player is holding

            PlrAnim.SetBool("carry", true);  // Show carrying animation
            PlrAnim.SetBool("RunWithPapers", true);  // Enable RunWithPapers while carrying
        }

        // Drop food at the correct table (coffeeplace)
        if (other.CompareTag("coffeeplace") && isCarryingFood)
        {
            Debug.Log("Coffeeplace detected: " + other.name);  // Debug message

            foodInHand.DOJump(new Vector3(other.transform.position.x, other.transform.position.y + 0.1f, other.transform.position.z), 2f, 1, 0.2f)
                .SetEase(Ease.Flash);

            foodInHand.parent = other.transform;  // Place the food on the coffee place
            foodInHand.localPosition = Vector3.zero;  // Align it properly on the coffee place
            foodInHand = null;  // Clear the reference to the food
            isCarryingFood = false;

            PlrAnim.SetBool("carry", false);  // No longer carrying
            PlrAnim.SetBool("RunWithPapers", false);  // Disable RunWithPapers when food is placed
            PlrAnim.SetBool("run", true);  // Continue running without food

            // Update money or score
            PlayerPrefs.SetInt("dollar", PlayerPrefs.GetInt("dollar") + 5);
            MoneyCounter.text = PlayerPrefs.GetInt("dollar").ToString("C0");

            // Check if the food was placed in the customer's foodplace, then remove the chatbox if correct food is delivered
            var customerChat = other.transform.parent.GetComponent<CustomerChat>();

            if (customerChat != null)
            {
                Debug.Log("Customer chatbox found.");
                if (currentHeldFoodTag == customerChat.GetRequestedFoodTag())
                {
                    Debug.Log("Correct food delivered. Removing chatbox...");
                    customerChat.RemoveChatbox();  // Remove the chatbox after placing the correct food
                }
                else
                {
                    Debug.Log("Incorrect food delivered. Chatbox stays.");
                }
            }
            else
            {
                Debug.Log("No CustomerChat found on parent of: " + other.transform.parent.name);
            }
        }
    }

    // Reset animations and states when exiting a trigger area
    private void OnTriggerExit(Collider other)
    {
        if (validFoodTags.Contains(other.tag) || other.CompareTag("coffeeplace"))
        {
            PlrAnim.SetBool("carry", isCarryingFood);  // Adjust animations based on food carrying
            PlrAnim.SetBool("RunWithPapers", isCarryingFood);  // Enable RunWithPapers only when carrying food
            PlrAnim.SetBool("run", !isCarryingFood);  // Regular run when not carrying food
        }
    }
}
