using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    [Header("Block Properties")]
    [SerializeField] private int blockLevel = 0; // 0 is the lowest level
    [SerializeField] private float descendSpeed = 2f;
    [SerializeField] private GameObject breakEffect; // Particle effect or animation prefab for breaking
    [SerializeField] private float breakAnimationDuration = 0.3f;
    [SerializeField] private float blockHeight = 1f; // Height of each block level
    [SerializeField] private float pivotOffset = 0f; // Offset for blocks with displaced pivot
    
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
        NextLevel,       // Pasa al siguiente nivel
        SpeedUp,         // Aumenta la velocidad de la bola
        SlowDown,        // Disminuye la velocidad de la bola
        Shoot            // Activa el modo disparo de la paleta
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
        { PowerUpType.NextLevel, "nextlevel" },
        { PowerUpType.SpeedUp, "speedup" },    // Nuevo powerup de velocidad aumentada
        { PowerUpType.SlowDown, "slowdown" },  // Nuevo powerup de velocidad reducida
        { PowerUpType.Shoot, "shoot" }         // Nuevo powerup de disparo
    };
    
    // Variables existentes
    private bool isBreaking = false;
    private bool isLowLevel = false;
    private bool shouldDescend = false;
    private bool isDescending = false;
    private Vector3 targetPosition;
    private float descendUnits = 0f;
    
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
        // Round x and z positions to nearest 0.5 units
        Vector3 position = transform.position;
        position.x = Mathf.Round(position.x * 2) / 2; // Round to nearest 0.5
        position.z = Mathf.Round(position.z * 2) / 2; // Round to nearest 0.5
        transform.position = position;
        
        // Initialize block level based on y position, accounting for pivot offset
        blockLevel = Mathf.RoundToInt((transform.position.y - pivotOffset) / blockHeight);

        position.x = Mathf.Round(position.x);
        position.z = Mathf.Round(position.z - 0.5f) + 0.5f;
        
        // Check if block is at the lowest level (y=0 + offset)
        isLowLevel = blockLevel == 0;
        
        // Set initial target position one level down
        targetPosition = new Vector3(
            transform.position.x,
            blockLevel > 0 ? (blockLevel - 1) * blockHeight + pivotOffset : pivotOffset,
            transform.position.z
        );
    }
    
    private void Update()
    {
        // If block is in the process of descending
        if (shouldDescend && !isBreaking)
        {
            if (!isDescending)
            {
                isDescending = true;
            }
            
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
                // Actualizar el nivel del bloque basado en su nueva posición Y
                blockLevel = Mathf.RoundToInt((transform.position.y - pivotOffset) / blockHeight);
                isLowLevel = blockLevel == 0;
                shouldDescend = false;
                isDescending = false;
            }
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
        Debug.Log($"Bloque destruido. Total: {blocksDestroyed}/{totalBlocksInitial} ({GetDestroyedPercentage():F1}%)");

        // Añadir puntos al GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPoints(10); // 10 puntos por bloque
        }

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

    private void NotifyBlocksAbove()
    {
        // Origen del rayo: posición del bloque actual
        Vector3 rayOrigin = transform.position;
        
        // Vector de tamaño de la caja para el BoxCast
        Vector3 boxSize = new Vector3(0.6f, 0.1f, 0.6f); // Caja delgada pero ancha
        
        // Distancia para detectar solo bloques inmediatamente encima
        float rayDistance = 20f; // Lo suficientemente largo para detectar toda la columna
        
        // Realizar un BoxCast hacia arriba para detectar bloques
        RaycastHit[] hits = Physics.BoxCastAll(
            rayOrigin,        // Origen
            boxSize * 0.5f,   // Mitad de los extents
            Vector3.up,       // Dirección hacia arriba
            Quaternion.identity, // Sin rotación
            rayDistance       // Distancia para toda la columna
        );
        
        // Ordenar los hits por distancia (más cercanos primero)
        System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
        
        // Variables para el algoritmo de propagación
        bool foundFirstBlock = false;
        float accumulatedDistance = 0f;
        BlockController previousBlock = null;
        
        // Recorrer todos los hits ordenados por distancia
        foreach (RaycastHit hit in hits)
        {
            BlockController blockAbove = hit.collider.GetComponent<BlockController>();
            
            // Ignorar objetos que no son bloques o el propio bloque
            if (blockAbove == null || blockAbove == this)
                continue;
            
            // Calculamos la distancia entre bloques
            float distance;
            
            if (!foundFirstBlock)
            {
                // Para el primer bloque, la distancia es relativa al bloque que se rompe
                distance = hit.distance;
                foundFirstBlock = true;
                accumulatedDistance = distance;
                
                // El primer bloque desciende (distancia - 1 unidad de bloque) o al menos 1 si están muy juntos
                float descendUnits = Mathf.Max(1f, Mathf.Floor(distance / blockHeight));
                
                Debug.Log($"Primer bloque en {blockAbove.transform.position} descenderá {descendUnits} unidades. Distancia: {distance}");
                blockAbove.DescendExactAmount(descendUnits * blockHeight);
            }
            else if (previousBlock != null)
            {
                // Para los siguientes bloques, calculamos la distancia respecto al bloque anterior
                distance = hit.distance - accumulatedDistance;
                accumulatedDistance = hit.distance;
                
                // Este bloque desciende lo mismo que el anterior + (distancia - 1 unidad)
                float additionalUnits = Mathf.Max(0f, Mathf.Floor(distance / blockHeight) - 1f);
                float descendUnits = previousBlock.GetDescendUnits() + additionalUnits;
                
                Debug.Log($"Bloque en {blockAbove.transform.position} descenderá {descendUnits} unidades. Distancia adicional: {distance}");
                blockAbove.DescendExactAmount(descendUnits * blockHeight);
            }
            
            previousBlock = blockAbove;
        }
    }

    public void DescendExactAmount(float amount)
    {
        // Guardar cuántas unidades desciende este bloque (para la recursión)
        descendUnits = amount / blockHeight;
        
        // Calcular posición objetivo
        targetPosition = new Vector3(
            transform.position.x,
            transform.position.y - amount,
            transform.position.z
        );
        
        // Asegurarse de que no baje por debajo del suelo
        if (targetPosition.y < pivotOffset)
        {
            targetPosition.y = pivotOffset;
            descendUnits = (transform.position.y - pivotOffset) / blockHeight;
        }
        
        Debug.Log($"Bloque en {transform.position} descenderá {descendUnits} unidades hasta {targetPosition}");
        shouldDescend = true;
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

    public void StartDescending()
    {
        shouldDescend = true;
    }

    private float GetDestroyedPercentage()
    {
        if (totalBlocksInitial == 0)
            return 0;
            
        return (float)blocksDestroyed / totalBlocksInitial * 100f;
    }

    public static void ResetLevelCounters()
    {
        totalBlocksInitial = 0;
        blocksDestroyed = 0;
        isInitialized = false;
    }

        public bool IsDescending()
    {
        return isDescending;
    }

    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }

    public float GetDescendUnits()
    {
        return descendUnits;
    }
}