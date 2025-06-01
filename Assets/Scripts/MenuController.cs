using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class MenuController : MonoBehaviour
{
    [Header("Menú Principal")]
    public GameObject mainMenu;
    public GameObject creditsPanel;
    public Text highScoreText;

    [Header("Game Over")]
    public GameObject gameOverMenu;
    public TMP_InputField playerNameInput;
    public TMP_Text scoreText;
    
    private int m_OpenParameterId;
    private Animator m_Open;
    private GameObject m_PreviouslySelected;
    
    public GameObject nameInputPanel;
    
    // Constantes para las animaciones
    const string k_OpenTransitionName = "Open";
    const string k_ClosedStateName = "Closed";
    
    // Control de modos
    private bool isMainMenuScene = false;

    private void Start()
    {
        // Asegurar que el tiempo está corriendo
        Time.timeScale = 1;
        
        // Inicializar animación
        m_OpenParameterId = Animator.StringToHash(k_OpenTransitionName);
        
        // Determinar en qué escena estamos
        isMainMenuScene = SceneManager.GetActiveScene().buildIndex == 0;
        
        if (isMainMenuScene)
        {
            // Configuración para escena de menú principal
            ShowMainMenu();
            UpdateHighScoreDisplay();
            
            // Ocultar créditos inicialmente
            if (creditsPanel != null)
                creditsPanel.SetActive(false);
        }
        else
        {
            // Configuración para escenas de nivel
            if (gameOverMenu != null)
                gameOverMenu.SetActive(false);
                
            FindOrCreateNameInputPanel();
            SetupGameOverButtons();
        }
        
        Debug.Log("MenuController iniciado en modo: " + (isMainMenuScene ? "Menú Principal" : "Nivel de Juego"));
    }
    
    // FUNCIONES PARA EL MENÚ PRINCIPAL
    
    // Mostrar menú principal
    public void ShowMainMenu()
    { 
        if (mainMenu != null)
        {
            OpenPanel(mainMenu.GetComponent<Animator>());
            
            // Ocultar otros paneles
            if (creditsPanel != null)
                creditsPanel.SetActive(false);
        }
    }
    
    // Mostrar créditos
    public void ShowCredits()
    {
        if (creditsPanel != null)
        {
            OpenPanel(creditsPanel.GetComponent<Animator>());
        }
    }
    
    // Actualizar el texto de mayor puntuación
    public void UpdateHighScoreDisplay()
    {
        if (highScoreText != null)
        {
            // Obtener el número de puntuaciones guardadas
            int scoreCount = PlayerPrefs.GetInt("ScoreCount", 0);
            Debug.Log("Número total de puntuaciones guardadas: " + scoreCount);
            
            if (scoreCount > 0)
            {
                // Buscar la puntuación más alta
                string bestPlayerName = "Player";
                int highestScore = 0;
                int bestScoreIndex = -1;
                
                // Mostrar todas las puntuaciones guardadas para depuración
                Debug.Log("Lista de todas las puntuaciones guardadas:");
                for (int i = 0; i < scoreCount; i++)
                {
                    string name = PlayerPrefs.GetString("ScoreName_" + i, "???");
                    int score = PlayerPrefs.GetInt("ScoreValue_" + i, 0);
                    Debug.Log($"Puntuación {i}: {name} - {score}");
                    
                    if (score > highestScore)
                    {
                        highestScore = score;
                        bestPlayerName = name;
                        bestScoreIndex = i;
                    }
                }
                
                // Verificar si se encontró el mejor nombre
                if (string.IsNullOrEmpty(bestPlayerName) || bestPlayerName == "???")
                {
                    bestPlayerName = "Player";
                    Debug.LogWarning("Nombre del mejor jugador no encontrado, usando 'Player'");
                }
                
                Debug.Log($"Mejor puntuación encontrada en índice {bestScoreIndex}: '{bestPlayerName}' - {highestScore}");
                highScoreText.text = $"{bestPlayerName} - {highestScore} pts";
            }
            else
            {
                highScoreText.text = "¡Sé el primero en jugar!";
            }
        }
    }
    
    // BOTONES DE NAVEGACIÓN
    
    // Iniciar juego
    public void StartGame()
    {
        // Cargar primera escena de juego (escena 1)
        SceneManager.LoadScene(1);
    }
    
    // Salir del juego
    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    // FUNCIONES PARA GAME OVER
    
    // Reiniciar nivel
    public void RestartGame()
    {
        // Restaurar el tiempo ANTES de realizar las acciones
        Time.timeScale = 1f;
        
        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Volver al menú principal
    public void ReturnToMainMenu()
    {
        // Restaurar el tiempo
        Time.timeScale = 1f;
        
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            SceneManager.LoadScene(0); // Menú principal
    }
    
    // Mostrar Game Over
    public void ShowGameOverMenu(int score)
    {
        if (!isMainMenuScene)
        {
            // Buscar o crear el panel de entrada de nombre
            FindOrCreateNameInputPanel();
            
            // Asegurar que el panel está activo
            if (gameOverMenu != null)
            {
                gameOverMenu.SetActive(true);
                
                // Mostrar botones
                Transform[] buttons = new Transform[] {
                    gameOverMenu.transform.Find("RestartButton"),
                    gameOverMenu.transform.Find("MenuButton"),
                    gameOverMenu.transform.Find("SaveButton")
                };
                
                foreach (var button in buttons)
                {
                    if (button != null)
                        button.gameObject.SetActive(true);
                }
                
                // Ocultar el panel de entrada de nombre
                if (nameInputPanel != null)
                    nameInputPanel.SetActive(false);
                
                // Actualizar el texto de la puntuación
                if (scoreText != null)
                    scoreText.text = "Score: " + score;
                
                // Configurar los listeners de los botones
                SetupGameOverButtons();
            }
        }
    }
    
    // SISTEMA DE GESTIÓN DE PUNTUACIONES
    
    // Guardar puntuación
    public void SaveHighscore()
    {
        Debug.Log("Botón Save presionado");
        
        // Buscar o crear el panel de entrada de nombre
        FindOrCreateNameInputPanel();
        
        // Si aún así no existe, guardamos con nombre por defecto
        if (nameInputPanel == null)
        {
            SaveWithDefaultName();
            return;
        }
        
        // Mostrar el panel de entrada de nombre y ocultar botones
        nameInputPanel.SetActive(true);
        
        // Ocultar botones del gameOverMenu
        if (gameOverMenu != null)
        {
            // Buscar y ocultar todos los botones
            Button[] buttons = gameOverMenu.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                if (button.transform.parent != nameInputPanel.transform)
                    button.gameObject.SetActive(false);
            }
        }
        
        // Enfocar el campo de entrada
        if (playerNameInput != null)
        {
            playerNameInput.text = "";
            playerNameInput.Select();
            playerNameInput.ActivateInputField();
        }
    }
    
    // Guardar con nombre predeterminado
    private void SaveWithDefaultName()
    {
        string defaultName = "Player";
        int score = PlayerPrefs.GetInt("CurrentScore", 0);
        SaveScore(defaultName, score);
        Debug.Log("Puntuación guardada con nombre predeterminado: " + defaultName);
        
        StartCoroutine(ShowSavedMessageAndReturn());
    }
    
    // Confirmar guardar puntuación
    public void ConfirmSaveHighscore()
    {
        // Restaurar el tiempo
        Time.timeScale = 1f;
        
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            string playerName = playerNameInput.text;
            int score = PlayerPrefs.GetInt("CurrentScore", 0);
            
            // Guardar la puntuación
            SaveScore(playerName, score);
            
            // Mostrar un mensaje breve de confirmación (opcional)
            Debug.Log("¡Puntuación guardada con éxito!");
            
            // Volver al menú principal
            ReturnToMainMenu();
        }
        else
        {
            // Si no hay nombre, usar un nombre predeterminado
            string defaultName = "Player";
            int score = PlayerPrefs.GetInt("CurrentScore", 0);
            SaveScore(defaultName, score);
            ReturnToMainMenu();
        }
    }
    
    // Guardar puntuación
    private void SaveScore(string playerName, int score)
    {
        // Obtenemos la cantidad actual de puntuaciones guardadas
        int scoreCount = PlayerPrefs.GetInt("ScoreCount", 0);
        
        // Guardamos la nueva puntuación
        PlayerPrefs.SetString("ScoreName_" + scoreCount, playerName);
        PlayerPrefs.SetInt("ScoreValue_" + scoreCount, score);
        
        // Incrementamos el contador de puntuaciones
        PlayerPrefs.SetInt("ScoreCount", scoreCount + 1);
        PlayerPrefs.Save();
        
        // Actualizar el texto de mayor puntuación si estamos en el menú principal
        if (isMainMenuScene)
        {
            UpdateHighScoreDisplay();
        }
    }
    
    // Mostrar mensaje y volver
    private System.Collections.IEnumerator ShowSavedMessageAndReturn()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        ReturnToMainMenu();
    }
    
    // SISTEMA DE GESTIÓN DE PANELES Y ANIMACIONES
    
    // Funcionalidad de PanelManager para abrir paneles con animación
    public void OpenPanel(Animator anim)
    {
        if (anim == null)
            return;
            
        if (m_Open == anim)
            return;

        anim.gameObject.SetActive(true);
        var newPreviouslySelected = EventSystem.current.currentSelectedGameObject;

        anim.transform.SetAsLastSibling();

        CloseCurrent();

        m_PreviouslySelected = newPreviouslySelected;

        m_Open = anim;
        m_Open.SetBool(m_OpenParameterId, true);

        GameObject go = FindFirstEnabledSelectable(anim.gameObject);

        SetSelected(go);
    }

    // Encontrar el primer seleccionable habilitado
    static GameObject FindFirstEnabledSelectable(GameObject gameObject)
    {
        GameObject go = null;
        var selectables = gameObject.GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in selectables) {
            if (selectable.IsActive() && selectable.IsInteractable()) {
                go = selectable.gameObject;
                break;
            }
        }
        return go;
    }

    // Cerrar el panel actual
    public void CloseCurrent()
    {
        if (m_Open == null)
            return;

        m_Open.SetBool(m_OpenParameterId, false);
        SetSelected(m_PreviouslySelected);
        StartCoroutine(DisablePanelDelayed(m_Open));
        m_Open = null;
    }

    // Deshabilitar el panel con delay para permitir la animación
    IEnumerator DisablePanelDelayed(Animator anim)
    {
        bool closedStateReached = false;
        bool wantToClose = true;
        while (!closedStateReached && wantToClose)
        {
            if (!anim.IsInTransition(0))
                closedStateReached = anim.GetCurrentAnimatorStateInfo(0).IsName(k_ClosedStateName);

            wantToClose = !anim.GetBool(m_OpenParameterId);

            yield return new WaitForEndOfFrame();
        }

        if (wantToClose)
            anim.gameObject.SetActive(false);
    }

    // Establecer el objeto seleccionado
    private void SetSelected(GameObject go)
    {
        EventSystem.current.SetSelectedGameObject(go);
    }
    
    // FUNCIONES PARA EL PANEL DE ENTRADA DE NOMBRE
    
    // Configurar botones del GameOver
    public void SetupGameOverButtons()
    {
        if (gameOverMenu == null) return;
        
        // Limpiar listeners anteriores y asignar nuevos
        Button[] allButtons = gameOverMenu.GetComponentsInChildren<Button>(true);
        foreach (Button button in allButtons)
        {
            button.onClick.RemoveAllListeners();
            
            if (button.name == "RestartButton" || button.name.Contains("Restart"))
            {
                button.onClick.AddListener(RestartGame);
            }
            else if (button.name == "MenuButton" || button.name.Contains("Menu"))
            {
                button.onClick.AddListener(ReturnToMainMenu);
            }
            else if (button.name == "SaveButton" || button.name.Contains("Save"))
            {
                button.onClick.AddListener(SaveHighscore);
            }
            else if (button.name == "ConfirmButton" || button.name.Contains("Confirm"))
            {
                button.onClick.AddListener(ConfirmSaveHighscore);
            }
        }
    }
    
    // Método para buscar o crear el panel de entrada de nombre
    private void FindOrCreateNameInputPanel()
    {
        // Si ya está asignado, no hacer nada
        if (nameInputPanel != null)
            return;
            
        // Solo buscar en escenas de juego
        if (!isMainMenuScene && gameOverMenu != null)
        {
            // Buscar como hijo del gameOverMenu
            nameInputPanel = gameOverMenu.transform.Find("NameInputPanel")?.gameObject;
            
            // Buscar en la escena por nombre
            if (nameInputPanel == null)
            {
                nameInputPanel = GameObject.Find("NameInputPanel");
            }
            
            // Buscar recursivamente
            if (nameInputPanel == null)
            {
                Canvas[] canvases = FindObjectsOfType<Canvas>();
                foreach (Canvas canvas in canvases)
                {
                    Transform found = FindRecursive(canvas.transform, "NameInputPanel");
                    if (found != null)
                    {
                        nameInputPanel = found.gameObject;
                        break;
                    }
                }
            }
            
            // Crear dinámicamente si es necesario
            if (nameInputPanel == null)
            {
                nameInputPanel = CreateNameInputPanel();
            }
            
            if (nameInputPanel != null)
            {
                // Asegurar que tenemos la referencia al inputField
                if (playerNameInput == null)
                {
                    playerNameInput = nameInputPanel.GetComponentInChildren<TMP_InputField>();
                }
                
                // Configurar el botón Confirmar
                Button confirmButton = nameInputPanel.GetComponentInChildren<Button>();
                if (confirmButton != null)
                {
                    confirmButton.onClick.RemoveAllListeners();
                    confirmButton.onClick.AddListener(ConfirmSaveHighscore);
                }
                
                // Ocultar panel inicialmente
                nameInputPanel.SetActive(false);
            }
        }
    }
    
    // Crear panel de entrada de nombre dinámicamente
    private GameObject CreateNameInputPanel()
    {
        // Crear panel base
        GameObject panel = new GameObject("NameInputPanel");
        panel.transform.SetParent(gameOverMenu.transform, false);
        
        // Configurar panel
        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(300, 150);
        
        // Añadir fondo
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Crear título
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
        titleRect.anchorMax = new Vector2(0.5f, 0.9f);
        titleRect.sizeDelta = new Vector2(280, 30);
        TMP_Text titleText = titleObj.AddComponent<TMP_Text>();
        titleText.text = "Ingresa tu nombre:";
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.font = TMP_Settings.defaultFontAsset;
        
        // Crear input field
        GameObject inputObj = new GameObject("PlayerNameInput");
        inputObj.transform.SetParent(panel.transform, false);
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.1f, 0.4f);
        inputRect.anchorMax = new Vector2(0.9f, 0.6f);
        inputRect.sizeDelta = new Vector2(0, 0);
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
        // Configurar el área de texto
        GameObject textAreaObj = new GameObject("TextArea");
        textAreaObj.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = new Vector2(0, 0);
        textAreaRect.anchorMax = new Vector2(1, 1);
        textAreaRect.sizeDelta = new Vector2(0, 0);
        Image textAreaImage = textAreaObj.AddComponent<Image>();
        textAreaImage.color = new Color(0.2f, 0.2f, 0.2f);
        
        // Texto del input field
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textAreaObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.sizeDelta = new Vector2(-20, -10);
        textRect.anchoredPosition = new Vector2(10, 0);
        TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.font = TMP_Settings.defaultFontAsset;
        
        // Asignar componentes
        inputField.textComponent = text;
        
        // Botón de confirmación
        GameObject buttonObj = new GameObject("ConfirmButton");
        buttonObj.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.3f, 0.1f);
        buttonRect.anchorMax = new Vector2(0.7f, 0.25f);
        buttonRect.sizeDelta = new Vector2(0, 0);
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.9f);
        
        // Texto del botón
        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;
        TMP_Text buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Confirmar";
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.font = TMP_Settings.defaultFontAsset;
        
        // Añadir listener
        button.onClick.AddListener(ConfirmSaveHighscore);
        
        // Asignar el input field
        playerNameInput = inputField;
        
        // Ocultar panel
        panel.SetActive(false);
        
        return panel;
    }

    // Método auxiliar para buscar recursivamente
    private Transform FindRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
                
            Transform found = FindRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    // Modifica el Update() en MenuController.cs (añádelo si no existe)
    private void Update()
    {
        // Verifica si estamos en la escena del menú principal
        if (isMainMenuScene)
        {
            // Verifica si hay una solicitud para actualizar el high score
            if (PlayerPrefs.GetInt("UpdateHighScore", 0) == 1)
            {
                // Resetea la flag
                PlayerPrefs.SetInt("UpdateHighScore", 0);
                PlayerPrefs.Save();
                
                // NUEVO: Mostrar todas las puntuaciones guardadas
                DumpAllScores();
                
                // Actualiza el display de high scores
                UpdateHighScoreDisplay();
                Debug.Log("High Score actualizado después de volver al menú principal");
            }
        }
    }
    
    // NUEVO: Método para mostrar todas las puntuaciones guardadas
    private void DumpAllScores()
    {
        int scoreCount = PlayerPrefs.GetInt("ScoreCount", 0);
        Debug.Log("===== TODAS LAS PUNTUACIONES GUARDADAS =====");
        
        for (int i = 0; i < scoreCount; i++)
        {
            string name = PlayerPrefs.GetString("ScoreName_" + i, "???");
            int score = PlayerPrefs.GetInt("ScoreValue_" + i, 0);
            Debug.Log($"[{i}] {name}: {score} puntos");
        }
        
        Debug.Log("===========================================");
    }
}

