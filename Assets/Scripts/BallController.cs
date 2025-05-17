using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Añadir esta línea

public class BallController : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private float initialSpeed = 10f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private Vector3 initialDirection = new Vector3(1f, 0f, 0.5f).normalized;
    [SerializeField] private float hitForce = 1.1f; // Optional: increase ball speed slightly on each hit

    private Rigidbody rb;
    private bool gameStarted = false;
    private Vector3 startPosition;

    // Para el modo Power Ball
    private bool isPowerBall = false;
    private float powerBallTimer = 0f;

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

        // Find and attach to the paddle at the start of the level
        PalletController paddle = FindObjectOfType<PalletController>();
        if (paddle != null)
        {
            AttachToPaddle(paddle.transform, paddle);
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
            
        // Resto de la lógica existente para mantener la velocidad...
        Vector3 velocity = rb.linearVelocity;
        float currentSpeed = velocity.magnitude;
        
        // Ajustar velocidad si es necesario
        if (Mathf.Abs(currentSpeed - initialSpeed) > 0.5f)
        {
            velocity = velocity.normalized * initialSpeed;
            rb.linearVelocity = velocity;
        }
        
        // Limitar la velocidad máxima
        if (currentSpeed > maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
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
            // Si es PowerBall, no rebota en los bloques
            if (isPowerBall)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), collision.collider);
            }
            
            // Damage the block regardless
            block.Hit();
        }

        StartCoroutine(FixBounceAngle(incomingVelocity, collision.contacts[0].normal));
    }

    private IEnumerator FixBounceAngle(Vector3 incomingVelocity, Vector3 surfaceNormal)
    {
        // Wait one frame to let physics engine apply the bounce
        yield return null;
        
        // Get the current velocity after physics has applied the bounce
        Vector3 currentVelocity = rb.linearVelocity;
        
        // Check if we have a "weird bounce" - velocity nearly reversed
        float bounceAngle = Vector3.Angle(currentVelocity, incomingVelocity);
        
        // If the bounce angle is too close to 180 degrees (complete reversal), fix it
        if (bounceAngle > 160f)
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
        
        // Now ensure non-zero components
        EnsureNonZeroVelocityComponents();
    }
    
    private void EnsureNonZeroVelocityComponents()
    {
        // Get current velocity
        Vector3 velocity = rb.linearVelocity;
        
        // Make sure x and z components aren't too close to zero
        // This prevents the ball from moving in a perfectly horizontal or vertical line
        float minComponentValue = initialSpeed * 0.1f;
        
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
        transform.position = startPosition;
        rb.linearVelocity = Vector3.zero;
        gameStarted = false;
        initialSpeed = 10f; // Reset to initial speed
        
        // Find and attach to the paddle at the start of the level
        PalletController paddle = FindObjectOfType<PalletController>();
        if (paddle != null)
        {
            AttachToPaddle(paddle.transform, paddle);
        }
    }

    // Método para lanzar la bola en una dirección específica
    public void LaunchBall(Vector2 direction)
    {
        gameStarted = true;
        Vector3 launchDirection = new Vector3(direction.x, 0, direction.y).normalized;
        rb.linearVelocity = launchDirection * initialSpeed;
    }

    // Restaura todas las colisiones ignoradas
    private void RestoreAllCollisions()
    {
        // En lugar de buscar por tag, encontramos todos los BlockController
        BlockController[] blocks = FindObjectsOfType<BlockController>();
        Collider ballCollider = GetComponent<Collider>();
        
        foreach (BlockController block in blocks)
        {
            Collider blockCollider = block.GetComponent<Collider>();
            if (blockCollider != null)
            {
                Physics.IgnoreCollision(ballCollider, blockCollider, false);
            }
        }
        
        Debug.Log($"Restauradas colisiones con {blocks.Length} bloques");
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
        
        isAttachedToPaddle = false;
    
        // IMPORTANTE: primero volver rb a no kinematic
        rb.isKinematic = false;
        
        // Ahora sí podemos establecer la velocidad
        LaunchBall(initialDirection);
        gameStarted = true;
        
        // Si hay un controlador de paleta, marcar el efecto magnético como usado
        if (paddleController != null)
        {
            paddleController.MarkMagnetAsUsed();
            paddleController = null;
        }
        
        Debug.Log("Bola liberada de la paleta");
    }
    // Añade este método para configurar IgnoreCollision con todos los bloques
private void SetIgnoreCollisionsWithBlocks(bool ignore)
{
    BlockController[] blocks = FindObjectsOfType<BlockController>();
    Collider ballCollider = GetComponent<Collider>();
    
    foreach (BlockController block in blocks)
    {
        Collider blockCollider = block.GetComponent<Collider>();
        if (blockCollider != null)
        {
            Physics.IgnoreCollision(ballCollider, blockCollider, ignore);
        }
    }
    
    Debug.Log($"Colisiones con bloques {(ignore ? "desactivadas" : "activadas")}");
}

// Modifica el método SetPowerBallMode
public void SetPowerBallMode(bool enabled)
{
    isPowerBall = enabled;
    
    // Al activar PowerBall, ignoramos TODAS las colisiones con bloques
    if (enabled)
    {
        SetIgnoreCollisionsWithBlocks(true);
    }
    else
    {
        SetIgnoreCollisionsWithBlocks(false);
    }
    
  
    
    // Cambiar aspecto visual de la bola para indicar el modo
    GetComponent<Renderer>().material.color = isPowerBall ? Color.red : Color.white;
    
    Debug.Log("Modo PowerBall: " + (enabled ? "ACTIVADO" : "DESACTIVADO"));
}

// Añade una corrutina que detecta bloques en el camino de la bola
private void FixedUpdate()
{
    // Si estamos en modo PowerBall, detectamos bloques en el camino
    if (isPowerBall && !rb.isKinematic && gameStarted)
    {
        CheckBlocksInPath();
    }
}

private void CheckBlocksInPath()
{
    // Lanzar un raycast en la dirección del movimiento
    Ray ray = new Ray(transform.position, rb.linearVelocity.normalized);
    RaycastHit hit;
    float rayDistance = rb.linearVelocity.magnitude * Time.fixedDeltaTime * 2; // Mirar un poco más adelante
    
    // Debug.DrawRay(transform.position, rb.velocity.normalized * rayDistance, Color.yellow);
    
    // Si detectamos un bloque en el camino
    if (Physics.Raycast(ray, out hit, rayDistance))
    {
        BlockController block = hit.collider.GetComponent<BlockController>();
        if (block != null)
        {
            // Dañar el bloque sin rebotar
            block.Hit();
            Debug.Log("PowerBall golpeó un bloque sin rebote");
        }
    }
}


}