using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Intro Animation Settings")]
    [SerializeField] private float animationDuration = 3.0f;
    [SerializeField] private float overviewRadius = 15.0f;
    [SerializeField] private float overviewHeight = 15.0f;
    [SerializeField] private Vector3 levelCenter = Vector3.zero;
    [SerializeField] private AnimationCurve transitionCurve;
    
    private Vector3 gameplayPosition;
    private Quaternion gameplayRotation;
    private bool animationCompleted = false;
    
    private void Awake()
    {
        // Store the camera's intended gameplay position and rotation
        gameplayPosition = transform.position;
        gameplayRotation = transform.rotation;
        
        // If no curve defined, create a default smooth curve
        if (transitionCurve.length == 0)
        {
            transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }
    
    private void Start()
    {
        // Start the intro animation
        StartCoroutine(PlayIntroAnimation());
    }
    
    public void PlayLevelIntro()
    {
        // Public method to trigger the animation externally
        if (!animationCompleted || Time.timeSinceLevelLoad < 0.5f)
        {
            StartCoroutine(PlayIntroAnimation());
        }
    }
    
    private IEnumerator PlayIntroAnimation()
    {
        // Calculate overview position with greater distance
        float startingDistance = overviewRadius * 2f;  // 50% further away
        float startingHeight = overviewHeight * 1.3f;    // 30% higher

        Vector3 overviewPosition = new Vector3(
            levelCenter.x, 
            levelCenter.y + startingHeight, 
            levelCenter.z + startingDistance
        );
        
        // Create a rotation that looks at the center of the level
        Quaternion overviewRotation = Quaternion.LookRotation(
            levelCenter - overviewPosition,
            Vector3.up
        );
        
        // Set camera to overview position immediately
        transform.position = overviewPosition;
        transform.rotation = overviewRotation;
        
        // Wait a moment to let the player see the level
        yield return new WaitForSeconds(0.5f);
        
        // Rest of animation remains the same
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // Use animation curve for smoother motion
            float curvedT = transitionCurve.Evaluate(t);
            
            // Interpolate position and rotation
            transform.position = Vector3.Lerp(overviewPosition, gameplayPosition, curvedT);
            transform.rotation = Quaternion.Slerp(overviewRotation, gameplayRotation, curvedT);
            
            yield return null;
        }
        
        // Ensure exact final position
        transform.position = gameplayPosition;
        transform.rotation = gameplayRotation;
        
        animationCompleted = true;
    }
    
    // Method to reset camera animation for new level
    public void ResetForNewLevel()
    {
        animationCompleted = false;
    }
}