using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Añadir esta línea

public class BallController : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private float initialSpeed = 5f;
    [SerializeField] private Vector3 initialDirection = new Vector3(0f, 0f, 1f).normalized;
    [SerializeField] public bool isMainBall = true;

    private Rigidbody rb;
    private bool gameStarted = false;
    private Vector3 startPosition;

    // Para el modo Power Ball
    private bool isPowerBall = false;


    private bool isAttachedToPaddle = false;
    private Transform attachedPaddle = null;
    private Vector3 attachOffset;
    private PalletController paddleController = null; // Referencia al controlador de la paleta

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        startPosition = transform.position;
        
        // Initialize ball at rest
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        // Solo adjuntar a la paleta si es la bola principal
        if (isMainBall)
        {
            // Find and attach to the paddle at the start of the level
            PalletController paddle = FindObjectOfType<PalletController>();
            if (paddle != null)
            {
                AttachToPaddle(paddle.transform, paddle);
            }
        }
        // Si es bola extra, asegurarse que no está cinemática
        else if (rb != null)
        {
            rb.isKinematic = false;
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

        // En lugar de verificar el tag, verificamos directamente si tiene el componente BlockController
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
        }
    }

    private IEnumerator FixBounceAngle(Vector3 incomingVelocity, Vector3 surfaceNormal)
    {
        // Wait one frame to let physics engine apply the bounce
        yield return null;
        
        // Get the current velocity after physics has applied the bounce
        Vector3 currentVelocity = rb.linearVelocity;

        // Ángulo mínimo permitido respecto a la normal (45°)
        float minBounceAngle = 45f;

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
        
        // Restaurar color original si es la bola principal
        if (isMainBall)
        {
            GetComponent<Renderer>().material.color = Color.white;
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
        
        Debug.Log("Bola lanzada con dirección: " + launchDirection);
    }

    public void ForceImmediateLaunch(Vector2 direction)
    {
        // Asegurarse de que no sea cinemático cuando se lanza
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        // CÓDIGO IMPORTANTE: Cancelar cualquier corrutina que pueda interferir
        StopAllCoroutines();
        
        // Asegurar que NO es cinemático
        rb.isKinematic = false;
        
        // Asegurar que la bola no esté adherida a la paleta
        isAttachedToPaddle = false;
        attachedPaddle = null;
        paddleController = null;
        
        // Marcar como juego iniciado
        gameStarted = true;
        
        // IMPORTANTE: Asegurar que la bola no tiene gravedad
        rb.useGravity = false;
        
        // Normalizar la dirección y aplicar la velocidad inicial inmediatamente
        Vector3 launchDirection = new Vector3(direction.x, 0, direction.y).normalized;
        
        // Reiniciar completamente la velocidad
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Asignar la nueva velocidad
        rb.linearVelocity = launchDirection * initialSpeed;
        
        Debug.Log("Bola lanzada forzosamente con dirección: " + launchDirection);
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

        // Cambiar aspecto visual de la bola para indicar el modo
        GetComponent<Renderer>().material.color = isPowerBall ? Color.red : Color.white;
        Debug.Log("Modo PowerBall: " + (enabled ? "ACTIVADO" : "DESACTIVADO"));
    }

    // Añade este método a BallController.cs si no existe
    public void SetAsExtraBall()
    {
        // Marcar como bola secundaria
        isMainBall = false;
        
        // No necesita estar adherida a la paleta al inicio
        gameStarted = true;
        
        // Cambiar color para distinguirla
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.8f, 0.2f); // Color dorado/amarillo
        }
        
        Debug.Log("Bola configurada como bola extra");
    }
}