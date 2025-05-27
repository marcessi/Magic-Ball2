using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f; // Máximo tiempo de vida
    [SerializeField] private TrailRenderer trailRenderer;
    
    private void Start()
    {
        // Destruir la bala después de un tiempo máximo
        Destroy(gameObject, lifetime);
        
        // Configurar la estela
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }
        }
        
        // Configurar apariencia de la estela
        ConfigureTrail();
    }
    
    private void ConfigureTrail()
    {
        if (trailRenderer != null)
        {
            // Configurar la estela con aspecto de fuego/energía
            trailRenderer.startWidth = 0.3f;
            trailRenderer.endWidth = 0.05f;
            trailRenderer.time = 0.5f;
            
            // Colores azul eléctrico
            Color startColor = new Color(0.2f, 0.6f, 1f, 0.8f);
            Color endColor = new Color(0.5f, 0.8f, 1f, 0f);
            
            trailRenderer.startColor = startColor;
            trailRenderer.endColor = endColor;
            
            // Crear material si no existe
            if (trailRenderer.material == null)
            {
                Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
                trailRenderer.material = trailMaterial;
            }
        }
    }
    
    private void Update()
    {
        // Mover la bala hacia adelante
        
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);

    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Si choca con un bloque, destruirlo
        BlockController block = other.GetComponent<BlockController>();
        if (block != null)
        {
            block.Hit();
            // No destruir la bala, permite atravesar múltiples bloques
        }
        
        // Destruir la bala cuando choca con cualquier pared
       
    }
}