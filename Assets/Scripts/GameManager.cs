using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Cambiar de UnityEngine.UI a TMPro

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

    [Header("Score System")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private TMP_Text scoreText; // Cambiar de Text a TMP_Text

    // Añadir esta propiedad para las vidas
    [Header("Lives")]
    [SerializeField] private int maxLives = 3;

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
        // Reiniciar la puntuación al comenzar un nuevo nivel
        currentScore = 0;
        UpdateScoreUI();
        
        // Cargar la escena del nivel actual
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
    
    // Método para añadir puntos
    public void AddPoints(int points)
    {
        currentScore += points;
        UpdateScoreUI();
    }

    // Método para actualizar el UI de puntuación
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
    }
    
    // Método para obtener la puntuación actual
    public int GetCurrentScore()
    {
        return currentScore;
    }

    // Método para mostrar gameover
    public void GameOver()
    {
        // Guardar puntuación actual
        PlayerPrefs.SetInt("CurrentScore", currentScore);
        PlayerPrefs.Save();
        
        // Mostrar el GameOver
        ShowGameOverMenu();
        
        // Pausar el juego
        Time.timeScale = 0;
    }

    private void ShowGameOverMenu()
    {
        // Buscar o crear panel de GameOver si no existe
        if (gameOverPanel == null)
        {
            gameOverPanel = GameObject.Find("GameOverPanel");
            
            if (gameOverPanel == null)
            {
                Debug.LogError("No se encontró el GameOverPanel en la escena");
                return;
            }
        }
        
        // Activar el panel
        gameOverPanel.SetActive(true);
        
        // Establecer el texto de puntuación - BUSCA EN CUALQUIER PARTE
        TMP_Text scoreTextUI = gameOverPanel.GetComponentInChildren<TMP_Text>();
        if (scoreTextUI != null)
        {
            scoreTextUI.text = "Score: " + currentScore;
        }
        else
        {
            Debug.LogWarning("No se encontró TMP_Text en el GameOverPanel");
        }
        
        // Configurar MenuController si existe
        ConfigureMenuController();
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

    // Añade este método al GameManager
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si es una escena de juego (cualquiera menos la 0), reiniciar puntuación
        if (scene.buildIndex > 0)
        {
            currentScore = 0;
            UpdateScoreUI();
        }
        
        // Inicializa el nivel actual basado en el índice de la escena
        currentLevel = scene.buildIndex;
        
        // Oculta paneles de UI si existen
        HideAllPanels();
        
        // Busca referencias a paneles de UI
        FindUIReferences();
        
        // Configura MenuController si existe
        ConfigureMenuController();
    }

    private void FindUIReferences()
    {
        // Buscar el panel de GameOver
        if (gameOverPanel == null)
            gameOverPanel = GameObject.Find("GameOverPanel");
        
        // Buscar el texto de puntuación (puede estar en el HUD)
        if (scoreText == null)
        {
            GameObject hud = GameObject.Find("HUD");
            if (hud != null)
            {
                scoreText = hud.GetComponentInChildren<TMP_Text>();
            }
        }
    }

    private void ConfigureMenuController()
    {
        // Buscar MenuController en la escena
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController != null)
        {
            // Buscar nameInputPanel
            GameObject nameInputPanel = GameObject.Find("NameInputPanel");
            if (nameInputPanel != null)
            {
                menuController.nameInputPanel = nameInputPanel;
                Debug.Log("NameInputPanel asignado automáticamente al MenuController");
            }
            
            // Asignar ScoreText del GameOver
            if (gameOverPanel != null)
            {
                TMP_Text scoreTextUI = gameOverPanel.GetComponentInChildren<TMP_Text>();
                if (scoreTextUI != null)
                    menuController.scoreText = scoreTextUI;
            }
        }
    }
}