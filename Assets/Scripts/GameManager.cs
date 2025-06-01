using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI; // Añadir este namespace para usar Image

public class GameManager : MonoBehaviour
{
    // Singleton para acceder al GameManager desde cualquier script
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    [SerializeField] private int currentLevel = 0;
    [SerializeField] private int currentLives = 3;
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int currentScore = 0;
    [SerializeField] private bool gameInProgress = false;
    
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanelPrefab;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject gameOverPanel; // Variable faltante
    [SerializeField] private GameObject victoryPanel; // Variable faltante
    [SerializeField] private GameObject pausePanel;   // Variable faltante
    private bool isPaused = false;                    // Variable faltante
    
    private void Awake()
    {
        // Configuración del singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Nueva función para inicializar variables
    private void InitializeGame()
    {
        // Registrar para eventos de carga de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // Desregistrar eventos al destruir
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // Esta función se llama cada vez que se carga una escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentLevel = scene.buildIndex;
        
        // Si es escena de menú principal (índice 0)
        if (currentLevel == 0)
        {
            // Resetear estado del juego al volver al menú principal
            if (gameInProgress) 
            {
                gameInProgress = false;
            }
        }
        else // Si es una escena de nivel
        {
            // Primera vez que iniciamos un nivel desde el menú
            if (!gameInProgress)
            {
                ResetGameState();
                gameInProgress = true;
            }
        }
        
        // Ocultar todos los paneles
        HideAllPanels();
        
        // Buscar/actualizar referencias de UI en la nueva escena
        FindUIReferences();
        UpdateUI();
        
        // Configurar MenuController si existe
        ConfigureMenuController();
    }
    
    // Resetea el estado del juego al iniciar una nueva partida
    public void ResetGameState()
    {
        currentLives = maxLives;
        currentScore = 0;
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
    
    // Método para obtener las vidas actuales (faltaba este método)
    public int GetCurrentLives()
    {
        return currentLives;
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
        
        // IMPORTANTE: Asegurar que los botones funcionen
        //SetupGameOverButtons();
        
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
            
            // Si no lo encontramos, buscar directamente
            if (scoreText == null)
            {
                GameObject scoreObject = GameObject.Find("ScoreText");
                if (scoreObject != null)
                {
                    scoreText = scoreObject.GetComponent<TMP_Text>();
                }
            }
        }
        
        // Buscar el panel de victoria
        if (victoryPanel == null)
            victoryPanel = GameObject.Find("VictoryPanel");
            
        // Buscar el panel de pausa
        if (pausePanel == null)
            pausePanel = GameObject.Find("PausePanel");
            
        // Si estamos en una escena de nivel, asegurarnos que el GameOver esté listo
        if (currentLevel > 0)
        {
            EnsureGameOverPanel();
        }
    }

    private void ConfigureMenuController()
    {
        // Buscar MenuController en la escena
        MenuController menuController = FindObjectOfType<MenuController>();
        if (menuController != null)
        {
            // Si tenemos GameOver panel, asignarlo al MenuController
            if (gameOverPanel != null)
            {
                menuController.gameOverMenu = gameOverPanel;
                
                // Asignar ScoreText del GameOver
                TMP_Text scoreTextUI = gameOverPanel.GetComponentInChildren<TMP_Text>();
                if (scoreTextUI != null)
                    menuController.scoreText = scoreTextUI;
                    
                // Asignar nameInputPanel si existe
                Transform nameInputPanelTrans = gameOverPanel.transform.Find("NameInputPanel");
                if (nameInputPanelTrans != null)
                {
                    menuController.nameInputPanel = nameInputPanelTrans.gameObject;
                    Debug.Log("NameInputPanel asignado automáticamente al MenuController");
                    
                    // Buscar el input field
                    TMP_InputField inputField = nameInputPanelTrans.GetComponentInChildren<TMP_InputField>();
                    if (inputField != null)
                        menuController.playerNameInput = inputField;
                }
                
                // Configurar los botones con el MenuController también
                menuController.SetupGameOverButtons();
            }
        }
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
    
    // Método para reiniciar el nivel currentLevel
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
    
    // Método que se llama cuando el jugador pierde una vida
    public void LoseLife()
    {
        currentLives--;
        UpdateUI();
        
        if (currentLives <= 0)
        {
            GameOver();
        }
    }
    
    // Método para asegurar que existe un panel de GameOver y configurar sus botones
    private void EnsureGameOverPanel()
    {
        if (gameOverPanel == null)
            gameOverPanel = GameObject.Find("GameOverPanel");
            
        if (gameOverPanel == null && gameOverPanelPrefab != null)
        {
            // Si no existe un panel en la escena, instanciar desde prefab
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                gameOverPanel = Instantiate(gameOverPanelPrefab, mainCanvas.transform);
                Debug.Log("GameOver panel instantiated");
            }
        }
        
        // Ocultar el panel al iniciar
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            
            // Configurar los botones del panel - SOLUCIÓN PRINCIPAL
            //SetupGameOverButtons();
        }
    }

    // Método para configurar los botones del GameOver (nuevo método)
    private void SetupGameOverButtons()
    {
       
    }

    // Método para guardar puntuación con nombre predeterminado (nuevo método)
    private void SaveDefaultScore()
    {
        string defaultName = "Player";
        
        // Guardar la puntuación
        int scoreCount = PlayerPrefs.GetInt("ScoreCount", 0);
        PlayerPrefs.SetString("ScoreName_" + scoreCount, defaultName);
        PlayerPrefs.SetInt("ScoreValue_" + scoreCount, currentScore);
        PlayerPrefs.SetInt("ScoreCount", scoreCount + 1);
        PlayerPrefs.Save();
        
        Debug.Log("Puntuación guardada con nombre predeterminado: " + defaultName);
        
        // Volver al menú principal después de un tiempo
        Invoke("GoToMainMenu", 1.5f);
    }
    
    // Actualizar toda la UI con los valores actuales
    private void UpdateUI()
    {
        // Actualizar puntuación
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
        
        // Actualizar vidas (si tienes un display de vidas)
        UpdateLivesDisplay();
    }
    
    // Actualizar display de vidas
    private void UpdateLivesDisplay()
    {
        // Buscar textos de vidas y corazones
        TMP_Text livesText = GameObject.Find("LivesText")?.GetComponent<TMP_Text>();
        if (livesText != null)
        {
            livesText.text = "Lives: " + currentLives;
        }
        
        // Actualizar corazones visuales si existen
        GameObject heartsContainer = GameObject.Find("HUD");
        if (heartsContainer != null)
        {
            Image[] heartImages = heartsContainer.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (i < currentLives)
                    heartImages[i].enabled = true;
                else
                    heartImages[i].enabled = false;
            }
        }
    }
}