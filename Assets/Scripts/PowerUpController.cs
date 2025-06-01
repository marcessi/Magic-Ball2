using UnityEngine;
using System.Collections;

public class PowerupController : MonoBehaviour
{
    public enum PowerupType { 
        Expand, 
        Shrink, 
        PowerBall, 
        NormalBall, 
        ExtraBalls, 
        Magnet, 
        NextLevel,
        SpeedUp,    
        SlowDown,
        Shoot
    }
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 90f;

    [Header("PowerUp Effects")]
    [SerializeField] private PowerupType powerupType = PowerupType.Expand;
    [SerializeField] private float expandFactor = 1.05f; // Reducido de 1.1 a 1.05
    [SerializeField] private float shrinkFactor = 0.95f; // Cambiado de 0.9 a 0.95
    [SerializeField] private float powerupDuration = 10f;  // Duración de los efectos temporales
    [SerializeField] private GameObject ballPrefab;  // Para las bolas extras
    
    [Header("Visual")]
    [SerializeField] private float powerupScale = 10.0f;

    [Header("Audio")]
    private AudioClip powerUpSound;

    private void Start()
    {
        transform.localScale *= powerupScale;
        
        // Registrar este power up en el contador global
        BlockController.IncrementActivePowerUps();
        
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
        
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
        }

        // Load the power up sound
        powerUpSound = Resources.Load<AudioClip>("Audio/powerUp");
        if (powerUpSound == null)
        {
            Debug.LogWarning("Could not load powerUp sound from Resources/Audio");
        }
        
        // Cargar automáticamente el prefab de la bola si es necesario
        if (ballPrefab == null && powerupType == PowerupType.ExtraBalls)
        {
            ballPrefab = Resources.Load<GameObject>("Prefabs/Ball");
        }

        
        
        Debug.Log($"PowerUp inicializado: {gameObject.name} - Tipo: {powerupType} (Total activos: {BlockController.GetActivePowerUps()})");
    }

    private void Update()
    {
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
    
    private void OnDestroy()
    {
        // Decrementar el contador al ser destruido
        BlockController.DecrementActivePowerUps();
        Debug.Log($"PowerUp destruido: {powerupType} (Total activos restantes: {BlockController.GetActivePowerUps()})");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Paddle"))
        {
            PalletController paddle = collision.gameObject.GetComponent<PalletController>();
            if (paddle != null)
            {
                ApplyPowerupEffect(paddle);
            }
            
            // NO necesitamos llamar a DecrementActivePowerUps aquí porque OnDestroy() lo hará
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Limit"))
        {
            Destroy(gameObject);
        }
        
        if (other.CompareTag("Paddle"))
        {
            PalletController paddle = other.GetComponent<PalletController>();
            if (paddle != null)
            {
                ApplyPowerupEffect(paddle);
            }
            Destroy(gameObject);
        }
    }
    
    private void ApplyPowerupEffect(PalletController paddle)
    {
        // Usar AudioManager para reproducir el sonido del power-up
        if (powerUpSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(powerUpSound, transform.position, 0.3f);
        }
        else if (powerUpSound != null)
        {
            // Fallback si no hay AudioManager
            AudioSource.PlayClipAtPoint(powerUpSound, transform.position, 0.3f);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPoints(20); // 20 puntos por powerup
        }
        
        switch (powerupType)
        {
            case PowerupType.Expand:
                paddle.ExpandPaddle(expandFactor);
                Debug.Log("PowerUp aplicado: Expandir paleta");
                break;
            
            case PowerupType.Shrink:
                paddle.ShrinkPaddle(shrinkFactor);
                Debug.Log("PowerUp aplicado: Reducir paleta");
                break;
                
            case PowerupType.PowerBall:
                ActivatePowerBall();
                Debug.Log("PowerUp aplicado: Power Ball");
                break;
                
            case PowerupType.NormalBall:
                DesactivatePowerBall();
                Debug.Log("PowerUp aplicado: Normal Ball - Modo PowerBall desactivado");
                break;
                
            case PowerupType.ExtraBalls:
                CreateExtraBalls();
                Debug.Log("PowerUp aplicado: Extra Balls");
                break;
                
            case PowerupType.Magnet:
                // Activar modo imán sin límite de tiempo (duración infinita)
                paddle.ActivateMagnetMode(0);
                Debug.Log("PowerUp aplicado: Magnet - La próxima bola se pegará a la paleta");
                break;
                
            case PowerupType.NextLevel:
                GoToNextLevel();
                Debug.Log("PowerUp aplicado: Next Level");
                break;
            
            
            case PowerupType.SpeedUp:
                ChangeAllBallsSpeed(1.5f, 20f); // Doble velocidad durante 20 segundos
                Debug.Log("PowerUp aplicado: Speed Up - Velocidad de la bola aumentada");
                break;
            
            case PowerupType.SlowDown:
                ChangeAllBallsSpeed(0.75f, 20f); // Mitad de velocidad durante 20 segundos
                Debug.Log("PowerUp aplicado: Slow Down - Velocidad de la bola reducida");
                break;
            case PowerupType.Shoot:
                // Cargar el prefab de bala directamente aquí para asegurarnos
                if (GameManager.Instance != null)
                {
                    // Comprobar si el jugador tiene menos de 3 vidas
                    if (GameManager.Instance.GetCurrentLives() < 3)
                    {
                        // Añadir una vida
                        GameManager.Instance.AddLife();
                        Debug.Log("PowerUp aplicado: Heart - Vida extra añadida");
                    }
                    else
                    {
                        // Si ya tiene el máximo de vidas, solo dar puntos extra
                        GameManager.Instance.AddPoints(50);
                        Debug.Log("PowerUp aplicado: Heart - Ya tienes vidas máximas, +50 puntos");
                    }
                }
                else
                {
                    Debug.LogError("No se encontró el GameManager para añadir vida");
                }
                break;
        }
    }
    
    // Activa el modo Power Ball en todas las bolas del juego
    private void ActivatePowerBall()
    {
        BallController[] balls = FindObjectsOfType<BallController>();
        foreach (var ball in balls)
        {
            ball.SetPowerBallMode(true);
        }
    }
    
    // Desactiva el modo Power Ball en todas las bolas
    private void DesactivatePowerBall()
    {
        // Buscar todas las bolas activas en la escena
        BallController[] balls = FindObjectsOfType<BallController>();
        
        // Desactivar el modo PowerBall en cada una
        foreach (var ball in balls)
        {
            // Llamar al método que ya existe en BallController con enabled=false
            ball.SetPowerBallMode(false);
        }
        
        Debug.Log($"Modo PowerBall desactivado en {balls.Length} bolas");
    }
    
    // Crea dos bolas extra clonando una bola existente
    private void CreateExtraBalls()
    {
        // Buscar bolas existentes para clonar una
        BallController[] existingBalls = FindObjectsOfType<BallController>();
        
        if (existingBalls.Length > 0)
        {
            // Seleccionar una bola aleatoria como referencia para clonar
            BallController referenceBall = existingBalls[Random.Range(0, existingBalls.Length)];
            Vector3 ballPosition = referenceBall.transform.position;
            
            // Verificar si la bola de referencia está en modo PowerBall
            bool isPowerBall = false;
            System.Reflection.FieldInfo powerBallField = referenceBall.GetType().GetField("isPowerBall", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic);
            
            if (powerBallField != null)
            {
                isPowerBall = (bool)powerBallField.GetValue(referenceBall);
            }
            
            // Obtener la dirección actual de la bola de referencia
            Rigidbody refRb = referenceBall.GetComponent<Rigidbody>();
            Vector3 currentDirection = Vector3.zero;
            
            if (refRb != null && refRb.linearVelocity.magnitude > 0)
            {
                currentDirection = refRb.linearVelocity.normalized;
            }
            else
            {
                // Si no hay velocidad, usar dirección por defecto hacia arriba
                currentDirection = new Vector3(0, 0, 1);
            }
            
            // Ángulos de desviación para las nuevas bolas
            float[] deviationAngles = { -30f, 30f }; // 30 grados a la izquierda y derecha
            
            // Crea dos bolas nuevas mediante clonación
            for (int i = 0; i < 2; i++)
            {
                // Posiciones más separadas
                Vector3 spawnPosition = ballPosition + new Vector3(
                    (i == 0) ? -0.8f : 0.8f, // Una a la izquierda, otra a la derecha
                    0, 
                    (i == 0) ? 0.3f : -0.3f); // Ligera variación en Z
                
                // Clonar la bola de referencia
                GameObject newBall = Instantiate(referenceBall.gameObject, spawnPosition, Quaternion.identity);
                
                // Obtener el controller de la nueva bola
                BallController ballController = newBall.GetComponent<BallController>();
                if (ballController != null)
                {
                    // Aplicar el mismo efecto de PowerBall si estaba activo
                    if (isPowerBall)
                    {
                        ballController.SetPowerBallMode(true);
                    }
                    
                    // Limpiar cualquier transformación padre o asociación a la paleta
                    ballController.transform.SetParent(null);
                    
                    // Lanzar la bola con la dirección modificada de la bola original
                    StartCoroutine(LaunchBallWithModifiedDirection(ballController, currentDirection, deviationAngles[i]));
                }
            }
            
            Debug.Log("Se crearon 2 bolas nuevas con dirección similar a la original pero desviadas");
        }
        else
        {
            Debug.LogWarning("No se encontró ninguna bola en el juego para crear bolas adicionales");
        }
    }

    // Nuevo método para lanzar la bola con una dirección modificada
    private IEnumerator LaunchBallWithModifiedDirection(BallController ball, Vector3 originalDirection, float deviationAngle)
    {
        // Esperar un frame para que Unity termine la inicialización
        yield return null;
        
        // Rotar la dirección original según el ángulo de desviación
        // La rotación se aplica alrededor del eje Y para desviar hacia izquierda o derecha
        Vector3 newDirection = Quaternion.Euler(0, deviationAngle, 0) * originalDirection;
        
        // Normalizar para asegurar que la magnitud es 1
        newDirection.Normalize();
        
        // Lanzar la bola con la dirección modificada
        ball.LaunchBall(newDirection);
        
        Debug.Log($"Bola nueva lanzada con dirección desviada {newDirection} (ángulo de desviación: {deviationAngle}°)");
    }
    
    // Activa el modo imán en la paleta
    private void ActivateMagnet(PalletController paddle)
    {
        paddle.ActivateMagnetMode(powerupDuration);
    }
    
    // Pasa al siguiente nivel
    private void GoToNextLevel()
    {
        // Busca el GameManager para cambiar de nivel
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.GoToNextLevel();
        }
        else
        {
            Debug.LogError("No se encontró el GameManager para cambiar de nivel");
        }
    }
    private void ChangeAllBallsSpeed(float speedMultiplier, float duration)
    {
        BallController[] balls = FindObjectsOfType<BallController>();
        foreach (var ball in balls)
        {
            ball.ChangeSpeed(speedMultiplier, duration);
        }
    }
}
