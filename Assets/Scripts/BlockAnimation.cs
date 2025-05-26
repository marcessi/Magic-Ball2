using UnityEngine;

public class BlockAnimation : MonoBehaviour
{
    public float scrollSpeedX = 0.1f;
    public float scrollSpeedY = 0.05f;

    private Renderer rend;
    private Material mat;

    void Start()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material; // Clona el material para no afectar a otros
    }

    void Update()
    {
        float offsetX = Time.time * scrollSpeedX;
        float offsetY = Time.time * scrollSpeedY;
        mat.mainTextureOffset = new Vector2(offsetX, offsetY);
    }
}
