using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Añadir esta línea

public class BallController : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private float initialSpeed = 5f;
    [SerializeField] private Vector3 initialDirection = new Vector3(0f, 0f, 1f).normalized;

    [Header("Speed Effects")]
    private float defaultSpeed;
    private float currentSpeedMultiplier = 1.0f;
    private Coroutine speedChangeCoroutine = null;

    [Header("Trail Effect")]
    [SerializeField] private TrailRenderer trailRenderer;
    private Color normalTrailColor = new Color(0.5f, 0.5f, 1f, 0.5f);
    private Color powerBallTrailColor = new Color(1f, 0.3f, 0.3f, 0.7f);

    [Header("Audio")]
    private AudioSource audioSource;
    [SerializeField] private AudioClip hitBlockSound;
    [SerializeField] private AudioClip hitPaddleSound;
    [SerializeField] private AudioClip hitWallSound;
    [SerializeField] private AudioClip breakBallSound; // Nuevo sonido para romper la bola

    private Rigidbody rb;
    private bool gameStarted = false;
    private Vector3 startPosition;

    // Para el modo Power Ball
    private bool isPowerBall = false;


    public bool isAttachedToPaddle = false;
    private Transform attachedPaddle = null;
    private Vector3 attachOffset;
    private PalletController paddleController = null; // Referencia al controlador de la paleta

    private float lastBlockSoundTime = 0f;
    private const float MIN_BLOCK_SOUND_INTERVAL = 0.25f; // Tiempo mínimo entre sonidos de bloque

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        defaultSpeed = initialSpeed;

        // Configurar la capa de la bola para evitar colisiones entre bolas
        gameObject.layer = LayerMask.NameToLayer("Ball");
    
        // Comprobar si hay un TrailRenderer o crearlo si no existe
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
                ConfigureTrailRenderer(trailRenderer);
            }
        }
        
        // Desactivar la estela al inicio
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }

        // Configure audio source if needed
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Load audio clips if not assigned in inspector
        if (hitBlockSound == null)
            hitBlockSound = Resources.Load<AudioClip>("Audio/hit-bloque");
        if (hitPaddleSound == null)
            hitPaddleSound = Resources.Load<AudioClip>("Audio/rebote");
        if (hitWallSound == null)
            hitWallSound = Resources.Load<AudioClip>("Audio/hit-pared");
        if (breakBallSound == null)
            breakBallSound = Resources.Load<AudioClip>("Audio/romper-bola"); // Cargar sonido de romper bola
    }

    private void Start()
    {
        startPosition = new Vector3(-9.3f, 0, 0.5f); // Posición inicial de la bola
  
        // Initialize ball at rest
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        // Verificar si esta bola es nueva (creada durante el juego) o es la bola inicial
        BallController[] allBalls = FindObjectsOfType<BallController>();
        bool isInitialBall = allBalls.Length <= 1 || Time.timeSinceLevelLoad < 1.0f;
        
        // Solo adherir a la paleta si es la bola inicial del nivel
        if (isInitialBall)
        {
            PalletController paddle = FindObjectOfType<PalletController>();
            if (paddle != null)
            {
                AttachToPaddle(paddle.transform, paddle);
            }
        }
        else
        {
            // Si es una bola adicional, comenzar en movimiento
            isAttachedToPaddle = false;
            gameStarted = true;
            
            // Asegurarse de que no sea cinemática
            if (rb != null)
            {
                rb.isKinematic = false;
                
                // Activar efecto de trail
                UpdateTrailEffect();
            }
        }
    }

    private void Update()
    {
        // Press Space to launch the ball using the new Input System
        if (!gameStarted && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            LaunchBall(initialDirection);
        }

        // Ensure ball stays at the correct velocity
        if (gameStarted)
        {
            MaintainSpeed();
        }

        // Comprobar si la bola está adherida y se pulsa espacio
        if (isAttachedToPaddle && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            DetachFromPaddle();
        }
    }

    private void MaintainSpeed()
    {
        // No modificar la velocidad si el cuerpo es cinemático
        if (rb.isKinematic)
            return;
            
        Vector3 velocity = rb.linearVelocity;
        if (velocity.magnitude != initialSpeed)
        {
            velocity = velocity.normalized * initialSpeed;
            rb.linearVelocity = velocity;
        }
        
        // Asegurar componentes no cero
        EnsureNonZeroVelocityComponents();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 incomingVelocity = rb.linearVelocity;

        // Check for collision with block
        BlockController block = collision.gameObject.GetComponent<BlockController>();
        if (block != null)
        {
            // Si es PowerBall, no rebota en los bloques y los atraviesa
            if (isPowerBall)
            {
                rb.linearVelocity = incomingVelocity;
                return;
            }
            
            // Damage the block
            block.Hit();
            
            // Play block hit sound
            PlaySound(hitBlockSound);
        }
        // Check for collision with paddle
        else if (collision.gameObject.GetComponent<PalletController>() != null)
        {
            PlaySound(hitPaddleSound);
        }
        // Check for collision with lower limit
        else if (collision.gameObject.GetComponent<LimitController>() != null)
        {
            PlaySound(breakBallSound);
        }
        // For all other collisions (walls, etc.)
        else
        {
            PlaySound(hitWallSound);
        }

        StartCoroutine(FixBounceAngle(incomingVelocity, collision.contacts[0].normal));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPowerBall) return;

        BlockController block = other.GetComponent<BlockController>();
        if (block != null)
        {
            block.Hit();
            PlaySound(hitBlockSound);
        }
    }

    private IEnumerator FixBounceAngle(Vector3 incomingVelocity, Vector3 surfaceNormal)
    {
        // Wait one frame to let physics engine apply the bounce
        yield return null;
        
        // Get the current velocity after physics has applied the bounce
        Vector3 currentVelocity = rb.linearVelocity;

        // Ángulo mínimo permitido respecto a la normal (30°)
        float minBounceAngle = 30f;

        // Ángulo real entre la velocidad y la normal
        float angleWithNormal = Vector3.Angle(currentVelocity, surfaceNormal);

        // Si el ángulo es menor al mínimo, corrige la dirección
        if (angleWithNormal < minBounceAngle)
        {
            // Calcula la proyección de la velocidad sobre el plano perpendicular a la normal
            Vector3 tangent = Vector3.ProjectOnPlane(currentVelocity, surfaceNormal).normalized;

            // Calcula la nueva dirección con el ángulo mínimo respecto a la normal
            Quaternion rotation = Quaternion.AngleAxis(minBounceAngle, Vector3.Cross(surfaceNormal, tangent));
            Vector3 correctedDirection = rotation * surfaceNormal;

            // Decide si usar + o - según el lado al que iba la bola
            if (Vector3.Dot(correctedDirection, currentVelocity) < 0)
                correctedDirection = -correctedDirection;

            // Aplica la velocidad corregida
            rb.linearVelocity = correctedDirection.normalized * initialSpeed;

            Debug.Log("Bounce angle too shallow, fixed to minimum: " + angleWithNormal);
        }
        else
        {
            // Check if we have a "weird bounce" - velocity nearly reversed
            float bounceAngle = Vector3.Angle(currentVelocity, incomingVelocity);

            // Si el ángulo de rebote es mayor a 150 grados (es decir, menos de 30° respecto a la dirección opuesta), corrige
            if (bounceAngle > 150f)
            {
                // Calculate proper reflection vector
                Vector3 properReflection = Vector3.Reflect(incomingVelocity, surfaceNormal);

                // Add a small random variation to prevent repetitive patterns
                Vector3 randomOffset = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
                properReflection += randomOffset;

                // Apply the corrected velocity
                rb.linearVelocity = properReflection.normalized * initialSpeed;

                Debug.Log("Fixed unusual bounce: " + bounceAngle);
            }
        }

        // Now ensure non-zero components
        EnsureNonZeroVelocityComponents();
    }
    
    private void EnsureNonZeroVelocityComponents()
    {
        
        // Get current velocity
        Vector3 velocity = rb.linearVelocity;
        
        // NUEVO: Solo aplicar este ajuste después de un tiempo o en rebotes
        if (Time.timeSinceLevelLoad < 1.0f && gameStarted)
            return; // Permitir que la bola salga recta al inicio
    
        // Reducir el valor mínimo para que afecte menos a la dirección
        float minComponentValue = initialSpeed * 0.1f; // Usar 0.05f en vez de 0.1f
        
        if (Mathf.Abs(velocity.x) < minComponentValue)
        {
            velocity.x = minComponentValue * Mathf.Sign(velocity.x == 0 ? Random.Range(-1f, 1f) : velocity.x);
        }
        
        if (Mathf.Abs(velocity.z) < minComponentValue)
        {
            velocity.z = minComponentValue * Mathf.Sign(velocity.z == 0 ? Random.Range(-1f, 1f) : velocity.z);
        }
        
        rb.linearVelocity = velocity.normalized * initialSpeed;
    }
    
    // Reset the ball to its initial position and state
    public void ResetBall()
    {
        // Restablecer estado
        transform.position = startPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        gameStarted = false;
        initialSpeed = 5f; // Reset to initial speed
        
        // Detener todas las corrutinas para asegurar estado limpio
        StopAllCoroutines();
        
        // Encontrar y adjuntar a la paleta
        PalletController paddle = FindObjectOfType<PalletController>();
        if (paddle != null)
        {
            AttachToPaddle(paddle.transform, paddle);
        }
        
        // Si es PowerBall, desactivar ese modo
        if (isPowerBall)
        {
            SetPowerBallMode(false);
        }
    }

    public void LaunchBall(Vector3 direction)
    {   
        // Para otras direcciones, mantener el comportamiento existente
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("No se encontró Rigidbody en la bola");
                return;
            }
        }
        
        rb.isKinematic = false;
        isAttachedToPaddle = false;
        gameStarted = true;
        
        Vector3 launchDirection = direction.normalized;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        // Usar velocity directo en vez de AddForce para mayor consistencia
        rb.linearVelocity = launchDirection * initialSpeed;
        UpdateTrailEffect();
        Debug.Log("Bola lanzada con dirección: " + launchDirection);
    }

    public void AttachToPaddle(Transform paddle, PalletController controller = null)
    {
        // Si ya está pegada, no hacer nada
        if (isAttachedToPaddle)
            return;
        
        isAttachedToPaddle = true;
        attachedPaddle = paddle;
        paddleController = controller;
        
        // Calcular y almacenar el offset entre la bola y la paleta
        attachOffset = transform.position - paddle.position;
        
        // Detener la bola - importante: primero cambiar velocidad a cero, luego hacer isKinematic=true
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        
        Debug.Log("Bola adherida a la paleta");
        
        // Iniciar corrutina de seguimiento
        StartCoroutine(FollowPaddle());
    }

    private IEnumerator FollowPaddle()
    {
        while (isAttachedToPaddle && attachedPaddle != null)
        {
            // Actualizar posición para seguir a la paleta
            transform.position = attachedPaddle.position + attachOffset;
            
            // Comprobar si se presiona espacio para liberar la bola
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("Tecla espacio presionada, liberando bola");
                DetachFromPaddle();
            }
            
            yield return null;
        }
    }

    public void DetachFromPaddle()
    {
        if (!isAttachedToPaddle)
            return;
        
        // Marcar como no adherida antes de lanzar
        isAttachedToPaddle = false;
        
        // Si hay un controlador de paleta, marcar el efecto magnético como usado
        if (paddleController != null)
        {
            paddleController.MarkMagnetAsUsed();
            paddleController = null;
        }
        
        LaunchBall(initialDirection);
    }

    public void SetPowerBallMode(bool enabled)
    {
        isPowerBall = enabled;

        BlockController[] blocks = FindObjectsOfType<BlockController>();
        foreach (var block in blocks)
        {
            Collider[] blockColliders = block.GetComponents<Collider>();
            foreach (var blockCol in blockColliders)
            {
                blockCol.isTrigger = enabled; // Cambia a trigger solo en PowerBall
            }
        }

        UpdateTrailEffect();
        Debug.Log("Modo PowerBall: " + (enabled ? "ACTIVADO" : "DESACTIVADO"));
    }

    private void ConfigureTrailRenderer(TrailRenderer trail)
    {
        trail.time = 0.5f;               // Duración de la estela
        trail.minVertexDistance = 0.1f;  // Distancia mínima entre vértices
        trail.startWidth = 0.3f;         // Ancho al inicio de la estela
        trail.endWidth = 0.0f;           // Ancho al final de la estela
        trail.startColor = normalTrailColor;
        trail.endColor = new Color(normalTrailColor.r, normalTrailColor.g, normalTrailColor.b, 0f);
        
        // Crear un material para la estela si no lo tiene
        if (trail.material == null)
        {
            Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trail.material = trailMaterial;
        }
    }

    // Añadir este método para cambiar la velocidad de la bola
    public void ChangeSpeed(float speedMultiplier, float duration)
    {
        // Cancelar cualquier cambio de velocidad en curso
        if (speedChangeCoroutine != null)
        {
            StopCoroutine(speedChangeCoroutine);
        }
        
        // Iniciar el nuevo cambio de velocidad
        speedChangeCoroutine = StartCoroutine(SpeedChangeCoroutine(speedMultiplier, duration));
    }

    // Corrutina para cambiar la velocidad durante un tiempo
    private IEnumerator SpeedChangeCoroutine(float speedMultiplier, float duration)
    {
        // Guardar el multiplicador actual para restaurarlo después
        float previousMultiplier = currentSpeedMultiplier;
        
        // Aplicar el nuevo multiplicador
        currentSpeedMultiplier = speedMultiplier;
        initialSpeed = defaultSpeed * currentSpeedMultiplier;
        
        // Si la bola ya está en movimiento, actualizar su velocidad actual
        if (gameStarted && !rb.isKinematic)
        {
            Vector3 currentDir = rb.linearVelocity.normalized;
            rb.linearVelocity = currentDir * initialSpeed;
        }
        
        // Cambiar el color/tamaño de la estela según la velocidad
        UpdateTrailEffect();
        
        // Esperar la duración especificada
        yield return new WaitForSeconds(duration);
        
        // Restaurar la velocidad normal
        currentSpeedMultiplier = 1.0f;
        initialSpeed = defaultSpeed;
        
        // Actualizar velocidad de la bola
        if (gameStarted && !rb.isKinematic)
        {
            Vector3 currentDir = rb.linearVelocity.normalized;
            rb.linearVelocity = currentDir * initialSpeed;
        }
        
        // Restaurar efecto de estela
        UpdateTrailEffect();
        
        speedChangeCoroutine = null;
    }

    // Actualizar el efecto de estela según el estado actual
    private void UpdateTrailEffect()
    {
        if (trailRenderer == null) return;
        
        // Activar la estela SOLO si es PowerBall, desactivarla en cualquier otro caso
        trailRenderer.enabled = isPowerBall;
        
        if (isPowerBall)
        {
            // Colores de fuego: rojo intenso a naranja/amarillo
            Color fireStartColor = new Color(1f, 0.3f, 0.1f, 0.8f); // Rojo intenso
            Color fireEndColor = new Color(1f, 0.6f, 0.0f, 0.4f);   // Naranja/amarillo
            
            trailRenderer.startColor = fireStartColor;
            trailRenderer.endColor = fireEndColor;
            
            // Aumentar tamaño y longitud para efecto más impactante
            trailRenderer.time = 0.8f;        // Estela más larga
            trailRenderer.startWidth = 0.5f;  // Más ancha
            trailRenderer.endWidth = 0.05f;   // Termina en punto más visible
            
            // Opcional: aumentar la emisión de luz
            if (trailRenderer.material != null)
            {
                trailRenderer.material.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f, 1f));
                trailRenderer.material.EnableKeyword("_EMISSION");
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            // Control de volumen según el tipo de sonido
            float volume = 1.0f;
            
            if (clip == hitWallSound)
            {
                volume = 0.3f;
            }
            else if (clip == hitPaddleSound)
            {
                volume = 0.3f;
            }
            else if (clip == hitBlockSound)
            {
                volume = 0.3f;
            }
            else if (clip == breakBallSound) // Control de volumen para romper bola
            {
                volume = 0.5f;
            }
            
            // Use AudioManager to play the sound instead of playing directly
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(clip, transform.position, volume);
            }
            else
            {
                // Fallback if AudioManager is not available
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(clip, volume);
                }
            }
        }
    }
}