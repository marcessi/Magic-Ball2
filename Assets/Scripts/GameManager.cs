using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

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
    private bool isChangingLevel = false;            // Nueva variable para controlar cambio de nivel
    
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private string mainMenuMusicTrack = "Audio/MainMenu";
    private string[] levelMusicTracks = {
        "Audio/Musica-Puente",
        "Audio/Musica-Iglu",
        "Audio/Musica-Cascada",
        "Audio/Musica-Volcan",
        "Audio/Musica-Piramide"
    };
    [SerializeField] private string victoryAudioTrack = "Audio/Victory";
    [SerializeField] private string gameOverAudioTrack = "Audio/GameOver";
    
    // Variables para recordar la música actual
    private AudioClip currentMusicClip;
    private float currentMusicTime;
    private bool isMusicPaused = false;
    
    private void Awake()
    {
        // Configuración del singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
            
            // Configurar fuente de audio si no existe
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.volume = 0.4f; // Inicializar con volumen para menú principal
            }
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
            // Notificar al MenuController para actualizar el high score
            MenuController menuController = FindObjectOfType<MenuController>();
            if (menuController != null)
            {
                menuController.UpdateHighScoreDisplay();
                Debug.Log("High Score actualizado por GameManager después de cargar el menú principal");
            }
            
            // Resetear estado del juego al volver al menú principal
            if (gameInProgress) 
            {
                gameInProgress = false;
            }
            
            // Reproducir música del menú principal
            PlayMainMenuMusic();
        }
        else // Si es una escena de nivel
        {
            // Primera vez que iniciamos un nivel desde el menú
            if (!gameInProgress)
            {
                ResetGameState();
                gameInProgress = true;
            }
            
            // Reproducir música del nivel actual
            PlayLevelMusic();
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
        
        // Reproducir sonido de Game Over
        PlayGameOverSound();
        
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
        Debug.Log("¡VICTORIA! Preparando transición al siguiente nivel...");
        
        // Marcar que estamos en proceso de cambio de nivel
        isChangingLevel = true;
        
        // PRIMERO: Asegurar que el tiempo esté corriendo
        Time.timeScale = 1f;
        
        // Mostrar panel de victoria si existe
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        
        // Usar corrutina en lugar de Invoke para mayor seguridad
        StartCoroutine(GoToNextLevelWithDelay(2f));
    }
    
    // Añadir este método para consulta
    public bool IsChangingLevel()
    {
        return isChangingLevel;
    }
    
    // Nuevo método para manejar el cambio de nivel con seguridad
    private System.Collections.IEnumerator GoToNextLevelWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Asegurar nuevamente que el tiempo está corriendo
        Time.timeScale = 1f;
        
        // Llamar al método para cambiar de nivel
        GoToNextLevel();
    }
    
    // Corrige el método GoToNextLevel en GameManager.cs
    public void GoToNextLevel()
    {
        // IMPORTANTE: Restaurar el tiempo normal
        Time.timeScale = 1f;
        
        try
        {
            // Forzar reinicio de contadores de bloques antes de cambiar de nivel
            BlockController.ResetLevelCounters();
            
            // Obtener el índice de la escena ACTUAL
            int currentIdx = SceneManager.GetActiveScene().buildIndex;
            int nextLevelIndex = currentIdx + 1;
            
            Debug.Log($"Intentando cargar nivel {nextLevelIndex} desde nivel actual {currentIdx}");
            
            // Verificar si existe el siguiente nivel
            if (nextLevelIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log("Cargando siguiente nivel: " + nextLevelIndex);
                
                // Guardar el índice antes de cargar la nueva escena
                currentLevel = nextLevelIndex;
                
                // IMPORTANTE: Asegurar que isChangingLevel es true antes de cargar la escena
                isChangingLevel = true;
                
                // Cargar la siguiente escena
                SceneManager.LoadScene(nextLevelIndex);
            }
            else
            {
                Debug.Log("¡Has completado todos los niveles! Mostrando Game Over para celebrar tu victoria.");
                
                // NUEVO: En lugar de ir al menú principal, mostrar el Game Over
                // con la puntuación final como celebración de victoria
                ShowVictoryGameOver();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al cambiar de nivel: " + e.Message);
            // Fallback: volver al menú principal
            GoToMainMenu();
        }
        
        // Restablecer la bandera de cambio de nivel después de un tiempo
        Invoke("ResetChangingLevelFlag", 3f);
    }
    
    // NUEVO: Método para mostrar el Game Over como pantalla de victoria final
    private void ShowVictoryGameOver()
    {
        // Reproducir sonido de victoria
        PlayVictorySound();
        
        // Pausar el juego
        Time.timeScale = 0;
        
        // Asegurar que tenemos un panel de Game Over
        EnsureGameOverPanel();
        
        if (gameOverPanel != null)
        {
            // Activar el panel
            gameOverPanel.SetActive(true);
            
            // Buscar el TMP_Text para mostrar un mensaje especial
            TMP_Text scoreTextUI = gameOverPanel.GetComponentInChildren<TMP_Text>();
            if (scoreTextUI != null)
            {
                // Mensaje especial de victoria
                scoreTextUI.text = "¡VICTORIA FINAL!\nPuntuación: " + currentScore;
            }
            
            // Buscar el GameOverPanelController para configurarlo adecuadamente
            GameOverPanelController controller = gameOverPanel.GetComponent<GameOverPanelController>();
            if (controller != null)
            {
                controller.Show(currentScore);
            }
            
            // Configurar los botones del panel
            ConfigureMenuController();
        }
        else
        {
            Debug.LogError("No se pudo encontrar o crear el panel de Game Over");
            // Fallback
            GameOver();
        }
    }
    
    public void AddLife()
    {
        // Solo añadir vida si no está al máximo
        if (currentLives < maxLives)
        {
            currentLives++;
            Debug.Log("Vida añadida. Vidas actuales: " + currentLives);
            
            // Actualizar la UI
            UpdateUI();
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

    private void HideAllPanels()
    {
        // Ocultar todos los paneles de UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
            
        if (pausePanel != null)
            pausePanel.SetActive(false);
            
        Debug.Log("Todos los paneles ocultados");
    }

    private void FindUIReferences()
    {
        // Buscar referencias a elementos UI en la escena actual
        if (scoreText == null)
            scoreText = GameObject.Find("ScoreText")?.GetComponent<TMP_Text>();
            
        if (gameOverPanel == null)
            gameOverPanel = GameObject.Find("GameOverPanel");
            
        if (victoryPanel == null)
            victoryPanel = GameObject.Find("VictoryPanel");
            
        if (pausePanel == null)
            pausePanel = GameObject.Find("PausePanel");
            
        // Asegurar que existe un panel de GameOver
        EnsureGameOverPanel();
        
        Debug.Log("Referencias UI actualizadas");
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
                    
                    // Buscar el input field
                    TMP_InputField inputField = nameInputPanelTrans.GetComponentInChildren<TMP_InputField>();
                    if (inputField != null)
                        menuController.playerNameInput = inputField;
                }
                
                // Configurar los botones
                menuController.SetupGameOverButtons();
                
                Debug.Log("MenuController configurado con éxito");
            }
        }
    }
    
    // Método para reproducir la música correspondiente al nivel
    private void PlayLevelMusic()
    {
        // Verificar que el nivel sea válido para nuestro array de música
        // Los niveles empiezan en 1 (index 0 es menú), así que restamos 1 para el array
        int musicIndex = currentLevel - 1;
        
        if (musicIndex >= 0 && musicIndex < levelMusicTracks.Length)
        {
            // Cargar y reproducir la música
            AudioClip levelMusic = Resources.Load<AudioClip>(levelMusicTracks[musicIndex]);
            
            if (levelMusic != null)
            {
                musicSource.clip = levelMusic;
                musicSource.volume = 0.25f; // Establecer volumen a 0.25 para música de niveles
                musicSource.Play();
                Debug.Log($"Reproduciendo música: {levelMusicTracks[musicIndex]} con volumen: 0.25");
            }
            else
            {
                Debug.LogWarning($"No se pudo cargar la música: {levelMusicTracks[musicIndex]}");
            }
        }
        else
        {
            Debug.LogWarning($"No hay música configurada para el nivel {currentLevel}");
        }
    }
    
    // Método para reproducir la música del menú principal
    private void PlayMainMenuMusic()
    {
        // Cargar y reproducir la música del menú principal
        AudioClip menuMusic = Resources.Load<AudioClip>(mainMenuMusicTrack);
        
        if (menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.volume = 0.4f; // Establecer volumen a 0.4 para música del menú principal
            musicSource.Play();
            Debug.Log($"Reproduciendo música del menú principal: {mainMenuMusicTrack} con volumen: 0.4");
        }
        else
        {
            Debug.LogWarning($"No se pudo cargar la música del menú principal: {mainMenuMusicTrack}");
        }
    }
    
    // Método para detener la música
    private void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }
    
    // Método para reproducir sonido de victoria
    private void PlayVictorySound()
    {
        // Detener cualquier música que esté sonando
        StopMusic();
        
        // Cargar y reproducir el sonido de victoria
        AudioClip victoryClip = Resources.Load<AudioClip>(victoryAudioTrack);
        
        if (victoryClip != null)
        {
            // Usar la misma fuente de audio para el sonido de victoria
            musicSource.clip = victoryClip;
            musicSource.loop = false; // Sin bucle para sonidos de victoria
            musicSource.volume = 0.25f; // Mantener volumen apropiado para efectos
            musicSource.Play();
            
            Debug.Log($"Reproduciendo sonido de victoria: {victoryAudioTrack} con volumen: 0.25");
        }
        else
        {
            Debug.LogWarning($"No se pudo cargar el sonido de victoria: {victoryAudioTrack}");
        }
    }
    
    // Método para reproducir sonido de game over
    private void PlayGameOverSound()
    {
        // Detener cualquier música que esté sonando
        StopMusic();
        
        // Cargar y reproducir el sonido de game over
        AudioClip gameOverClip = Resources.Load<AudioClip>(gameOverAudioTrack);
        
        if (gameOverClip != null)
        {
            // Usar la misma fuente de audio para el sonido de derrota
            musicSource.clip = gameOverClip;
            musicSource.loop = false; // Sin bucle para sonidos de derrota
            musicSource.volume = 0.25f; // Mantener volumen apropiado para efectos
            musicSource.Play();
            
            Debug.Log($"Reproduciendo sonido de game over: {gameOverAudioTrack} con volumen: 0.25");
        }
        else
        {
            Debug.LogWarning($"No se pudo cargar el sonido de game over: {gameOverAudioTrack}");
        }
    }
}