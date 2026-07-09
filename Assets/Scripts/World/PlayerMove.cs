using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed = 25f;
    private float jumpHeight = 100f;

    [Header("Running")]
    public bool Running_allowed = true;
    private float runSpeedMultiplier = 2.2f;
    private bool isRunning = false; // Tracks the toggle state

    private CharacterController controller;
    private Vector3 velocity;

    public bool IsGrounded => controller.isGrounded;

    public Vector3 ActualHorizontalVelocity { get; private set; }

    private Vector3 lastPosition;

    public static bool canMove = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // 1. Reset downward velocity if already grounded
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -5f;
        }

        Vector3 move = Vector3.zero;

        if (canMove)
        {
            // 2. Gather WASD / Arrow Keys Input
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            // Check if player is holding/pressing any movement keys
            bool isMovingInput = Mathf.Abs(x) > 0.01f || Mathf.Abs(z) > 0.01f;

            if (Running_allowed && isMovingInput)
            {
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    isRunning = true; 
                }
            }
            else
            {
                // Cancel sprint automatically if the player completely stops moving
                isRunning = false;
            }

            float currentSpeed = moveSpeed * (isRunning ? runSpeedMultiplier : 1f);

            // Combine directional move vector
            move = (transform.right * x + transform.forward * z) * currentSpeed;

            // 3. Jumping Calculations
            if (Running_allowed && Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);

                // Optional Cyberpunk behavior: Un-comment the line below 
                // if you want jumping to cancel the sprint toggle.
                // isRunning = false; 
            }
        }
        else
        {
            // Force sprint off if player movement is disabled globally
            isRunning = false;
        }

        // 4. Apply Gravity over time
        velocity.y += Physics.gravity.y * Time.deltaTime * 10f;

        // 5. COMBINED MOVE CALL
        Vector3 finalMovement = (move + velocity) * Time.deltaTime;
        controller.Move(finalMovement);

        // 6. Track actual velocity values
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        ActualHorizontalVelocity = delta / Time.deltaTime;
        lastPosition = transform.position;
    }
}
