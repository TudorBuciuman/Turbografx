using UnityEngine;

public class CameraJuice : MonoBehaviour
{
    [Header("Head Bobbing")]
    public float bobFrequency = 14f;  
    public float bobHorizontalAmount = 0.05f; 
    public float bobVerticalAmount = 0.08f;  
    [Range(0, 1)] public float bobSmoothing = 0.1f; 

    [Header("Momentum Lean")]
    public float leanAmount = 2f;     
    public float leanSpeed = 5f;      

    private float timer = 0f;
    private Vector3 initialLocalPosition;
    private CharacterController playerController;

    void Start()
    {
        initialLocalPosition = transform.localPosition;
        playerController = GetComponentInParent<CharacterController>();
    }

    void Update()
    {
        if (playerController == null) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        float inputMagnitude = new Vector2(moveX, moveZ).magnitude;

        float speed = inputMagnitude * 12f;

        HandleHeadBob(speed);
        HandleCameraLean(moveX);
    }

    void HandleHeadBob(float speed)
    {
        if (speed > 0.01f && playerController.isGrounded)
        {
            timer += Time.deltaTime * speed * (bobFrequency / 10f);
            float newX = initialLocalPosition.x + Mathf.Cos(timer / 2) * bobHorizontalAmount;
            float newY = initialLocalPosition.y + Mathf.Sin(timer) * bobVerticalAmount;

            transform.localPosition = new Vector3(newX, newY, transform.localPosition.z);
        }
        else
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPosition, bobSmoothing);
        }
    }

    void HandleCameraLean(float moveX)
    {
        float targetZRotation = -moveX * leanAmount;
        Vector3 currentRotation = transform.localRotation.eulerAngles;
        float newZ = Mathf.LerpAngle(currentRotation.z, targetZRotation, Time.deltaTime * leanSpeed);

        transform.localRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, newZ);
    }
}