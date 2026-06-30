using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 12f;
    public float gravity = -19.62f; // Slightly stronger than real gravity for a snappy feel
    public float jumpHeight = 3f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

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
        // 1. Ground Check
        // Creates a small sphere at the groundCheck transform. If it hits anything in groundMask, true.
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            // Reset velocity, but keep a tiny bit of downward force to keep it grounded
            velocity.y = -2f;
        }

        if (canMove)
        {
            // 2. WASD / Arrow Keys Input
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            // Calculate move direction relative to the direction the player is facing
            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * moveSpeed * Time.deltaTime);

            // 3. Jumping
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                // Physics formula for jump velocity: v = sqrt(h * -2 * g)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // 4. Apply Gravity
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;

            ActualHorizontalVelocity = delta / Time.deltaTime;

            lastPosition = transform.position;
        }
    }
}