using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    [Header("Block Properties")]
    [SerializeField] private int blockLevel = 0; // 0 is the lowest level
    [SerializeField] private float descendSpeed = 2f;
    [SerializeField] private GameObject breakEffect; // Particle effect or animation prefab for breaking
    [SerializeField] private float breakAnimationDuration = 0.5f;
    [SerializeField] private float blockHeight = 1f; // Height of each block level
    
    [Header("PowerUp Settings")]
    [SerializeField] private bool enablePowerUps = true;
    [SerializeField, Range(0f, 1f)] private float powerUpChance = 0.3f;
    
    // Variables para el seguimiento de bloques
    private static int totalBlocksInitial = 0;
    private static int blocksDestroyed = 0;
    private static bool isInitialized = false;
    
    // Lista de tipos de powerups
    private enum PowerUpType 
    { 
        ExpandPaddle,    // "more" - Agranda la paleta
        ShrinkPaddle,    // "less" - Reduce la paleta
        PowerBall,       // Bola que atraviesa los bloques
        NormalBall,      // Restaura la bola al modo normal
        ExtraBalls,      // Añade dos bolas extra
        Magnet,          // La bola se pega a la paleta
        NextLevel        // Pasa al siguiente nivel
    }
    
    // Diccionario que mapea tipos a nombres de prefab
    private Dictionary<PowerUpType, string> powerUpPrefabNames = new Dictionary<PowerUpType, string>()
    {
        { PowerUpType.ExpandPaddle, "more" },
        { PowerUpType.ShrinkPaddle, "less" },
        { PowerUpType.PowerBall, "powerball" },
        { PowerUpType.NormalBall, "normalball" },
        { PowerUpType.ExtraBalls, "extraballs" },
        { PowerUpType.Magnet, "magnet" },
        { PowerUpType.NextLevel, "nextlevel" }
    };
    
    // Variables existentes
    private bool isBreaking = false;
    private bool isLowLevel = false;
    private bool shouldDescend = false;
    private bool isDescending = false;
    private Vector3 targetPosition;
    
    private void Awake()
    {
        // Inicializar contador de bloques solo una vez por nivel
        if (!isInitialized)
        {
            totalBlocksInitial = FindObjectsOfType<BlockController>().Length;
            blocksDestroyed = 0;
            isInitialized = true;
            Debug.Log($"Nivel inicializado con {totalBlocksInitial} bloques");
        }
    }
    
    private void Start()
    {
        // Initialize block level based on y position
        blockLevel = Mathf.RoundToInt(transform.position.y / blockHeight);
        
        // Check if block is at the lowest level (y=0)
        isLowLevel = blockLevel == 0;
        
        // Set initial target position one level down
        UpdateTargetPosition();
    }
    
    private void Update()
    {
        // If block is in the process of descending
        if (shouldDescend && !isBreaking)
        {
            isDescending = true;
            
            // Move block towards target position
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                descendSpeed * Time.deltaTime
            );
            
            // Check if block has reached the target level
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                blockLevel--;
                isLowLevel = blockLevel == 0;
                shouldDescend = false;
                isDescending = false;
                
                // Update target for next potential descent
                UpdateTargetPosition();
            }
        }
    }
    
    private void UpdateTargetPosition()
    {
        // Target is one level down from current position
        targetPosition = new Vector3(
            transform.position.x,
            blockLevel > 0 ? (blockLevel - 1) * blockHeight : 0f,
            transform.position.z
        );
    }
    
    public void OnHit()
    {
        // Only breakable if at the lowest level and not currently descending or breaking
        if (isLowLevel && !isDescending && !isBreaking)
        {
            StartCoroutine(BreakAnimation());
        }
    }
    
    // Método llamado cuando un bloque es destruido
    public void Hit()
    {
        // Si el bloque ya está en proceso de destrucción, ignoramos
        if (isBreaking)
            return;

        // Incrementar contador de bloques destruidos
        blocksDestroyed++;
        Debug.Log($"Bloque destruido. Total: {blocksDestroyed}/{totalBlocksInitial} " +
                  $"({GetDestroyedPercentage():F1}%)");

        // Iniciar la animación de destrucción
        StartCoroutine(BreakAnimation());
    }
    
    private IEnumerator BreakAnimation()
    {
        isBreaking = true;
        
        // Animation code...
        if (breakEffect != null)
        {
            Instantiate(breakEffect, transform.position, Quaternion.identity);
        }
        
        // Visual breaking animation
        Vector3 originalScale = transform.localScale;
        Quaternion originalRotation = transform.rotation;
        float elapsed = 0f;
        
        while (elapsed < breakAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / breakAnimationDuration;
            
            // Scale down
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            
            // Add some rotation for effect
            transform.Rotate(Vector3.one * 180f * Time.deltaTime);
            
            yield return null;
        }
        
        // Notify blocks above to descend
        NotifyBlocksAbove();

        // Lógica de generación de powerups con probabilidad
        if (enablePowerUps && Random.value <= powerUpChance)
        {
            SpawnRandomPowerUp();
        }
        
        // Destroy the block
        Destroy(gameObject);
    }
    
    private void SpawnRandomPowerUp()
    {
        // Elegir un tipo de powerup basado en probabilidades
        PowerUpType selectedType = SelectPowerUpType();
        
        // Obtener el nombre del prefab
        string prefabName = powerUpPrefabNames[selectedType];
        
        // Cargar y crear el powerup
        GameObject powerupPrefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        
        if (powerupPrefab != null)
        {
            GameObject powerup = Instantiate(
                powerupPrefab,
                transform.position,
                Quaternion.identity
            );
            
            Debug.Log("PowerUp generado: " + prefabName);
        }
        else
        {
            Debug.LogWarning("No se pudo cargar el prefab: Resources/Prefabs/" + prefabName);
        }
    }
    
    private PowerUpType SelectPowerUpType()
    {
        // Verificar si se puede generar el powerup NextLevel
        bool canGenerateNextLevel = GetDestroyedPercentage() >= 95f;
        
        // Lista de tipos disponibles, excluyendo NextLevel si no se cumple la condición
        List<PowerUpType> availableTypes = new List<PowerUpType>();
        
        foreach (PowerUpType type in System.Enum.GetValues(typeof(PowerUpType)))
        {
            // Solo incluir NextLevel si se ha destruido más del 95% de los bloques
            if (type != PowerUpType.NextLevel || canGenerateNextLevel)
            {
                availableTypes.Add(type);
            }
        }
        
        // Si NextLevel está disponible, darle menor probabilidad
        if (canGenerateNextLevel)
        {
            // Añadir tipos comunes múltiples veces para aumentar su probabilidad relativa
            // NextLevel solo aparece una vez en la lista, los demás aparecen 3 veces
            List<PowerUpType> weightedTypes = new List<PowerUpType>(availableTypes);
            
            foreach (PowerUpType type in availableTypes)
            {
                if (type != PowerUpType.NextLevel)
                {
                    // Añadir cada tipo común 2 veces más (total 3 apariciones)
                    weightedTypes.Add(type);
                    weightedTypes.Add(type);
                }
            }
            
            // Seleccionar aleatoriamente de la lista ponderada
            int randomIndex = Random.Range(0, weightedTypes.Count);
            return weightedTypes[randomIndex];
        }
        else
        {
            // Seleccionar aleatoriamente entre los tipos disponibles (sin NextLevel)
            int randomIndex = Random.Range(0, availableTypes.Count);
            return availableTypes[randomIndex];
        }
    }
    
    private void NotifyBlocksAbove()
    {
        // Cast a ray upwards to find ALL blocks above this one
        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            Vector3.up,
            100f // Increased range to detect all blocks above
        );
        
        foreach (RaycastHit hit in hits)
        {
            // Check if hit object has a BlockController
            BlockController blockAbove = hit.collider.GetComponent<BlockController>();
            if (blockAbove != null)
            {
                // Tell the block above to start descending
                blockAbove.StartDescending();
            }
        }
    }
    
    public void StartDescending()
    {
        shouldDescend = true;
    }

    // Método para obtener el porcentaje de bloques destruidos
    private float GetDestroyedPercentage()
    {
        if (totalBlocksInitial == 0)
            return 0;
            
        return (float)blocksDestroyed / totalBlocksInitial * 100f;
    }

    // Método para reiniciar los contadores estáticos cuando se cambia de nivel
    public static void ResetLevelCounters()
    {
        totalBlocksInitial = 0;
        blocksDestroyed = 0;
        isInitialized = false;
    }
}