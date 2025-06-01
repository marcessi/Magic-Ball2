using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PalletController : MonoBehaviour
{
    private float velocidad = 15f;
    private float limiteIzquierdo = -6f;
    private float limiteDerecho = 7f;
    
    // Limites del mapa definidos por las paredes
    private const float WALL_LEFT = -7f;  // Posición Z de la pared izquierda
    private const float WALL_RIGHT = 8f;  // Posición Z de la pared derecha
    private const float WALL_PADDING = 0.5f; // Espacio entre la paleta y la pared

    [Header("Scale Settings")]
    private float maxScaleFactor = 1.75f; // Escala máxima respecto a la original
    private float minScaleFactor = 0.75f; // Escala mínima respecto a la original
    
    [Header("Shooting Mode")]
    [SerializeField] private Transform leftShootPoint;
    [SerializeField] private Transform rightShootPoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootInterval = 0.5f;
    
    private Vector2 movimientoInput;
    private Vector3 initialPosition;
    private Vector3 originalScale;
    private bool magnetModeActive = false;
    private bool magnetEffectUsed = false;
    private bool shootModeActive = false;
    private Coroutine shootingCoroutine = null;
    
    private void Start()
    {
        // Store initial position and scale
        initialPosition = transform.position;
        originalScale = transform.localScale;
        
        // Calcular los límites iniciales basados en el tamaño de la paleta
        CalculateMovementLimits();
    }
    
    // Nueva función para calcular los límites de movimiento
    private void CalculateMovementLimits()
    {
        // Calcular el tamaño medio de la paleta en el eje Z
        float paddleHalfWidth = transform.localScale.z * 0.5f;
        
        // Límites calculados para que la paleta no atraviese las paredes
        limiteIzquierdo = WALL_LEFT + paddleHalfWidth + WALL_PADDING;
        limiteDerecho = WALL_RIGHT - paddleHalfWidth - WALL_PADDING;
        
        Debug.Log($"Límites de movimiento calculados: Izquierdo={limiteIzquierdo:F2}, Derecho={limiteDerecho:F2}, Tamaño paleta={transform.localScale.z:F2}");
    }
    
    private void Update()
    {
        // Invertir la entrada para corregir el problema: A -> izquierda, D -> derecha
        float movimientoHorizontal = -movimientoInput.x;
        
        // Mover la paleta en el eje Z mundial usando entrada horizontal invertida
        transform.Translate(Vector3.forward * movimientoHorizontal * velocidad * Time.deltaTime);
        
        // Aplicar límites en coordenadas mundiales
        Vector3 posicionActual = transform.position;
        
        // Aplicar límite a la coordenada Z en el espacio mundial
        posicionActual.z = Mathf.Clamp(posicionActual.z, limiteIzquierdo, limiteDerecho);
        
        transform.position = posicionActual;
    }
    
    // Este método será llamado por el Input System cuando ocurra un movimiento
    public void OnMove(InputValue value)
    {
        movimientoInput = value.Get<Vector2>();
    }
    
    // Method to reset the paddle position and scale
    public void ResetPosition()
    {
        transform.position = initialPosition;
        transform.localScale = originalScale;
        
        // Recalcular los límites para la escala original
        CalculateMovementLimits();
    }
    
    // Método para expandir la paleta gradualmente
    public void ExpandPaddle(float expandFactor)
    {
        // Store original scale if not stored yet
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        // Calcular incremento absoluto en lugar de factor multiplicativo
        float incrementAmount = 0.2f; // Valor fijo de incremento en unidades
        
        // Calcular escala objetivo con el incremento
        Vector3 targetScale = transform.localScale;
        targetScale.z += incrementAmount; // Aumentar tamaño en el eje Z
        
        // Limitar al tamaño máximo (basado en la escala original)
        float maxScale = originalScale.z * maxScaleFactor;
        if (targetScale.z > maxScale)
        {
            targetScale.z = maxScale;
        }
        
        // Expand the paddle gradually
        StartCoroutine(GradualScaleChange(targetScale));
    }
    
    // Método para reducir la paleta gradualmente
    public void ShrinkPaddle(float shrinkFactor)
    {
        // Store original scale if not stored yet
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        // Calcular decremento absoluto en lugar de factor multiplicativo
        float decrementAmount = 0.2f; // Valor fijo de decremento en unidades
        
        // Calcular escala objetivo con el decremento
        Vector3 targetScale = transform.localScale;
        targetScale.z -= decrementAmount; // Reducir tamaño en el eje Z
        
        // Limitar al tamaño mínimo (basado en la escala original)
        float minScale = originalScale.z * minScaleFactor;
        if (targetScale.z < minScale)
        {
            targetScale.z = minScale;
        }
        
        // Shrink the paddle gradually
        StartCoroutine(GradualScaleChange(targetScale));
    }
    
    private IEnumerator GradualScaleChange(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        float duration = 1.5f; // Duración más larga para que sea más gradual
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Función de suavizado para hacer la transición más natural
            t = Mathf.SmoothStep(0, 1, t);
            
            // Interpolar entre la escala actual y la objetivo
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            // Recalcular los límites en cada frame durante el cambio de escala
            CalculateMovementLimits();
            
            yield return null;
        }
        
        // Asegurar que llegamos exactamente al valor deseado
        transform.localScale = targetScale;
        
        // Asegurar que los límites finales sean correctos
        CalculateMovementLimits();
        
        Debug.Log($"Escala de la paleta ajustada. Factor actual: {transform.localScale.z / originalScale.z:F2}x");
    }

    // Activa el modo imán
    public void ActivateMagnetMode(float duration)
    {
        magnetModeActive = true;
        magnetEffectUsed = false; // Resetear el estado de uso
        
        // Si se especifica una duración, desactivar después de ese tiempo
        if (duration > 0)
        {
            StartCoroutine(DeactivateMagnetAfterDelay(duration));
        }
        
        Debug.Log("Modo imán activado en la paleta");
    }
    
    private IEnumerator DeactivateMagnetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DeactivateMagnetMode();
    }
    
    public void DeactivateMagnetMode()
    {
        magnetModeActive = false;
        magnetEffectUsed = false;
        
        Debug.Log("Modo imán desactivado en la paleta");
    }
    
    // Marca el efecto magnético como usado (llama a este método cuando la bola se suelte)
    public void MarkMagnetAsUsed()
    {
        magnetEffectUsed = true;
        magnetModeActive = false; // Desactivar completamente el modo imán
        
        Debug.Log("Efecto imán usado y desactivado");
    }
    
    // Comprueba colisiones con la bola para aplicar el efecto imán
    private void OnCollisionEnter(Collision collision)
    {
        // Verificar si es la bola
        if (collision.gameObject.CompareTag("Ball"))
        {
            BallController ball = collision.gameObject.GetComponent<BallController>();
            if (ball == null || ball.isAttachedToPaddle) return;
            
            if (magnetModeActive && !magnetEffectUsed && !ball.isAttachedToPaddle)
                {
                    // Calcular posición de enganche (en la parte superior de la paleta)
                    Vector3 attachPosition = transform.position + new Vector3(0, 0.5f, 0);
                    
                    // Llamar al método AttachToPaddle de la bola
                    ball.AttachToPaddle(this.transform, this);
                    
                    // Marcar el efecto como usado
                    magnetEffectUsed = true;
                    
                    Debug.Log("¡Bola pegada a la paleta por efecto imán!");
                    return; // Salir para evitar el resto de la lógica de colisión
                }
            // Obtener el punto de contacto
            ContactPoint contact = collision.contacts[0];
            
            // Calcular la posición relativa del impacto en la paleta (entre -1 y 1)
            Vector3 paddleLocalPoint = transform.InverseTransformPoint(contact.point);
            
            // Usar paddleLocalPoint.z para una paleta que se mueve en el eje Z
            float relativePosition = paddleLocalPoint.z / (transform.localScale.z * 0.5f);
            
            // Clamp entre -1 y 1 por seguridad
            relativePosition = Mathf.Clamp(relativePosition, -1f, 1f);
            
            // Obtener el rigidbody y la velocidad actual de la bola
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            Vector3 currentVelocity = ballRb.linearVelocity;
            
            // Asegurar que la bola tenga una velocidad mínima para evitar que se atasque
            float minVelocityMagnitude = 10f; // Ajustar según sea necesario

            if (relativePosition < -0.3f) // Golpeó el lado izquierdo
            {
                // Dirigir hacia la izquierda (z negativo) con componente Y siempre positiva
                Vector3 newDirection = new Vector3(currentVelocity.x, Mathf.Abs(currentVelocity.y), -Mathf.Abs(currentVelocity.z));
                Debug.Log("Rebote hacia IZQUIERDA. Posición relativa: " + relativePosition);
                float finalSpeed = Mathf.Max(currentVelocity.magnitude, minVelocityMagnitude);
                ballRb.linearVelocity = newDirection.normalized * finalSpeed;
            }
            else if (relativePosition > 0.3f) // Golpeó el lado derecho
            {
                // Dirigir hacia la derecha (z positivo) con componente Y siempre positiva
                Vector3 newDirection = new Vector3(currentVelocity.x, Mathf.Abs(currentVelocity.y), Mathf.Abs(currentVelocity.z));
                Debug.Log("Rebote hacia DERECHA. Posición relativa: " + relativePosition);
                float finalSpeed = Mathf.Max(currentVelocity.magnitude, minVelocityMagnitude);
                ballRb.linearVelocity = newDirection.normalized * finalSpeed;
            }
        }
    }

    // Añadir este método a PalletController.cs
    public void ResetToOriginalScale()
    {
        // Restaurar escala original inmediatamente, sin animación
        transform.localScale = originalScale;
        
        // Recalcular los límites para la escala original
        CalculateMovementLimits();
        
        Debug.Log("Paleta restaurada a su escala original y límites de movimiento ajustados");
    }

    // Método para activar el modo de disparo
    public void ActivateShootMode(float duration)
    {
        // Activar modo de disparo
        shootModeActive = true;

        // Iniciar la corrutina de disparo
        if (shootingCoroutine != null)
        {
            StopCoroutine(shootingCoroutine);
        }
        shootingCoroutine = StartCoroutine(ShootBullets());
        
        // Si se especifica una duración, desactivar después de ese tiempo
        if (duration > 0)
        {
            StartCoroutine(DeactivateShootModeAfterDelay(duration));
        }
        
        Debug.Log("Modo disparo activado en la paleta");
    }

    // Corrutina para disparar balas
    private System.Collections.IEnumerator ShootBullets()
    {
        Debug.Log("Corrutina de disparo iniciada");
        
        // Crear puntos de disparo si no existen
        if (leftShootPoint == null)
        {
            Debug.Log("Creando punto de disparo izquierdo");
            leftShootPoint = new GameObject("LeftShootPoint").transform;
            leftShootPoint.SetParent(transform);
            // Posición ajustada para una pala que se mueve en el eje Z
            leftShootPoint.localPosition = new Vector3(-0.4f, 0.3f, transform.localScale.z/2 - 0.2f);
            leftShootPoint.localRotation = Quaternion.Euler(90, 0, 0); // Rotación para que dispare hacia arriba (Y+)
        }
        
        if (rightShootPoint == null)
        {
            Debug.Log("Creando punto de disparo derecho");
            rightShootPoint = new GameObject("RightShootPoint").transform;
            rightShootPoint.SetParent(transform);
            rightShootPoint.localPosition = new Vector3(0.4f, 0.3f, transform.localScale.z/2 - 0.2f);
            rightShootPoint.localRotation = Quaternion.Euler(90, 0, 0); // Rotación para que dispare hacia arriba (Y+)
        }
        
        // Mientras el modo esté activo, disparar a intervalos
        while (shootModeActive)
        {
            // Verificar que tenemos el prefab y los puntos de disparo
            if (bulletPrefab != null && leftShootPoint != null && rightShootPoint != null)
            {
                Debug.Log("Disparando balas");
                
                // Disparo izquierdo
                GameObject leftBullet = Instantiate(
                    bulletPrefab, 
                    leftShootPoint.position, 
                    Quaternion.identity // Usar identidad para que no haya rotación inicial
                );
                
                // Disparo derecho
                GameObject rightBullet = Instantiate(
                    bulletPrefab, 
                    rightShootPoint.position, 
                    Quaternion.identity // Usar identidad para que no haya rotación inicial
                );
                
                // Verificar que se crearon las balas
                if (leftBullet == null || rightBullet == null)
                    Debug.LogError("Error al crear las balas");
                else
                    Debug.Log("Balas creadas correctamente");
                
                // Añadir velocidad a las balas si no tienen el controlador
                if (leftBullet.GetComponent<BulletController>() == null)
                {
                    Rigidbody rb = leftBullet.GetComponent<Rigidbody>();
                    if (rb != null) rb.linearVelocity = Vector3.up * 10f;
                }
                
                if (rightBullet.GetComponent<BulletController>() == null)
                {
                    Rigidbody rb = rightBullet.GetComponent<Rigidbody>();
                    if (rb != null) rb.linearVelocity = Vector3.up * 10f;
                }
            }
            else
            {
                Debug.LogError("Falta prefab de bala o puntos de disparo: " +
                          "bulletPrefab=" + (bulletPrefab != null) +
                          ", leftShootPoint=" + (leftShootPoint != null) +
                          ", rightShootPoint=" + (rightShootPoint != null));
            }
            
            // Esperar el intervalo entre disparos
            yield return new WaitForSeconds(shootInterval);
        }
        
        Debug.Log("Corrutina de disparo finalizada");
    }

    // Método para desactivar el modo de disparo después de un tiempo
    private System.Collections.IEnumerator DeactivateShootModeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DeactivateShootMode();
    }

    // Método para desactivar manualmente el modo de disparo
    public void DeactivateShootMode()
    {
        shootModeActive = false;
        
        if (shootingCoroutine != null)
        {
            StopCoroutine(shootingCoroutine);
            shootingCoroutine = null;
        }
        
        Debug.Log("Modo disparo desactivado en la paleta");
    }

    // Añade este método en PalletController.cs
    public void SetBulletPrefab(GameObject prefab)
    {
        bulletPrefab = prefab;
        Debug.Log("Prefab de bala asignado correctamente: " + prefab.name);
    }
}