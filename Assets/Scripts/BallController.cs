using UnityEngine;
using UnityEngine.InputSystem;

public class BallController : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private float initialSpeed = 10f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private Vector3 initialDirection = new Vector3(1f, 0f, 1f).normalized;
    [SerializeField] private float hitForce = 1.1f; // Optional: increase ball speed slightly on each hit

    private Rigidbody rb;
    private bool gameStarted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Initialize ball at rest
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
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
        // Check if we hit a block
        BlockController block = collision.gameObject.GetComponent<BlockController>();
        if (block != null)
        {
            // Notify the block it was hit
            block.OnHit();
            
            // Optional: Increase ball speed slightly on each hit
            initialSpeed = Mathf.Min(initialSpeed * hitForce, maxSpeed);
        }
        
        // Ensure the ball doesn't get stuck horizontally or vertically
        EnsureNonZeroVelocityComponents();
    }
    
    private void EnsureNonZeroVelocityComponents()
    {
        // Get current velocity
        Vector3 velocity = rb.linearVelocity;
        
        // Make sure x and z components aren't too close to zero
        // This prevents the ball from moving in a perfectly horizontal or vertical line
        float minComponentValue = initialSpeed * 0.1f;
        
        if (Mathf.Abs(velocity.x) < minComponentValue)
        {
            velocity.x = minComponentValue * Mathf.Sign(velocity.x == 0 ? Random.Range(-1f, 1f) : velocity.x);
        }
        
        if (Mathf.Abs(velocity.z) < minComponentValue)
        {
            velocity.z = minComponentValue * Mathf.Sign(velocity.z == 0 ? Random.Range(-1f, 1f) : velocity.z);
        }
        
        rb.linearVelocity = velocity.normalized * initialSpeed;
    }
    
    // Reset the ball to its initial position and state
    public void ResetBall(Vector3 position)
    {
        transform.position = position;
        rb.linearVelocity = Vector3.zero;
        gameStarted = false;
        initialSpeed = 10f; // Reset to initial speed
    }
}