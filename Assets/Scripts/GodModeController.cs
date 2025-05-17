using UnityEngine;

public class GodModeController : MonoBehaviour
{
    [Header("Wall Settings")]
    private bool isActive = false;
    private MeshRenderer meshRenderer;
    private Collider col;

    [Header("Boundary Settings")]
    [SerializeField] private GameObject lowerBoundary; // Reference to the lower boundary object

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        col = GetComponent<Collider>();

        SetWallState();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            isActive = !isActive;
            SetWallState();
            ToggleLowerBoundary();
        }
    }

    void SetWallState()
    {
        if (meshRenderer != null) meshRenderer.enabled = isActive;
        if (col != null) col.enabled = isActive;
    }

    void ToggleLowerBoundary()
    {
        if (lowerBoundary != null)
        {
            // Toggle collider if it exists
            Collider boundaryCollider = lowerBoundary.GetComponent<Collider>();
            if (boundaryCollider != null)
                boundaryCollider.enabled = !isActive;
        }
    }
}
