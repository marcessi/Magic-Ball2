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
        NextLevel 
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
                DeactivatePowerBall();
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
    private void DeactivatePowerBall()
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
            Debug.LogError("Falta el prefab de la bola para crear bolas extra");
            return;
        }
        
        // Busca la bola actual para usar su posición
        BallController[] existingBalls = FindObjectsOfType<BallController>();
        if (existingBalls.Length > 0)
        {
            Vector3 ballPosition = existingBalls[0].transform.position;
            
            // Crea dos bolas con ligeras variaciones en su dirección inicial
            for (int i = 0; i < 2; i++)
            {
                Vector3 spawnPosition = ballPosition + new Vector3(i * 0.5f, 0, 0);
                GameObject newBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
                
                // Lanza la bola en una dirección ligeramente distinta
                BallController ballController = newBall.GetComponent<BallController>();
                if (ballController != null)
                {
                    ballController.LaunchBall(Random.insideUnitCircle.normalized);
                }
            }
        }
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
}
