using UnityEngine;

public class LimitController : MonoBehaviour
{
    [SerializeField] private Transform ballResetPosition; // Position to reset the ball to
    [SerializeField] private Vector3 defaultResetPosition = new Vector3(0, 1, 0); // Default position if no reference

    // Store initial positions
    private Vector3 initialBallPosition;
    private Vector3 initialPaddlePosition;
    private BallController ball;
    private PalletController paddle;

    private void Start()
    {
        // Find the ball and store its initial position
        ball = FindObjectOfType<BallController>();
        if (ball != null)
        {
            initialBallPosition = ball.transform.position;
        }

        // Find the paddle and store its initial position
        paddle = FindObjectOfType<PalletController>();
        if (paddle != null)
        {
            initialPaddlePosition = paddle.transform.position;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the colliding object is the ball
        BallController collidedBall = collision.gameObject.GetComponent<BallController>();
        if (collidedBall != null)
        {
            // Use stored initial position, reference position, or default position
            Vector3 resetPos = initialBallPosition;
            
            if (ballResetPosition != null && ballResetPosition != collidedBall.transform)
            {
                resetPos = ballResetPosition.position;
            }
            else if (initialBallPosition == Vector3.zero)
            {
                resetPos = defaultResetPosition;
            }
                
            // Reset the ball to the starting position
            collidedBall.ResetBall(resetPos);
            
            // Reset the paddle to its initial position
            if (paddle != null)
            {
                paddle.transform.position = initialPaddlePosition;
            }
        }
    }
}