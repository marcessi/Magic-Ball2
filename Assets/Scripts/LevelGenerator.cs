using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Brick Settings")]
    public GameObject brickPrefab;
    public int rows = 5;
    public int columns = 10;
    public float spacingX = 1.2f;
    public float spacingY = 0.6f;
    public Vector3 startPos = new Vector3(-5f, 3f, 0);

    void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 pos = new Vector3(
                    startPos.x + x * spacingX,
                    startPos.y - y * spacingY,
                    startPos.z
                );
                Instantiate(brickPrefab, pos, Quaternion.identity, transform);
            }
        }
    }
}
