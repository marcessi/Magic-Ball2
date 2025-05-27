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
    
    // Crea dos bolas extra
    private void CreateExtraBalls()
    {
        if (ballPrefab == null)
        {
            // Buscar la bola actual como plantilla
            BallController mainBall = FindObjectOfType<BallController>();
            if (mainBall != null && mainBall.isMainBall) 
            {
                ballPrefab = mainBall.gameObject;
                Debug.Log("Usando la bola principal como ballPrefab");
            }
            else
            {
                Debug.LogError("No se pudo encontrar ballPrefab ni una bola existente para duplicar");
                return;
            }
        }
        
        // Busca la bola principal para usar su posición EXACTA
        BallController existingMainBall = FindMainBall();
        
        if (existingMainBall != null)
        {
            Vector3 ballPosition = existingMainBall.transform.position;
            
            // Crea dos bolas desde la posición exacta de la bola principal
            for (int i = 0; i < 2; i++)
            {
                // Posición ligeramente diferente para evitar superposición - usar solo offset X
                Vector3 spawnPosition = ballPosition + new Vector3(i == 0 ? -0.5f : 0.5f, 0, 0);
                
                // Crear la bola con la opción de EXACTA posición
                GameObject newBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
                
                // Configurar la bola extra primero antes de cualquier otra acción
                BallController ballController = newBall.GetComponent<BallController>();
                if (ballController != null)
                {
                    // IMPORTANTE: Primero configurar como bola extra
                    ballController.SetAsExtraBall();
                    
                    // IMPORTANTE: Esperar una fracción de segundo para asegurar inicialización
                    StartCoroutine(LaunchBallDelayed(ballController, i));
                }
            }
        }
        else
        {
            Debug.LogWarning("No se encontró la bola principal para crear bolas extras");
        }
    }

    // Añade este método auxiliar para encontrar la bola principal
    private BallController FindMainBall()
    {
        BallController[] balls = FindObjectsOfType<BallController>();
        foreach (var ball in balls)
        {
            if (ball.isMainBall)
                return ball;
        }
        return null;
    }

    // Añade esta corrutina para asegurar el lanzamiento correcto
    private IEnumerator LaunchBallDelayed(BallController ball, int index)
    {
        // Esperar un frame para que Unity termine la inicialización
        yield return null;
        
        // Vector dirección equilibrado para asegurar que vaya hacia arriba/adelante
        // Dirección 1: hacia arriba-izquierda, Dirección 2: hacia arriba-derecha
        Vector2 direction = new Vector2((index == 0) ? -0.5f : 0.5f, 1f).normalized;
        
        // Forzar el lanzamiento inmediato
        ball.ForceImmediateLaunch(direction);
        
        Debug.Log($"Bola extra {index+1} lanzada con dirección {direction}");
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
