using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton para acceder al GameManager desde cualquier script
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [SerializeField] private bool isPaused = false;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int totalLevels = 1; // Ajusta este valor al número total de niveles
    
    [Header("References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject pausePanel;

    private void Awake()
    {
        // Configuración del singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Inicializa el nivel actual basado en el índice de la escena
        currentLevel = SceneManager.GetActiveScene().buildIndex;
        
        // Oculta paneles de UI si existen
        HideAllPanels();
    }

    // Método para ir al siguiente nivel con verificación
    public void GoToNextLevel()
    {
        try
        {
            // Calcula el índice del siguiente nivel
            int nextLevelIndex = currentLevel + 1;
            
            // Verifica si existe el siguiente nivel
            if (nextLevelIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log("Cargando siguiente nivel: " + nextLevelIndex);
                SceneManager.LoadScene(nextLevelIndex);
                currentLevel = nextLevelIndex;
            }
            else
            {
                Debug.Log("¡Has completado todos los niveles! Volviendo al menú principal.");
                GoToMainMenu();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al cambiar de nivel: " + e.Message);
            // Fallback: volver al menú principal
            GoToMainMenu();
        }
    }
    
    // Método para reiniciar el nivel actual
    public void RestartLevel()
    {
        SceneManager.LoadScene(currentLevel);
    }
    
    // Método para volver al menú principal
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0); // Asumimos que el menú principal es la escena 0
    }
    
    // Método para pausar/reanudar el juego
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;
        
        // Mostrar/ocultar panel de pausa si existe
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }
    }
    
    // Método para mostrar gameover
    public void GameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Pausa el juego
        Time.timeScale = 0;
    }
    
    // Método para mostrar victoria
    public void Victory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        
        // Continuar al siguiente nivel después de un tiempo
        Invoke("GoToNextLevel", 3f);
    }
    
    // Oculta todos los paneles de UI
    private void HideAllPanels()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }
}