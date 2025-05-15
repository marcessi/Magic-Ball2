using UnityEngine;

public class LimitController : MonoBehaviour
{
    private Vector3 initialPaddlePosition;
    private BallController ball;
    private PalletController paddle;

    private void Start()
    {

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

            // Reset the paddle to its initial position
            if (paddle != null)
            {
                paddle.ResetPosition();
            }    
            // Reset the ball to the starting position
            collidedBall.ResetBall();

        }
    }
}