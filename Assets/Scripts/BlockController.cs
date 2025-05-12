using System.Collections;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    [Header("Block Properties")]
    [SerializeField] private int blockLevel = 0; // 0 is the lowest level
    [SerializeField] private float descendSpeed = 2f;
    [SerializeField] private GameObject breakEffect; // Particle effect or animation prefab for breaking
    [SerializeField] private float breakAnimationDuration = 0.5f;
    [SerializeField] private float blockHeight = 1f; // Height of each block level
    
    private bool isBreaking = false;
    private bool isLowLevel = false;
    private bool shouldDescend = false;
    private bool isDescending = false;
    private Vector3 targetPosition;
    
    private void Start()
    {
        // Round x and z positions to nearest 0.5 units
        Vector3 position = transform.position;
        position.x = Mathf.Round(position.x * 2) / 2; // Round to nearest 0.5
        position.z = Mathf.Round(position.z * 2) / 2; // Round to nearest 0.5
        transform.position = position;
        
        // Initialize block level based on y position
        blockLevel = Mathf.RoundToInt(transform.position.y / blockHeight);
        
        // Check if block is at the lowest level (y=0)
        isLowLevel = blockLevel == 0;
        
        // Set initial target position one level down
        UpdateTargetPosition();
    }
    
    private void Update()
    {
        // If block is in the process of descending
        if (shouldDescend && !isBreaking)
        {
            isDescending = true;
            
            // Move block towards target position
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                descendSpeed * Time.deltaTime
            );
            
            // Check if block has reached the target level
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                blockLevel--;
                isLowLevel = blockLevel == 0;
                shouldDescend = false;
                isDescending = false;
                
                // Update target for next potential descent
                UpdateTargetPosition();
            }
        }
    }
    
    private void UpdateTargetPosition()
    {
        // Target is one level down from current position
        targetPosition = new Vector3(
            transform.position.x,
            blockLevel > 0 ? (blockLevel - 1) * blockHeight : 0f,
            transform.position.z
        );
    }
    
    public void OnHit()
    {
        // Only breakable if at the lowest level and not currently descending or breaking
        if (isLowLevel && !isDescending && !isBreaking)
        {
            StartCoroutine(BreakAnimation());
        }
    }
    
    private IEnumerator BreakAnimation()
    {
        isBreaking = true;
        
        // Play break effect/animation
        if (breakEffect != null)
        {
            Instantiate(breakEffect, transform.position, Quaternion.identity);
        }
        
        // Visual breaking animation (e.g., scale down and rotate)
        Vector3 originalScale = transform.localScale;
        Quaternion originalRotation = transform.rotation;
        float elapsed = 0f;
        
        while (elapsed < breakAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / breakAnimationDuration;
            
            // Scale down
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            
            // Add some rotation for effect
            transform.Rotate(Vector3.one * 180f * Time.deltaTime);
            
            yield return null;
        }
        
        // Notify blocks above to descend
        NotifyBlocksAbove();
        
        // Destroy the block
        Destroy(gameObject);
    }
    
    private void NotifyBlocksAbove()
    {
        // Cast a ray upwards to find ALL blocks above this one
        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            Vector3.up,
            100f // Increased range to detect all blocks above
        );
        
        foreach (RaycastHit hit in hits)
        {
            // Check if hit object has a BlockController
            BlockController blockAbove = hit.collider.GetComponent<BlockController>();
            if (blockAbove != null)
            {
                // Tell the block above to start descending
                blockAbove.StartDescending();
            }
        }
    }
    
    public void StartDescending()
    {
        shouldDescend = true;
    }
}