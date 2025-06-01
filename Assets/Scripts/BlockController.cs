using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Asegurarse de incluir esto para SceneManager

public class BlockController : MonoBehaviour
{
    [Header("Block Properties")]
    [SerializeField] private int blockLevel = 0; // 0 is the lowest level
    [SerializeField] private float descendSpeed = 2f;
    [SerializeField] private GameObject breakEffect; // Particle effect or animation prefab for breaking
    [SerializeField] private float breakAnimationDuration = 0.3f;
    [SerializeField] private float pivotOffset = 0f; // Offset for blocks with displaced pivot
    
    [Header("PowerUp Settings")]
    private bool enablePowerUps = true;
    private float powerUpChance = 0.3f;

 
    
    // Variables para el seguimiento de bloques
    private static int totalBlocksInitial = 0;
    private static int blocksDestroyed = 0;
    private static bool isInitialized = false;
    
    // Añadir una variable para identificar el nivel actual
    private static int currentLevelId = -1;
    
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
    
    // Agregar contador estático para los power ups activos
    private static int activePowerUps = 0;
    private static int maxPowerUps = 10;

    private void Awake()
    {
        // Obtener el ID del nivel actual
        int levelId = SceneManager.GetActiveScene().buildIndex;
        
        // Si cambiamos de nivel, forzar la reinicialización
        if (levelId != currentLevelId)
        {
            isInitialized = false;
            blocksDestroyed = 0; // Reiniciar el contador de bloques destruidos
            totalBlocksInitial = 0; // Reiniciar el contador total
            currentLevelId = levelId;
            Debug.Log($"Cambiado a nivel {levelId} - Forzando reinicialización completa");
        }
        
        // Inicializar contador de bloques solo una vez por nivel y por bloque
        // Esperar un poco más para asegurar que todos los bloques estén cargados
        Invoke("InitializeBlockCount", Random.Range(0.2f, 0.5f));
    }
    
    // Nuevo método para inicializar el conteo de bloques después de una pequeña pausa
    private void InitializeBlockCount()
    {
        if (!isInitialized)
        {
            // Esperar un poco más antes de contar para asegurar que todos los bloques estén cargados
            // Contar bloques después de que todos se hayan instanciado - CONTAR SOLO LOS ACTIVOS
            BlockController[] blocks = FindObjectsOfType<BlockController>();
            
            // Filtrar solo los bloques activos
            int activeBlocks = 0;
            foreach (BlockController block in blocks)
            {
                if (block.gameObject.activeInHierarchy && block != this)
                    activeBlocks++;
            }
            
            // Sumar 1 para incluir este bloque
            totalBlocksInitial = activeBlocks + 1;
            blocksDestroyed = 0;
            isInitialized = true;
            
            Debug.Log($"Nivel inicializado con {totalBlocksInitial} bloques en escena {currentLevelId}");
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
        blockLevel = Mathf.RoundToInt((transform.position.y - pivotOffset));

        position.x = Mathf.Round(position.x);
        position.z = Mathf.Round(position.z - 0.5f) + 0.5f;
        
        // Check if block is at the lowest level (y=0 + offset)
        isLowLevel = blockLevel == 0;
        
        // Set initial target position one level down
        targetPosition = new Vector3(
            transform.position.x,
            blockLevel > 0 ? (blockLevel - 1) + pivotOffset : pivotOffset,
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
                blockLevel = Mathf.RoundToInt((transform.position.y - pivotOffset));
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

        // Comprobar si todos los bloques han sido destruidos
        CheckAllBlocksDestroyed();
    }
   

    // Corrutina para el fadeout de las partículas
    private IEnumerator FadeOutAndDestroy(GameObject obj, float duration)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        Color startColor = renderer.material.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            
            // Reducir el tamaño gradualmente
            obj.transform.localScale *= (1f - Time.deltaTime * 0.5f);
            
            // Reducir la opacidad
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, normalizedTime);
            renderer.material.color = newColor;
            
            yield return null;
        }
        
        Destroy(obj);
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

                // El primer bloque desciende exactamente su coordenada Y para acabar en el nivel 0
                float descendUnits = blockAbove.transform.position.y - pivotOffset;

                Debug.Log($"Primer bloque en {blockAbove.transform.position} descenderá hasta el nivel 0 ({descendUnits} unidades)");
                blockAbove.DescendExactAmount(descendUnits);
            }
            else if (previousBlock != null)
            {
                // Para los siguientes bloques, calculamos la distancia respecto al bloque anterior
                distance = hit.distance - accumulatedDistance;
                accumulatedDistance = hit.distance;

                // Este bloque desciende lo mismo que el anterior + (distancia - 1 unidad)
                float additionalUnits = Mathf.Max(0f, Mathf.Floor(distance) - 1f);
                float descendUnits = previousBlock.GetDescendUnits() + additionalUnits;
                blockAbove.DescendExactAmount(descendUnits);
            }

            previousBlock = blockAbove;
        }
    }

    public void DescendExactAmount(float amount)
    {
        // Guardar cuántas unidades desciende este bloque (para la recursión)
        descendUnits = amount;
        
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
            descendUnits = (transform.position.y - pivotOffset);
        }
        
        Debug.Log($"Bloque en {transform.position} descenderá {descendUnits} unidades hasta {targetPosition}");
        shouldDescend = true;
    }
    
    private void SpawnRandomPowerUp()
    {
        // Verificar si ya alcanzamos el límite de power ups activos
        if (activePowerUps >= maxPowerUps)
        {
            Debug.Log($"Límite de power ups alcanzado ({activePowerUps}/{maxPowerUps}). No se generará nuevo power up.");
            return;
        }
        
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
            
            Debug.Log($"PowerUp generado: {prefabName}");
        }
        else
        {
            Debug.LogWarning("No se pudo cargar el prefab: Resources/Prefabs/" + prefabName);
        }
    }

    // Actualizar el método SelectPowerUpType para que NextLevel sea realmente raro
    private PowerUpType SelectPowerUpType()
    {
        // Verificar si se puede generar el powerup NextLevel - aumentar el umbral
        bool canGenerateNextLevel = GetDestroyedPercentage() > 95f;
        
        // Lista de tipos disponibles
        List<PowerUpType> availableTypes = new List<PowerUpType>();
        
        foreach (PowerUpType type in System.Enum.GetValues(typeof(PowerUpType)))
        {
            // Excluir NextLevel si no se cumple la condición
            if (type != PowerUpType.NextLevel)
            {
                availableTypes.Add(type);
            }
        }
        
        // Si está disponible NextLevel, añadirlo pero con muy baja probabilidad
        if (canGenerateNextLevel)
        {
            // Probabilidad baja: 1% de probabilidad de que sea NextLevel
            availableTypes.Add(PowerUpType.NextLevel);
        }
        
        // Seleccionar aleatoriamente entre los tipos normales
        int randomIndex = Random.Range(0, availableTypes.Count);
        return availableTypes[randomIndex];
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

    // Método explícito para reiniciar contadores (llamado desde GameManager)
    public static void ResetLevelCounters()
    {
        totalBlocksInitial = 0;
        blocksDestroyed = 0;
        isInitialized = false;
        activePowerUps = 0; // Resetear el contador de power ups
        Debug.Log("Contadores de bloques y power ups reiniciados explícitamente");
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

    // Mejorar el método CheckAllBlocksDestroyed para ser más robusto
    private static void CheckAllBlocksDestroyed()
    {
        // Verificar que hay bloques para contar
        if (totalBlocksInitial <= 0)
        {
            Debug.LogWarning("No hay bloques inicializados para comprobar");
            return;
        }
        
        // Contar los bloques actuales en la escena (solo los activos)
        BlockController[] blocks = FindObjectsOfType<BlockController>();
        int activeBlocks = 0;
        foreach (BlockController block in blocks)
        {
            if (block.gameObject.activeInHierarchy && !block.isBreaking)
                activeBlocks++;
        }
        
        Debug.Log($"CheckAllBlocksDestroyed: Bloques activos={activeBlocks}, Destruidos={blocksDestroyed}, Total={totalBlocksInitial}");
        
        // Si se han destruido todos los bloques (usando la cuenta actual de bloques activos)
        if (activeBlocks <= 0 || blocksDestroyed >= totalBlocksInitial)
        {
            Debug.Log($"¡TODOS LOS BLOQUES DESTRUIDOS! ({blocksDestroyed}/{totalBlocksInitial}) - Bloques restantes: {activeBlocks}");
            
            // COMPROBAR si ya estamos cambiando de nivel para evitar llamadas múltiples
            if (GameManager.Instance != null && !GameManager.Instance.IsChangingLevel())
            {
                Debug.Log("Llamando a Victory() después de verificar IsChangingLevel=false");
                GameManager.Instance.Invoke("Victory", 1.5f);
            }
            else
            {
                Debug.Log("NO se llama a Victory - Ya estamos cambiando de nivel o GameManager es null");
            }
        }
    }

    // Método para decrementar el contador de power ups (usado por PowerupDestroyTracker)
    public static bool DecrementActivePowerUps()
    {
        if (activePowerUps > 0)
        {
            activePowerUps--;
            return true;
        }
        return false;
    }

    // Método para obtener el número actual de power ups
    public static int GetActivePowerUps()
    {
        return activePowerUps;
    }

    // Método para incrementar el contador de power ups (usado por PowerupController)
    public static void IncrementActivePowerUps()
    {
        activePowerUps++;
        Debug.Log($"Power up registrado. Total activos: {activePowerUps}/{maxPowerUps}");
    }
}