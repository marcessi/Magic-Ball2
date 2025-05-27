using UnityEngine;
using TMPro; // Reemplazar UnityEngine.UI por TMPro

public class LimitController : MonoBehaviour
{
    private Vector3 initialPaddlePosition;
    private BallController ball;
    private PalletController paddle;
    [SerializeField] private GameObject gameOverMenu; // Referencia al menú de game over
    [SerializeField] private int maxLives = 3; // Vidas máximas
    private int currentLives; // Vidas actuales
    
    // Cambiar de Text a TextMeshProUGUI
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private UnityEngine.UI.Image[] heartImages; // Las imágenes siguen siendo Image normal

    // Actualizar este método
    private void Start()
    {
        // Inicializar vidas
        currentLives = maxLives;
        UpdateLivesDisplay();
        
        // Find the paddle and store its initial position
        paddle = FindObjectOfType<PalletController>();
        if (paddle != null)
        {
            initialPaddlePosition = paddle.transform.position;
        }
        
        // Ocultar menú de game over si está asignado
        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the colliding object is the ball
        BallController collidedBall = collision.gameObject.GetComponent<BallController>();
        if (collidedBall != null)
        {
            // Si es una bola extra, simplemente destruirla sin penalización
            if (!collidedBall.isMainBall)
            {
                Debug.Log("Bola extra perdida - sin penalización");
                Destroy(collidedBall.gameObject);
                return;
            }
            
            // Restar una vida
            currentLives--;
            UpdateLivesDisplay();
            
            // Si aún quedan vidas, resetear la pelota y los powerups
            if (currentLives > 0)
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
            else
            {
                // Game over
                ShowGameOver();
                
                // Guardar la puntuación actual (implementar esto en otro script)
                SaveCurrentScore();
            }
        }
    }
    
    private void ShowGameOver()
    {
        // Mostrar el menú de game over si está asignado
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
            
            // Cambiar para usar TMP_Text
            TMP_Text scoreTextUI = gameOverMenu.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            if (scoreTextUI != null)
            {
                int finalScore = CalculateScore();
                scoreTextUI.text = "Score: " + finalScore;
            }
            
            // Pausar el juego
            Time.timeScale = 0;
        }
        
        // También podemos usar el GameManager si existe
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }
    
    private void SaveCurrentScore()
    {
        // Aquí deberías calcular la puntuación final basada en bloques destruidos, tiempo, etc.
        int finalScore = CalculateScore();
        
        // Guardar temporalmente para que el menú pueda acceder a ella
        PlayerPrefs.SetInt("CurrentScore", finalScore);
        PlayerPrefs.Save();
    }
    
    private int CalculateScore()
    {
        // Obtener la puntuación directamente del GameManager
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.GetCurrentScore();
        }
        
        // Fallback en caso de que no exista GameManager
        return PlayerPrefs.GetInt("CurrentScore", 0);
    }

    // Cambiar UpdateLivesDisplay para usar TMP_Text
    private void UpdateLivesDisplay()
    {
        // Actualizar texto si existe
        if (livesText != null)
        {
            livesText.text = "Lives: " + currentLives;
        }
        
        // Las imágenes siguen igual
        if (heartImages != null && heartImages.Length > 0)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] != null)
                    heartImages[i].enabled = i < currentLives;
            }
        }
    }

    // Añadir este método a LimitController.cs
    private void ResetAllPowerups()
    {
        // 1. Eliminar todos los powerups que estén cayendo en la escena
        PowerupController[] activePowerups = FindObjectsOfType<PowerupController>();
        foreach (var powerup in activePowerups)
        {
            Destroy(powerup.gameObject);
        }
        PalletController paddle = FindObjectOfType<PalletController>();
        if (paddle != null)
        {
            paddle.DeactivateShootMode();
        }
        
        // 2. Restablecer la paleta a su tamaño original
        if (paddle != null)
        {
            paddle.ResetToOriginalScale();
            paddle.DeactivateMagnetMode();
        }
        
        // 3. Desactivar PowerBall en todas las bolas
        BallController[] balls = FindObjectsOfType<BallController>();
        foreach (var ball in balls)
        {
            if (ball.isMainBall) // Solo resetear la bola principal
            {
                ball.SetPowerBallMode(false);
            }
        }
        
        Debug.Log("Todos los powerups han sido restablecidos");
    }
}