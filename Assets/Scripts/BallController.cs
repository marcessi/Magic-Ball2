using UnityEngine;
using UnityEngine.InputSystem;

public class BallController : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private float initialSpeed = 5f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private Vector3 initialDirection = new Vector3(1f, 0f, 1f).normalized;

    private Rigidbody rb;
    private bool gameStarted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Prepare the ball's rigidbody
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY; // Lock Y movement for a 3D game on a flat plane
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        // Press Space to launch the ball using the new Input System
        if (!gameStarted && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            LaunchBall();
        }

        // Ensure ball stays at the correct velocity
        if (gameStarted)
        {
            MaintainSpeed();
        }
    }

    private void LaunchBall()
    {
        gameStarted = true;
        rb.linearVelocity = initialDirection * initialSpeed;
    }

    private void MaintainSpeed()
    {
        // Ensure ball maintains a consistent speed
        Vector3 currentVelocity = rb.linearVelocity;
        float currentSpeed = currentVelocity.magnitude;
        
        if (currentSpeed != initialSpeed)
        {
            // Normalize the velocity and apply the desired speed
            rb.linearVelocity = currentVelocity.normalized * initialSpeed;
        }

        // Ensure ball never exceeds max speed
        if (currentSpeed > maxSpeed)
        {
            rb.linearVelocity = currentVelocity.normalized * maxSpeed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ball has already collided and physics system has calculated the new velocity
        // Additional effects or sound could be added here
    }
}