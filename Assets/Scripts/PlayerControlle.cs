using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControlle : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float minSpeed = 4f;
    [SerializeField] private float maxSpeed = 800f;
    [SerializeField] private float maxAcceleration = 400f;
    [SerializeField] private float minAcceleration = -150f;

    [Header("Turning")]
    [SerializeField] private float maxTurnRate = 90f;
    [SerializeField] private float turnAcceleration = 260f;
    [SerializeField] private float turnDeceleration = 420f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Track Bounds")]
    [SerializeField] private bool keepInsideTrack = true;

    private float speed = 0f;
    private float currentTurnRate = 0f;
    private float targetTurn = 0f;
    private bool isGrounded = true;
    private Rigidbody rb;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        speed = minSpeed;
    }

    void Update()
    {
        float y = transform.eulerAngles.y;
        targetTurn = 0f;

        if (Keyboard.current.aKey.isPressed && y < 269f)
            targetTurn = maxTurnRate;
        if (Keyboard.current.dKey.isPressed && y > 91f)
            targetTurn = -maxTurnRate;
    }

    void FixedUpdate()
    {
        // Ground check
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down,
            groundCheckDistance + 0.1f, groundLayers);

        // Smooth turn rate
        float ramp = targetTurn != 0f ? turnAcceleration : turnDeceleration;
        currentTurnRate = Mathf.MoveTowards(currentTurnRate, targetTurn, ramp * Time.fixedDeltaTime);

        if (Mathf.Abs(currentTurnRate) > 0.01f)
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, currentTurnRate * Time.fixedDeltaTime, 0f));

        // Speed based on angle to slope
        float angle = Mathf.Abs(transform.eulerAngles.y - 180f);
        float accel = Remap(0f, 90f, maxAcceleration, minAcceleration, angle);
        speed += accel * Time.fixedDeltaTime;
        speed = Mathf.Clamp(speed, minSpeed, maxSpeed);

        animator.SetFloat("playerSpeed", speed);
        animator.SetBool("grounded", isGrounded);

        Vector3 velocity = transform.forward * speed * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

        if (keepInsideTrack)
            KeepInsideTrack();
    }

    private float Remap(float oldMin, float oldMax, float newMin, float newMax, float val)
    {
        return ((val - oldMin) / (oldMax - oldMin)) * (newMax - newMin) + newMin;
    }

    private void KeepInsideTrack()
    {
        Vector3 position = rb.position;
        float clampedX = Mathf.Clamp(position.x, RaceCourseLayout.MinTrackX, RaceCourseLayout.MaxTrackX);
        if (Mathf.Approximately(position.x, clampedX)) return;

        rb.MovePosition(new Vector3(clampedX, position.y, position.z));
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
    }
}
