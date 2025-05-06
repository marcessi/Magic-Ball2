using UnityEngine;
using UnityEngine.InputSystem;

public class PalletController : MonoBehaviour
{
    [SerializeField] private float velocidad = 10f;  // Velocidad de movimiento de la paleta
    [SerializeField] private float limiteIzquierdo = -8f;  // Límite izquierdo del movimiento
    [SerializeField] private float limiteDerecho = 8f;  // Límite derecho del movimiento
    
    private Vector2 movimientoInput;
    private Vector3 initialPosition;
    
    private void Start()
    {
        // Store initial position
        initialPosition = transform.position;
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
    
    // Method to reset the paddle position
    public void ResetPosition()
    {
        transform.position = initialPosition;
    }
}