using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Cambiar de UnityEngine.UI a TMPro
using System.Collections.Generic; // Añade esta línea para resolver los errores
using UnityEngine.UI; // Añade esta línea para encontrar Button

public class MenuController : MonoBehaviour
{
    [Header("Menú Principal")]
    public GameObject mainMenu;
    public GameObject playMenu;
    public GameObject creditsMenu;
    public GameObject highscoresMenu;
    
    [Header("Game Over")]
    public GameObject gameOverMenu;
    public TMP_InputField playerNameInput; // Cambiar de InputField a TMP_InputField
    public TMP_Text scoreText; // Cambiar de Text a TMP_Text
    
    [Header("Medal Images")]
    public Sprite goldMedalSprite;
    public Sprite silverMedalSprite;
    public Sprite bronzeMedalSprite;
    
    // Define estos colores al inicio de la clase
    private Color rowColor1 = new Color(0.25f, 0.25f, 0.25f, 0.5f); // Color oscuro
    private Color rowColor2 = new Color(0.4f, 0.4f, 0.4f, 0.5f); // Color un poco más claro
    
    // Añade esta variable a tu clase
    public GameObject nameInputPanel;

    private void Start()
    {
        // Asegurar que el juego está en velocidad normal al iniciarse
        Time.timeScale = 1;
        
        // Buscar o crear el panel de entrada de nombre
        FindOrCreateNameInputPanel();
        
        // Mostrar menú principal por defecto
        ShowMain();
        
        // Asignar listeners a los botones del GameOver programáticamente
        SetupGameOverButtons();
        
        // Crear o actualizar las referencias a los sprites de medalla si no existen
        if (goldMedalSprite == null || silverMedalSprite == null || bronzeMedalSprite == null)
        {
            Debug.LogWarning("Faltan sprites de medallas. Por favor, asígnalos en el inspector.");
        }
        
        // Nuevos logs para depuración
        Debug.Log("MenuController iniciado. gameOverMenu asignado: " + (gameOverMenu != null));
        Debug.Log("nameInputPanel asignado: " + (nameInputPanel != null));
        Debug.Log("playerNameInput asignado: " + (playerNameInput != null));
    }
    
    // Métodos para el menú principal
    public void ShowMain()
    { 
        if (mainMenu != null)
            mainMenu.SetActive(true);
        
        if (playMenu != null)
            playMenu.SetActive(false); 
        
        if (creditsMenu != null)
            creditsMenu.SetActive(false);
        
        if (highscoresMenu != null)
            highscoresMenu.SetActive(false);
        
        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);
    }
    
    public void ShowPlay()
    { 
        if (mainMenu != null)
            mainMenu.SetActive(false); 
        
        // Solo activar playMenu si existe
        if (playMenu != null)
            playMenu.SetActive(true);
        
        if (creditsMenu != null)
            creditsMenu.SetActive(false);
        
        if (highscoresMenu != null)
            highscoresMenu.SetActive(false);
    }
    
    public void ShowCredits()
    { 
        if (mainMenu != null)
            mainMenu.SetActive(false);
        
        if (playMenu != null)
            playMenu.SetActive(false);
        
        if (creditsMenu != null)
            creditsMenu.SetActive(true);
        
        if (highscoresMenu != null)
            highscoresMenu.SetActive(false);
    }
    
    public void ShowHighscores()
    {
        if (mainMenu != null)
            mainMenu.SetActive(false);
        
        if (playMenu != null)
            playMenu.SetActive(false);
        
        if (creditsMenu != null)
            creditsMenu.SetActive(false);
        
        if (highscoresMenu != null)
            highscoresMenu.SetActive(true);
    
        // Cargar las puntuaciones
        LoadHighscores();
    }
    
    // Botones de navegación
    public void StartGame()
    {
        // Cargar primera escena de juego (asumiendo que es la escena 1)
        SceneManager.LoadScene(1);
    }
    
    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    // Métodos para el menú de Game Over
    public void RestartGame()
    {
        // Restaurar el tiempo ANTES de realizar las acciones
        Time.timeScale = 1f;
        
        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void ReturnToMainMenu()
    {
        // Restaurar el tiempo
        Time.timeScale = 1f;
        
        // IMPORTANTE: Antes de salir al menú principal, restaurar el estado de los botones
        if (gameOverMenu != null)
        {
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
        }
        
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            SceneManager.LoadScene(0); // Menú principal
    }
    
    // Modifica el método SaveHighscore()
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
        else
        {
            // Intentar encontrar el playerNameInput
            playerNameInput = nameInputPanel.GetComponentInChildren<TMP_InputField>();
            if (playerNameInput != null)
            {
                playerNameInput.text = "";
                playerNameInput.Select();
                playerNameInput.ActivateInputField();
            }
        }
    }

    private void HideGameOverButtons()
    {
        if (gameOverMenu == null) return;
        
        Transform[] buttons = new Transform[] {
            gameOverMenu.transform.Find("RestartButton"),
            gameOverMenu.transform.Find("MenuButton"),
            gameOverMenu.transform.Find("SaveButton")
        };
        
        foreach (var button in buttons)
        {
            if (button != null)
                button.gameObject.SetActive(false);
        }
    }

    private void SaveWithDefaultName()
    {
        string defaultName = "Player";
        int score = PlayerPrefs.GetInt("CurrentScore", 0);
        SaveScore(defaultName, score);
        Debug.Log("Puntuación guardada con nombre predeterminado: " + defaultName);
        
        // IMPORTANTE: No llamar directamente a ReturnToMainMenu aquí
        // Mostrar mensaje de confirmación y esperar un momento
        StartCoroutine(ShowSavedMessageAndReturn());
    }

    private System.Collections.IEnumerator ShowSavedMessageAndReturn()
    {
        // Aquí podrías mostrar un mensaje temporal de "¡Puntuación guardada!"
        
        // Esperar un momento antes de volver al menú
        yield return new WaitForSecondsRealtime(1.5f);
        
        // Ahora sí, volver al menú principal
        ReturnToMainMenu();
    }
    
    // Añade este nuevo método para el botón confirmar
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
            // Si no hay nombre, mostrar un mensaje de error o usar un nombre predeterminado
            string defaultName = "Player";
            int score = PlayerPrefs.GetInt("CurrentScore", 0);
            SaveScore(defaultName, score);
            ReturnToMainMenu();
        }
    }
    
    // Sistema de puntuaciones
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
    }
    
    // Método para mostrar highscores con TMP
    private void LoadHighscores()
    {
        Transform scoreContainer = highscoresMenu.transform.Find("ScoreContainer");
        if (scoreContainer == null)
        {
            Debug.LogWarning("No se encontró el contenedor de puntuaciones");
            return;
        }
        
        foreach (Transform child in scoreContainer)
        {
            if (child.name != "ScoreRowTemplate")
                Destroy(child.gameObject);
        }
        
        Transform template = scoreContainer.Find("ScoreRowTemplate");
        if (template == null)
        {
            Debug.LogWarning("No se encontró la plantilla de fila de puntuación");
            return;
        }
        
        template.gameObject.SetActive(false);
        
        int scoreCount = PlayerPrefs.GetInt("ScoreCount", 0);
        List<KeyValuePair<string, int>> scores = new List<KeyValuePair<string, int>>();
        
        for (int i = 0; i < scoreCount; i++)
        {
            string name = PlayerPrefs.GetString("ScoreName_" + i, "???");
            int score = PlayerPrefs.GetInt("ScoreValue_" + i, 0);
            scores.Add(new KeyValuePair<string, int>(name, score));
        }
        
        scores.Sort((x, y) => y.Value.CompareTo(x.Value));
        
        int displayCount = Mathf.Min(scores.Count, 10);
        
        // Modifica el bucle for que crea las filas
        for (int i = 0; i < displayCount; i++)
        {
            Transform newRow = Instantiate(template, scoreContainer);
            newRow.gameObject.SetActive(true);
            newRow.name = "ScoreRow_" + i;
            
            // Aplicar color alternado
            Image rowBackground = newRow.GetComponent<Image>();
            if (rowBackground == null)
            {
                // Añadir imagen de fondo si no existe
                rowBackground = newRow.gameObject.AddComponent<Image>();
                rowBackground.color = (i % 2 == 0) ? rowColor1 : rowColor2;
            }
            else
            {
                rowBackground.color = (i % 2 == 0) ? rowColor1 : rowColor2;
            }
            
            // Obtener referencias a los textos
            TMP_Text rankText = newRow.Find("RankText").GetComponent<TMP_Text>();
            TMP_Text nameText = newRow.Find("NameText").GetComponent<TMP_Text>();
            TMP_Text scoreText = newRow.Find("ScoreText").GetComponent<TMP_Text>();
            
            // Verificar si necesitamos mostrar una medalla
            Image medalImage = newRow.Find("MedalImage")?.GetComponent<Image>();
            
            // Si estamos en el top 3 y tenemos una imagen para medalla
            if (i < 3 && medalImage != null)
            {
                // Establecer la medalla según la posición
                if (i == 0 && goldMedalSprite != null)
                {
                    medalImage.sprite = goldMedalSprite;
                    medalImage.gameObject.SetActive(true);
                    if (rankText != null) rankText.gameObject.SetActive(false);
                }
                else if (i == 1 && silverMedalSprite != null)
                {
                    medalImage.sprite = silverMedalSprite;
                    medalImage.gameObject.SetActive(true);
                    if (rankText != null) rankText.gameObject.SetActive(false);
                }
                else if (i == 2 && bronzeMedalSprite != null)
                {
                    medalImage.sprite = bronzeMedalSprite;
                    medalImage.gameObject.SetActive(true);
                    if (rankText != null) rankText.gameObject.SetActive(false);
                }
            }
            else
            {
                // Para posiciones 4+ mostrar el número
                if (medalImage != null) medalImage.gameObject.SetActive(false);
                if (rankText != null) 
                {
                    rankText.gameObject.SetActive(true);
                    rankText.text = (i + 1).ToString();
                }
            }
            
            // Establecer el nombre y puntuación
            if (nameText) nameText.text = scores[i].Key;
            if (scoreText) scoreText.text = scores[i].Value.ToString();
        }
        
        // Eliminar toda la sección de creación del encabezado y solo verificar si existe
        Transform headerRow = highscoresMenu.transform.Find("HeaderRow");
        if (headerRow == null)
        {
            Debug.LogWarning("El encabezado 'HeaderRow' no existe. Por favor, créelo manualmente en el editor.");
        }
        
        // Añadir línea separadora después del encabezado
        GameObject separator = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        separator.transform.SetParent(highscoresMenu.transform, false);
        separator.GetComponent<Image>().color = new Color(1f, 0.8f, 0.2f); // Color dorado
        RectTransform sepRect = separator.GetComponent<RectTransform>();
        sepRect.sizeDelta = new Vector2(350, 2); // Ancho y alto de la línea
        // Posicionarlo entre el encabezado y el contenedor
        separator.transform.SetSiblingIndex(scoreContainer.GetSiblingIndex());
    }

    // Añadir este método y llamarlo desde Start() y también cuando el GameOver se active
    public void SetupGameOverButtons()
    {
        if (gameOverMenu == null) return;
        
        Debug.Log("Configurando botones del Game Over...");
        
        // Limpiar listeners anteriores y asignar nuevos
        Button[] allButtons = gameOverMenu.GetComponentsInChildren<Button>(true);
        foreach (Button button in allButtons)
        {
            button.onClick.RemoveAllListeners();
            
            if (button.name == "RestartButton" || button.name.Contains("Restart"))
            {
                button.onClick.AddListener(RestartGame);
                Debug.Log("Listener de Restart asignado a: " + button.name);
            }
            else if (button.name == "MenuButton" || button.name.Contains("Menu"))
            {
                button.onClick.AddListener(ReturnToMainMenu);
                Debug.Log("Listener de Menu asignado a: " + button.name);
            }
            else if (button.name == "SaveButton" || button.name.Contains("Save"))
            {
                button.onClick.AddListener(SaveHighscore);
                Debug.Log("Listener de Save asignado a: " + button.name);
            }
            else if (button.name == "ConfirmButton" || button.name.Contains("Confirm"))
            {
                button.onClick.AddListener(ConfirmSaveHighscore);
                Debug.Log("Listener de Confirm asignado a: " + button.name);
            }
        }
    }

    // Método para mostrar el GameOver correctamente
    public void ShowGameOverMenu(int score)
    {
        // Buscar o crear el panel de entrada de nombre
        FindOrCreateNameInputPanel();
        
        // Asegurar que el panel está activo
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
            
            // Mostrar TODOS los botones y asegurar que estén activos
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

    // Añadir este nuevo método para crear el panel dinámicamente
    private GameObject CreateNameInputPanel()
    {
        // Crear el panel de entrada
        GameObject panel = new GameObject("NameInputPanel");
        panel.transform.SetParent(gameOverMenu.transform, false);
        
        // Configurar como panel
        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(300, 150);
        
        // Añadir imagen de fondo
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
        
        // Crear campo de entrada
        GameObject inputObj = new GameObject("PlayerNameInput");
        inputObj.transform.SetParent(panel.transform, false);
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.1f, 0.4f);
        inputRect.anchorMax = new Vector2(0.9f, 0.6f);
        inputRect.sizeDelta = new Vector2(0, 0);
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
        // Configurar componentes visuales del TMP_InputField
        GameObject textAreaObj = new GameObject("TextArea");
        textAreaObj.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = new Vector2(0, 0);
        textAreaRect.anchorMax = new Vector2(1, 1);
        textAreaRect.sizeDelta = new Vector2(0, 0);
        Image textAreaImage = textAreaObj.AddComponent<Image>();
        textAreaImage.color = new Color(0.2f, 0.2f, 0.2f);
        
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
        
        // Asignar componentes al InputField
        inputField.textComponent = text;
        
        // Crear botón de confirmación
        GameObject buttonObj = new GameObject("ConfirmButton");
        buttonObj.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.3f, 0.1f);
        buttonRect.anchorMax = new Vector2(0.7f, 0.25f);
        buttonRect.sizeDelta = new Vector2(0, 0);
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.9f);
        
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
        
        // Añadir listener al botón
        button.onClick.AddListener(ConfirmSaveHighscore);
        
        // Asignar el inputField a la variable del controlador
        playerNameInput = inputField;
        
        // Esconder el panel inicialmente
        panel.SetActive(false);
        
        return panel;
    }

    // Método para buscar o crear el panel de entrada de nombre
    private void FindOrCreateNameInputPanel()
    {
        // Si ya está asignado, no hacer nada
        if (nameInputPanel != null)
            return;

        Debug.Log("Buscando nameInputPanel...");
        
        // 1. Buscar como hijo del gameOverMenu
        if (gameOverMenu != null)
        {
            nameInputPanel = gameOverMenu.transform.Find("NameInputPanel")?.gameObject;
        }
        
        // 2. Buscar en la escena por nombre
        if (nameInputPanel == null)
        {
            nameInputPanel = GameObject.Find("NameInputPanel");
        }
        
        // 3. Buscar recursivamente en la jerarquía del canvas
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
        
        // 4. Si aún no se encuentra, crearlo dinámicamente
        if (nameInputPanel == null && gameOverMenu != null)
        {
            Debug.Log("Creando nameInputPanel dinámicamente...");
            nameInputPanel = CreateNameInputPanel();
        }
        
        if (nameInputPanel != null)
        {
            Debug.Log("nameInputPanel asignado correctamente");
            
            // Asegurar que tenemos la referencia al inputField
            if (playerNameInput == null)
            {
                playerNameInput = nameInputPanel.GetComponentInChildren<TMP_InputField>();
                if (playerNameInput == null)
                    Debug.LogWarning("No se encontró un TMP_InputField en el nameInputPanel");
            }
            
            // AÑADIR ESTO: Configurar el botón Confirmar
            Button confirmButton = nameInputPanel.GetComponentInChildren<Button>();
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmSaveHighscore);
                Debug.Log("Listener de ConfirmSaveHighscore asignado al botón");
            }
            else
            {
                Debug.LogWarning("No se encontró un botón en el nameInputPanel");
            }
        }
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
}

