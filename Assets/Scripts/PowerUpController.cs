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

    private void Start()
    {
        transform.localScale *= powerupScale;
        
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
        
        // Cargar automáticamente el prefab de la bola si es necesario
        if (ballPrefab == null && powerupType == PowerupType.ExtraBalls)
        {
            ballPrefab = Resources.Load<GameObject>("Prefabs/Ball");
        }

        if (powerupType == PowerupType.Shoot)
        {
            // Buscar el prefab de bala
            GameObject bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet");
            
            // Buscar la paleta para asignarle el prefab
            PalletController paddle = FindObjectOfType<PalletController>();
            if (paddle != null && bulletPrefab != null)
            {
                // Usar reflexión para asignar el prefab (ya que el campo puede ser privado)
                System.Reflection.FieldInfo field = paddle.GetType().GetField("bulletPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(paddle, bulletPrefab);
                }
            }
        }
        
        Debug.Log("PowerUp inicializado: " + gameObject.name + " - Tipo: " + powerupType);
    }

    private void Update()
    {
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
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
                ChangeAllBallsSpeed(2.0f, 20f); // Doble velocidad durante 20 segundos
                Debug.Log("PowerUp aplicado: Speed Up - Velocidad de la bola aumentada");
                break;
            
            case PowerupType.SlowDown:
                ChangeAllBallsSpeed(0.5f, 20f); // Mitad de velocidad durante 20 segundos
                Debug.Log("PowerUp aplicado: Slow Down - Velocidad de la bola reducida");
                break;
            case PowerupType.Shoot:
                // Activar modo de disparo durante 20 segundos
                paddle.ActivateShootMode(20f);
                Debug.Log("PowerUp aplicado: Shoot - Disparando balas durante 20 segundos");
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
            
            // Ángulos predefinidos para las direcciones de las bolas (más separados)
            float[] angles = { 60f, 120f }; // Una a 60° (hacia arriba-derecha) y otra a 120° (hacia arriba-izquierda)
            
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
                    
                    // Lanzar la bola con un ángulo predefinido
                    StartCoroutine(LaunchBallDelayed(ballController, angles[i]));
                }
            }
            
            Debug.Log("Se crearon 2 bolas nuevas con los mismos efectos que la original");
        }
        else
        {
            Debug.LogWarning("No se encontró ninguna bola en el juego para crear bolas adicionales");
        }
    }

    // Modificado para aceptar un ángulo específico
    private IEnumerator LaunchBallDelayed(BallController ball, float angle)
    {
        // Esperar un frame para que Unity termine la inicialización
        yield return null;
        
        // Convertir ángulo a radianes
        float radians = angle * Mathf.Deg2Rad;
        
        // Convertir ángulo a dirección (x,z)
        Vector3 direction = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians)).normalized;
        
        ball.LaunchBall(direction);
        
        Debug.Log($"Bola nueva lanzada con dirección {direction} (ángulo: {angle}°)");
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
