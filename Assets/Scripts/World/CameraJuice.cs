using UnityEngine;

public class CameraJuice : MonoBehaviour
{
    [Header("Head Bobbing")]
    public float bobFrequency = 14f;  // How fast the head bobs (higher = faster footsteps)
    public float bobHorizontalAmount = 0.05f; // Side-to-side movement
    public float bobVerticalAmount = 0.08f;   // Up-and-down movement
    [Range(0, 1)] public float bobSmoothing = 0.1f; // How smoothly it returns to center

    [Header("Momentum Lean")]
    public float leanAmount = 2f;     // How much the camera tilts when strafing
    public float leanSpeed = 5f;      // How fast the camera tilts

    private float timer = 0f;
    private Vector3 initialLocalPosition;
    private CharacterController playerController;

    void Start()
    {
        initialLocalPosition = transform.localPosition;
        // Automatically finds the CharacterController on the parent object
        playerController = GetComponentInParent<CharacterController>();
    }

    void Update()
    {
        if (playerController == null) return;

        // OLD WAY (Reading physics velocity):
        // float speed = new Vector3(playerController.velocity.x, 0, playerController.velocity.z).magnitude;

        // NEW WAY (Calculate pseudo-speed based on WASD input keys):
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // This creates a value between 0 and 1 depending on how hard keys are pressed
        float inputMagnitude = new Vector2(moveX, moveZ).magnitude;

        // Multiply by your player's actual movement speed (e.g., 12f from your PlayerMovement script)
        float speed = inputMagnitude * 12f;

        HandleHeadBob(speed);
        HandleCameraLean(moveX);
    }

    void HandleHeadBob(float speed)
    {
        // Only bob the head if the player is moving and touching the ground
        if (speed > 0.01f && playerController.isGrounded)
        {
            // Advance the timer based on player speed
            timer += Time.deltaTime * speed * (bobFrequency / 10f);
            // Calculate new positions using Sine and Cosine waves
            float newX = initialLocalPosition.x + Mathf.Cos(timer / 2) * bobHorizontalAmount;
            float newY = initialLocalPosition.y + Mathf.Sin(timer) * bobVerticalAmount;

            transform.localPosition = new Vector3(newX, newY, transform.localPosition.z);
        }
        else
        {
            // Smoothly return to the original head height when stopped
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPosition, bobSmoothing);
        }
    }

    void HandleCameraLean(float moveX)
    {
        // Calculate target Z rotation based on strafe input (A/D keys)
        // Moving Right (D) tilts camera Left, Moving Left (A) tilts camera Right
        float targetZRotation = -moveX * leanAmount;
        // Smoothly interpolate the current local rotation to include the lean
        Vector3 currentRotation = transform.localRotation.eulerAngles;

        // Properly handle angle wrap-around using Mathf.LerpAngle
        float newZ = Mathf.LerpAngle(currentRotation.z, targetZRotation, Time.deltaTime * leanSpeed);

        transform.localRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, newZ);
    }
}