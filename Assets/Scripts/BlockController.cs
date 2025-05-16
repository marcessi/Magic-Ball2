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
    private float floatingCheckDelay = 0.1f;
    private float lastFloatingCheck = 0f;
    private static Dictionary<Vector2, bool> brokenColumns = new Dictionary<Vector2, bool>(); // Rastrea columnas rotas
    private Vector2 columnPosition; // Posición de la columna (X,Z) de este bloque
    
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

        // Calculate column position (only X and Z coordinates)
        columnPosition = new Vector2(position.x, position.z);

        if (!brokenColumns.ContainsKey(columnPosition))
        {
        brokenColumns[columnPosition] = false;
        }
        
        // Check if block is at the lowest level (y=0 + offset)
        isLowLevel = blockLevel == 0;
        
        // Set initial target position one level down
        UpdateTargetPosition();
    }
    
    private void Update()
    {
        // If block is in the process of descending
        if (shouldDescend && !isBreaking)
        {
            if (!isDescending)
            {
                // Antes de iniciar el descenso, calcular la posición más baja posible
                FindLowestPossiblePosition();
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

        lastFloatingCheck += Time.deltaTime;
        if (lastFloatingCheck >= floatingCheckDelay && !isDescending && !shouldDescend)
        {
            CheckIfFloating();
            lastFloatingCheck = 0f;
        }
    }
    
    private void UpdateTargetPosition()
    {
        // Esta función ahora solo se usa para inicialización
        targetPosition = new Vector3(
            transform.position.x,
            blockLevel > 0 ? (blockLevel - 1) * blockHeight + pivotOffset : pivotOffset,
            transform.position.z
        );
    }

    private void FindLowestPossiblePosition()
    {
        // Origen del rayo: posición del bloque actual
        Vector3 rayOrigin = transform.position;
        
        // Vector de tamaño de la caja para el BoxCast
        Vector3 boxSize = new Vector3(0.6f, 0.1f, 0.6f);
        
        // Distancia máxima del rayo (hacia abajo)
        float maxRayDistance = transform.position.y;
        
        // Lista para almacenar los bloques encontrados
        List<RaycastHit> foundBlocks = new List<RaycastHit>();
        
        // Realizar BoxCastAll para detectar todos los bloques debajo
        RaycastHit[] hits = Physics.BoxCastAll(
            rayOrigin,        // Origen
            boxSize * 0.5f,   // Mitad de los extents
            Vector3.down,     // Dirección hacia abajo
            Quaternion.identity, // Sin rotación
            maxRayDistance    // Distancia máxima
        );
        
        // Buscar el bloque más bajo que no está descendiendo o el suelo
        float lowestY = 0f + pivotOffset; // Posición mínima (suelo + offset)
        bool foundStableBlock = false;
        
        foreach (RaycastHit hit in hits)
        {
            // Ignorar el propio bloque
            if (hit.collider.gameObject == gameObject)
                continue;
                
            BlockController blockBelow = hit.collider.GetComponent<BlockController>();
            
            if (blockBelow != null)
            {
                // Si el bloque está descendiendo, consultar su posición objetivo
                if (blockBelow.IsDescending())
                {
                    // Obtener la posición objetivo del bloque en descenso
                    Vector3 targetPos = blockBelow.GetTargetPosition();
                    float blockTopY = targetPos.y + blockHeight;
                    
                    // Actualizar la posición más baja si es mayor que la actual
                    if (blockTopY > lowestY)
                    {
                        lowestY = blockTopY;
                        foundStableBlock = true;
                    }
                }
                else
                {
                    // Si el bloque no está descendiendo, usar su posición actual
                    float blockTopY = blockBelow.transform.position.y + blockHeight;
                    
                    // Actualizar la posición más baja si es mayor que la actual
                    if (blockTopY > lowestY)
                    {
                        lowestY = blockTopY;
                        foundStableBlock = true;
                    }
                }
            }
            else if (!foundStableBlock)
            {
                // Si no es un bloque y no hemos encontrado bloques estables,
                // podría ser el suelo u otro obstáculo
                float obstacleTopY = hit.point.y + pivotOffset;
                
                if (obstacleTopY > lowestY)
                {
                    lowestY = obstacleTopY;
                }
            }
        }

        float adjustedY = lowestY - pivotOffset;
        float roundedY = Mathf.Floor(adjustedY / blockHeight) * blockHeight;
        lowestY = roundedY + pivotOffset;
        // Establecer la posición objetivo final
        targetPosition = new Vector3(transform.position.x, lowestY, transform.position.z);
        
        Debug.Log($"Bloque en {transform.position} descenderá hasta {targetPosition}");
    }

    private void CheckIfFloating()
    {
        // Si estamos en el nivel 0, no puede estar flotando
        if (blockLevel == 0)
            return;

        // Verificar si la columna de este bloque ha sido rota
        if (!brokenColumns.ContainsKey(columnPosition) || !brokenColumns[columnPosition])
            return;
            
        // Origen del rayo: posición del bloque actual
        Vector3 rayOrigin = transform.position;
        Vector3 boxSize = new Vector3(0.6f, 0.1f, 0.6f);
        float rayDistance = 0.8f*blockHeight; // Suficiente para detectar espacios de un nivel
        
        // Realizar cast para ver si hay algo debajo
        RaycastHit[] hits = Physics.BoxCastAll(
            rayOrigin, boxSize * 0.5f, Vector3.down, 
            Quaternion.identity, rayDistance);
        
        bool foundSupport = false;
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject != gameObject)
            {
                // Hay algo debajo
                foundSupport = true;
                break;
            }
        }
        
        // Si no hay nada debajo, iniciar descenso
        if (!foundSupport)
        {
            Debug.LogWarning($"Bloque flotante detectado en {transform.position}. Iniciando descenso.");
            StartDescending();
        }
    }

    public bool IsDescending()
    {
        return isDescending;
    }

    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
    
    // Método llamado cuando un bloque es destruido
    public void Hit()
    {
        // Si el bloque ya está en proceso de destrucción, ignoramos
        if (isBreaking)
            return;
        
        brokenColumns[columnPosition] = true;

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
        //NotifyBlocksAbove();

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
        // Origen del rayo: posición del bloque actual
        Vector3 rayOrigin = transform.position;
        
        // Dirección: hacia arriba
        Vector3 rayDirection = Vector3.up;
        
        // Vector de tamaño de la caja para el BoxCast
        Vector3 boxSize = new Vector3(0.6f, 0.1f, 0.6f); // Caja delgada pero ancha
        
        // Distancia máxima del rayo (bastante alta para alcanzar toda la columna)
        float maxRayDistance = 20f; // Ajustar según la altura de tu nivel
        
        // Lista para almacenar todos los bloques detectados
        List<BlockController> blocksAbove = new List<BlockController>();
        
        // Realizar un BoxCast hacia arriba para detectar todos los bloques
        RaycastHit[] hits = Physics.BoxCastAll(
            rayOrigin,        // Origen
            boxSize * 0.5f,   // Mitad de los extents
            rayDirection,     // Dirección hacia arriba
            Quaternion.identity, // Sin rotación
            maxRayDistance    // Distancia máxima
        );
        
        Debug.Log($"Buscando TODOS los bloques encima de {transform.position}. Encontrados: {hits.Length}");
        
        // Ordenar los resultados por distancia (los más cercanos primero)
        System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
        
        // Procesar todos los hits encontrados
        foreach (RaycastHit hit in hits)
        {
            BlockController blockAbove = hit.collider.GetComponent<BlockController>();
            if (blockAbove != null && blockAbove != this) // Excluir el propio bloque
            {
                Debug.Log($"Notificando bloque en {blockAbove.transform.position} para descender");
                blockAbove.StartDescending();
            }
            else if (hit.collider.gameObject != gameObject) // Excluir este mismo objeto
            {
                Debug.Log($"Objeto detectado sin BlockController: {hit.collider.gameObject.name}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibuja la caja de detección original (pequeña)
        Vector3 center = transform.position + Vector3.up * blockHeight + Vector3.up * -pivotOffset;
        Vector3 halfExtents = new Vector3(0.6f, blockHeight * 0.8f, 0.6f) * 0.5f;
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2);
        Gizmos.matrix = Matrix4x4.identity;
        
        // Dibuja el BoxCast vertical (columna completa)
        Gizmos.color = Color.green;
        Vector3 boxSize = new Vector3(0.6f, 0.1f, 0.6f);
        Vector3 rayStart = transform.position;
        Vector3 rayEnd = transform.position + Vector3.up * 20f;
        
        // Dibujar la línea central
        Gizmos.DrawLine(rayStart, rayEnd);
        
        // Dibujar las cajas a lo largo del rayo
        Gizmos.DrawWireCube(rayStart, boxSize);
        Gizmos.DrawWireCube(rayStart + Vector3.up * 10f, boxSize);
        Gizmos.DrawWireCube(rayEnd, boxSize);
        
        // Dibuja la posición objetivo para el descenso
        if (blockLevel > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 targetPos = new Vector3(
                transform.position.x,
                (blockLevel - 1) * blockHeight + pivotOffset,
                transform.position.z
            );
            Gizmos.DrawWireSphere(targetPos, 0.3f);
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