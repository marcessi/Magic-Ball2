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
    
    [Header("Animation Light")]
    [SerializeField] private Light spotlightPrefab;
    [SerializeField] private float spotlightIntensity = 1f;
    [SerializeField] private Color spotlightColor = Color.white;
    
    private Vector3 gameplayPosition;
    private Quaternion gameplayRotation;
    private bool animationCompleted = false;
    private PalletController playerPaddle;
    private Light animationLight;
    
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
        
        // Find the player paddle controller
        playerPaddle = FindObjectOfType<PalletController>();
    }
    
    private void Start()
    {
        // Disable player controls
        if (playerPaddle != null)
        {
            playerPaddle.enabled = false;
        }
        
        // Create animation directional light
        SetupAnimationLight();
        
        // Start the intro animation
        StartCoroutine(PlayIntroAnimation());
    }
    
    private void SetupAnimationLight()
    {
        // Create directional light
        GameObject lightObj = new GameObject("Animation Directional Light");
        animationLight = lightObj.AddComponent<Light>();
        animationLight.type = LightType.Directional;
        animationLight.intensity = spotlightIntensity;
        animationLight.color = spotlightColor;
        animationLight.shadows = LightShadows.Soft;
        
        // Parent to camera and set rotation
        animationLight.transform.parent = transform;
        animationLight.transform.localPosition = Vector3.zero;
        animationLight.transform.localRotation = Quaternion.identity;
    }
    
    public void PlayLevelIntro()
    {
        // Public method to trigger the animation externally
        if (!animationCompleted || Time.timeSinceLevelLoad < 0.5f)
        {
            // Disable player controls
            if (playerPaddle != null)
            {
                playerPaddle.enabled = false;
            }
            
            // Reactivate the directional light
            if (animationLight != null)
            {
                animationLight.gameObject.SetActive(true);
            }
            else
            {
                SetupAnimationLight();
            }
            
            StartCoroutine(PlayIntroAnimation());
        }
    }
    
    private IEnumerator PlayIntroAnimation()
    {
        // Store original position and rotation
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        // Final position and rotation (back to original gameplay position)
        Vector3 finalPosition = gameplayPosition;
        Quaternion finalRotation = gameplayRotation;
        
        // Ensure we have the exact original rotation stored
        gameplayRotation = transform.rotation;
        
        float elapsed = 0f;
        
        // Calculate distance from center to create orbit
        float distanceFromCenter = Vector3.Distance(levelCenter, new Vector3(startPosition.x, 0, startPosition.z));
        float height = startPosition.y;
        
        // Calculate initial angle
        Vector3 dirToCamera = new Vector3(startPosition.x, 0, startPosition.z) - levelCenter;
        float initialAngle = Mathf.Atan2(dirToCamera.x, dirToCamera.z) * Mathf.Rad2Deg;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float curveValue = transitionCurve.Evaluate(t);
            
            // For the last portion of the animation, start blending to the final position/rotation
            if (t > 0.8f)
            {
                float endBlend = (t - 0.8f) * 5f; // 0 to 1 in last 20% of animation
                
                // Blend position
                transform.position = Vector3.Lerp(
                    // Position on the orbit
                    levelCenter + new Vector3(
                        Mathf.Sin(Mathf.Deg2Rad * (initialAngle + curveValue * 360f)) * distanceFromCenter,
                        height,
                        Mathf.Cos(Mathf.Deg2Rad * (initialAngle + curveValue * 360f)) * distanceFromCenter
                    ),
                    // Target final position
                    finalPosition,
                    endBlend
                );
                
                // Blend rotation directly to final rotation
                transform.rotation = Quaternion.Slerp(
                    Quaternion.LookRotation(levelCenter - transform.position, Vector3.up),
                    finalRotation,
                    endBlend
                );
            }
            else
            {
                // Regular orbit animation (first 80%)
                float angle = initialAngle + curveValue * 360f;
                float rad = angle * Mathf.Deg2Rad;
                
                // Calculate orbit position
                Vector3 orbitPosition = levelCenter + new Vector3(
                    Mathf.Sin(rad) * distanceFromCenter,
                    height,
                    Mathf.Cos(rad) * distanceFromCenter
                );
                
                // Position the camera
                transform.position = orbitPosition;
                
                // Look at center
                transform.rotation = Quaternion.LookRotation(levelCenter - transform.position, Vector3.up);
            }
            
            // Update directional light rotation to match camera
            if (animationLight != null)
            {
                animationLight.transform.rotation = transform.rotation;
            }
            
            yield return null;
        }
        
        // Ensure camera ends up exactly at the initial position and rotation
        transform.position = finalPosition;
        transform.rotation = finalRotation;
        
        // Animation complete
        animationCompleted = true;
        
        // Enable player controls
        if (playerPaddle != null)
        {
            playerPaddle.enabled = true;
        }
        
        // Disable animation light
        if (animationLight != null)
        {
            animationLight.gameObject.SetActive(false);
        }
    }
    
    // Method to reset camera animation for new level
    public void ResetForNewLevel()
    {
        animationCompleted = false;
    }
}