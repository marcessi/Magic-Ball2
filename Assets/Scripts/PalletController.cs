using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PalletController : MonoBehaviour
{
    [SerializeField] private float velocidad = 10f;
    [SerializeField] private float limiteIzquierdo = -8f;
    [SerializeField] private float limiteDerecho = 8f;
    
    [Header("Scale Settings")]
    [SerializeField] private float maxScaleFactor = 2.0f; // Escala máxima respecto a la original
    [SerializeField] private float minScaleFactor = 0.5f; // Escala mínima respecto a la original
    
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
    }
    
    private void Update()
    {
        // Invertir el valor de entrada para corregir la dirección
        float direccionCorregida = -movimientoInput.x;
        
        // Mover la paleta en su propio eje Y local (debido a la rotación de 90° en X)
        transform.Translate(Vector3.up * direccionCorregida * velocidad * Time.deltaTime, Space.Self);
        
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
        // También restauramos la escala original al reiniciar
        transform.localScale = originalScale;
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
        targetScale.y += incrementAmount;
        
        // Limitar al tamaño máximo (basado en la escala original)
        float maxScale = originalScale.y * maxScaleFactor;
        if (targetScale.y > maxScale)
        {
            targetScale.y = maxScale;
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
        targetScale.y -= decrementAmount;
        
        // Limitar al tamaño mínimo (basado en la escala original)
        float minScale = originalScale.y * minScaleFactor;
        if (targetScale.y < minScale)
        {
            targetScale.y = minScale;
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
            
            yield return null;
        }
        
        // Asegurar que llegamos exactamente al valor deseado
        transform.localScale = targetScale;
        
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
        
        // Cambiar apariencia visual para indicar que el modo imán está activo
        GetComponent<Renderer>().material.color = Color.blue;
        
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
        
        // Restaurar apariencia visual
        GetComponent<Renderer>().material.color = Color.white;
        
        Debug.Log("Modo imán desactivado en la paleta");
    }
    
    // Marca el efecto magnético como usado (llama a este método cuando la bola se suelte)
    public void MarkMagnetAsUsed()
    {
        magnetEffectUsed = true;
        magnetModeActive = false; // Desactivar completamente el modo imán
        
        // Restaurar apariencia visual
        GetComponent<Renderer>().material.color = Color.white;
        
        Debug.Log("Efecto imán usado y desactivado");
    }
    
    // Comprueba colisiones con la bola para aplicar el efecto imán
    private void OnCollisionEnter(Collision collision)
    {
        // Solo aplicar el efecto si está activo Y no ha sido usado aún
        if (magnetModeActive && !magnetEffectUsed && collision.gameObject.CompareTag("Ball"))
        {
            BallController ball = collision.gameObject.GetComponent<BallController>();
            if (ball != null)
            {
                // Aplicar efecto imán a la bola
                ball.AttachToPaddle(transform, this);
                Debug.Log("Bola adherida a la paleta por efecto imán");
            }
        }
    }

    // Añadir este método a PalletController.cs
    public void ResetToOriginalScale()
    {
        // Restaurar escala original inmediatamente, sin animación
        transform.localScale = originalScale;
        Debug.Log("Paleta restaurada a su escala original");
    }

    // Método para activar el modo de disparo
    public void ActivateShootMode(float duration)
    {
        // Activar modo de disparo
        shootModeActive = true;
        
        // Cambiar apariencia visual para indicar que el modo disparo está activo
        GetComponent<Renderer>().material.color = new Color(0.2f, 0.6f, 1f); // Azul eléctrico
        
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
        // Crear puntos de disparo si no existen
        if (leftShootPoint == null)
        {
            leftShootPoint = new GameObject("LeftShootPoint").transform;
            leftShootPoint.SetParent(transform);
            // Modificar la posición - notar que Z ahora es positivo
            leftShootPoint.localPosition = new Vector3(-transform.localScale.y/2 + 0.2f, 0.2f, 0.3f);
            // Asegurarnos que mira hacia adelante
            leftShootPoint.localRotation = Quaternion.Euler(0, 0, 0);
        }
        
        if (rightShootPoint == null)
        {
            rightShootPoint = new GameObject("RightShootPoint").transform;
            rightShootPoint.SetParent(transform);
            // Modificar la posición - notar que Z ahora es positivo
            rightShootPoint.localPosition = new Vector3(transform.localScale.y/2 - 0.2f, 0.2f, 0.3f);
            // Asegurarnos que mira hacia adelante
            rightShootPoint.localRotation = Quaternion.Euler(0, 0, 0);
        }
        
        // Mientras el modo esté activo, disparar a intervalos
        while (shootModeActive)
        {
            // Disparar desde ambos puntos
            if (bulletPrefab != null)
            {
                // Disparo izquierdo - usar la rotación correcta para que vaya hacia adelante
                GameObject leftBullet = Instantiate(
                    bulletPrefab, 
                    leftShootPoint.position, 
                    Quaternion.Euler(0, 0, 0) // Esta rotación es crucial
                );
                
                // Disparo derecho - usar la rotación correcta para que vaya hacia adelante
                GameObject rightBullet = Instantiate(
                    bulletPrefab, 
                    rightShootPoint.position, 
                    Quaternion.Euler(0, 0, 0) // Esta rotación es crucial
                );
                
                // Sonido de disparo (opcional)
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.Play();
                }
            }
            else
            {
                Debug.LogWarning("Prefab de bala no asignado en PalletController");
            }
            
            // Esperar el intervalo entre disparos
            yield return new WaitForSeconds(shootInterval);
        }
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
        
        // Restaurar apariencia visual
        GetComponent<Renderer>().material.color = Color.white;
        
        Debug.Log("Modo disparo desactivado en la paleta");
    }
}