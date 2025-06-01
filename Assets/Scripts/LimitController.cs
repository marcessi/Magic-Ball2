using UnityEngine;
using TMPro;

public class LimitController : MonoBehaviour
{
    private Vector3 initialPaddlePosition;
    private PalletController paddle;
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private int maxLives = 3;
    private int currentLives;
    
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private UnityEngine.UI.Image[] heartImages;

    private void Start()
    {
        currentLives = maxLives;
        UpdateLivesDisplay();
        
        paddle = FindObjectOfType<PalletController>();
        if (paddle != null)
        {
            initialPaddlePosition = paddle.transform.position;
        }
        
        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the colliding object is a ball
        BallController collidedBall = collision.gameObject.GetComponent<BallController>();
        if (collidedBall != null)
        {
            // Verificar cuántas bolas hay en juego
            BallController[] allBalls = FindObjectsOfType<BallController>();
            
            // Si es la última bola, restamos vida y hacemos reset
            if (allBalls.Length <= 1)
            {
                // Notificar al GameManager que perdimos una vida
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoseLife();
                    
                    // Si aún quedan vidas, resetear la pelota y los powerups
                    if (GameManager.Instance.GetCurrentLives() > 0)
                    {
                        // Restablecer todos los powerups activos
                        ResetAllPowerups();
                        
                        // Reset the paddle to its initial position
                        if (paddle != null)
                        {
                            paddle.ResetPosition();
                        }
                        
                        // Reset the ball to the starting position
                        collidedBall.ResetBall();
                    }
                    // El GameOver lo maneja el GameManager cuando las vidas llegan a 0
                }
            }
            else
            {
                // Si hay más bolas en juego, simplemente destruimos esta
                Debug.Log("Una bola perdida - quedan " + (allBalls.Length-1) + " bolas");
                Destroy(collidedBall.gameObject);
            }
        }
    }
    
    private void ShowGameOver()
    {
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
            
            TMP_Text scoreTextUI = gameOverMenu.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            if (scoreTextUI != null)
            {
                int finalScore = CalculateScore();
                scoreTextUI.text = "Score: " + finalScore;
            }
            
            Time.timeScale = 0;
        }
        
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }
    
    private void SaveCurrentScore()
    {
        int finalScore = CalculateScore();
        PlayerPrefs.SetInt("CurrentScore", finalScore);
        PlayerPrefs.Save();
    }
    
    private int CalculateScore()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.GetCurrentScore();
        }
        
        return PlayerPrefs.GetInt("CurrentScore", 0);
    }

    private void UpdateLivesDisplay()
    {
        if (livesText != null)
        {
            livesText.text = "Lives: " + currentLives;
        }
        
        if (heartImages != null && heartImages.Length > 0)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] != null)
                    heartImages[i].enabled = i < currentLives;
            }
        }
    }

    private void ResetAllPowerups()
    {
        // 1. Eliminar todos los powerups que estén cayendo en la escena
        PowerupController[] activePowerups = FindObjectsOfType<PowerupController>();
        foreach (var powerup in activePowerups)
        {
            Destroy(powerup.gameObject);
        }
        
        // 2. Restablecer la paleta a su estado original
        if (paddle != null)
        {
            paddle.DeactivateShootMode();
            paddle.ResetToOriginalScale();
            paddle.DeactivateMagnetMode();
        }
        
        // 3. Desactivar PowerBall en todas las bolas
        BallController[] balls = FindObjectsOfType<BallController>();
        foreach (var ball in balls)
        {
            ball.SetPowerBallMode(false);
        }
        
        Debug.Log("Todos los powerups han sido restablecidos");
    }
}