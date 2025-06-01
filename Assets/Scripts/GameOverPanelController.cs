using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

public class GameOverPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button confirmButton;

    private int currentScore = 0;

    private void Awake()
    {
        // NUEVO: Desactivar GameOverButton para evitar conflictos
        GameOverButton[] gameOverButtons = GetComponentsInChildren<GameOverButton>(true);
        foreach (GameOverButton gob in gameOverButtons)
        {
            gob.enabled = false;
            Debug.Log("Desactivado componente GameOverButton en " + gob.gameObject.name);
        }
        
        // Ocultar el panel de entrada de nombre al inicio
        if (nameInputPanel != null)
            nameInputPanel.SetActive(false);
            
        // Asignar listeners a los botones
        SetupButtons();
    }

    // Método para mostrar el panel con una puntuación específica
    public void Show(int score)
    {
        Debug.Log("GameOverPanelController.Show() llamado con puntuación: " + score);
        
        // IMPORTANTE: Asignar la puntuación correctamente
        currentScore = score;
        
        // Guardar la puntuación actual en PlayerPrefs para recuperarla si es necesario
        PlayerPrefs.SetInt("CurrentScore", score);
        
        // Actualizar texto de puntuación
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
            Debug.Log("ScoreText actualizado: " + scoreText.text);
        }
        else
        {
            Debug.LogError("ScoreText es null - no asignado en el Inspector");
        }
        
        // Activar panel y asegurar que está visible
        gameObject.SetActive(true);
        
        // Asegurarse que el Canvas está en primer plano
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 100; // Asegurar que está al frente
            Debug.Log("Canvas sorting order ajustado a 100");
        }
        
        // Asegurarse que el panel de input está oculto
        if (nameInputPanel != null)
            nameInputPanel.SetActive(false);
            
        // Mostrar los botones principales
        ShowMainButtons(true);
        
        Debug.Log("GameOverPanel activado correctamente");
    }
    
    // Inicializar manualmente las referencias
    public void InitializeManually()
    {
        Debug.Log("Inicializando GameOverPanelController manualmente");
        
        // Buscar referencias si no están asignadas
        if (scoreText == null)
            scoreText = GetComponentInChildren<TMP_Text>(true);
            
        if (nameInputPanel == null)
            nameInputPanel = transform.Find("NameInputPanel")?.gameObject;
            
        if (playerNameInput == null && nameInputPanel != null)
            playerNameInput = nameInputPanel.GetComponentInChildren<TMP_InputField>(true);
            
        // Buscar botones por nombre si no están asignados
        if (restartButton == null)
            restartButton = transform.Find("RestartButton")?.GetComponent<Button>();
            
        if (menuButton == null)
            menuButton = transform.Find("MenuButton")?.GetComponent<Button>();
            
        if (saveButton == null)
            saveButton = transform.Find("SaveButton")?.GetComponent<Button>();
            
        if (confirmButton == null && nameInputPanel != null)
            confirmButton = nameInputPanel.transform.Find("ConfirmButton")?.GetComponent<Button>();
            
        // Configurar botones
        SetupButtons();
        
        // Ocultar panel de entrada
        if (nameInputPanel != null)
            nameInputPanel.SetActive(false);
            
        Debug.Log("Inicialización manual completada");
    }
    
    // Configurar los botones
    private void SetupButtons()
    {
        //Debug.Log("Configurando botones del GameOverPanel");
        
        //// Restart Button
        //if (restartButton != null)
        //{
        //    restartButton.onClick.RemoveAllListeners();
        //    restartButton.onClick.AddListener(() => {
        //        Debug.Log("Botón RESTART presionado");
        //        RestartGame();
        //    });
        //    Debug.Log("Botón Restart configurado");
        //}
        //else
        //{
        //    Debug.LogError("restartButton no asignado");
        //}
        
        //// Menu Button
        //if (menuButton != null)
        //{
        //    menuButton.onClick.RemoveAllListeners();
        //    menuButton.onClick.AddListener(() => {
        //        Debug.Log("Botón MENU presionado");
        //        ReturnToMainMenu();
        //    });
        //    Debug.Log("Botón Menu configurado");
        //}
        //else
        //{
        //    Debug.LogError("menuButton no asignado");
        //}
        
        //// Save Button
        //if (saveButton != null)
        //{
        //    saveButton.onClick.RemoveAllListeners();
        //    saveButton.onClick.AddListener(() => {
        //        Debug.Log("Botón SAVE presionado");
        //        ShowNameInputPanel();
        //    });
        //    Debug.Log("Botón Save configurado");
        //}
        //else
        //{
        //    Debug.LogError("saveButton no asignado");
        //}
        
        //// Confirm Button
        //if (confirmButton != null)
        //{
        //    confirmButton.onClick.RemoveAllListeners();
        //    confirmButton.onClick.AddListener(() => {
        //        Debug.Log("Botón CONFIRM presionado");
        //        SaveScore();
        //    });
        //    Debug.Log("Botón Confirm configurado");
        //}
        //else
        //{
        //    Debug.LogError("confirmButton no asignado");
        //}
    }

    public void OnRestartButtonClick()
{
    Debug.Log("BOTÓN RESTART PULSADO - EJECUCIÓN CONFIRMADA");
    RestartGame();
}

public void OnMenuButtonClick()
{
    Debug.Log("BOTÓN MENU PULSADO - EJECUCIÓN CONFIRMADA");
    ReturnToMainMenu();
}

public void OnSaveButtonClick()
{
    Debug.Log("BOTÓN SAVE PULSADO - EJECUCIÓN CONFIRMADA");
    ShowNameInputPanel();
}

public void OnConfirmButtonClick()
{
    Debug.Log("BOTÓN CONFIRM PULSADO - EJECUCIÓN CONFIRMADA");
    
    // NUEVO: Desactivar todos los botones para evitar clics múltiples
    if (restartButton != null) restartButton.interactable = false;
    if (menuButton != null) menuButton.interactable = false;
    if (saveButton != null) saveButton.interactable = false;
    if (confirmButton != null) confirmButton.interactable = false;
    
    SaveScore();
}
    
    // Mostrar el panel de entrada de nombre
    private void ShowNameInputPanel()
    {
        // Mostrar panel de entrada de nombre
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(true);
            
            // Ocultar botones principales
            ShowMainButtons(false);
            
            // Enfocar el campo de entrada
            if (playerNameInput != null)
            {
                playerNameInput.text = "";
                playerNameInput.Select();
                playerNameInput.ActivateInputField();
            }
        }
    }
    
    // Mostrar/ocultar botones principales
    private void ShowMainButtons(bool show)
    {
        if (restartButton != null) restartButton.gameObject.SetActive(show);
        if (menuButton != null) menuButton.gameObject.SetActive(show);
        if (saveButton != null) saveButton.gameObject.SetActive(show);
    }
    
    // Guardar la puntuación
    private void SaveScore()
    {
        string playerName = "Player";
        
        // IMPORTANTE: Obtener la puntuación directamente del GameManager
        int actualScore = 0;
        if (GameManager.Instance != null)
        {
            actualScore = GameManager.Instance.GetCurrentScore();
            Debug.Log("Puntuación obtenida directamente del GameManager: " + actualScore);
        }
        else
        {
            actualScore = PlayerPrefs.GetInt("CurrentScore", currentScore);
            Debug.Log("Puntuación obtenida de PlayerPrefs: " + actualScore);
        }
        
        // Usar la puntuación más alta entre las disponibles
        if (actualScore > currentScore)
        {
            currentScore = actualScore;
            Debug.Log("Usando puntuación mayor: " + currentScore);
        }
        
        Debug.Log("Guardando puntuación...");
        
        // MÉTODO MEJORADO PARA OBTENER EL TEXTO DEL INPUT
        if (playerNameInput != null)
        {
            Debug.Log("Input field encontrado: " + playerNameInput.name);
            
            // 1. FORZAR ACTUALIZACIÓN DEL CAMPO DE TEXTO
            playerNameInput.ForceLabelUpdate();
            
            // 2. SOLUCIÓN PARA OBTENER EL TEXTO DE MÚLTIPLES FORMAS
            string textFromInputField = playerNameInput.text;
            string textFromTextComponent = playerNameInput.textComponent != null ? 
                                           playerNameInput.textComponent.text : "";
            string textFromPlaceholder = playerNameInput.placeholder != null ? 
                                        (playerNameInput.placeholder as TMP_Text)?.text : "";
            
            Debug.Log($"Texto del InputField: '{textFromInputField}'");
            Debug.Log($"Texto del TextComponent: '{textFromTextComponent}'");
            Debug.Log($"Texto del Placeholder: '{textFromPlaceholder}'");
            
            // 3. USAR LA MEJOR FUENTE DE TEXTO DISPONIBLE
            string bestTextValue = !string.IsNullOrWhiteSpace(textFromInputField) ? textFromInputField : 
                                  (!string.IsNullOrWhiteSpace(textFromTextComponent) ? textFromTextComponent : "");
            
            if (!string.IsNullOrWhiteSpace(bestTextValue))
            {
                playerName = bestTextValue.Trim();
                Debug.Log("Nombre final del jugador: '" + playerName + "'");
            }
            else
            {
                // 4. VERIFICAR SI HAY TEXTO ACCESIBLE A TRAVÉS DE OTRAS PROPIEDADES
                try {
                    var inputFieldType = playerNameInput.GetType();
                    var textProperty = inputFieldType.GetProperty("m_Text", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (textProperty != null)
                    {
                        string textValue = textProperty.GetValue(playerNameInput) as string;
                        if (!string.IsNullOrWhiteSpace(textValue))
                        {
                            playerName = textValue.Trim();
                            Debug.Log("Nombre recuperado mediante reflexión: '" + playerName + "'");
                        }
                    }
                }
                catch (System.Exception ex) {
                    Debug.LogWarning("Error al intentar obtener el texto mediante reflexión: " + ex.Message);
                }
            }
        }
        else
        {
            Debug.LogError("playerNameInput es null - verificando alternativas...");
            
            // 5. BUSCAR EL INPUT FIELD EN TIEMPO DE EJECUCIÓN SI LA REFERENCIA ES NULL
            if (nameInputPanel != null)
            {
                TMP_InputField foundInputField = nameInputPanel.GetComponentInChildren<TMP_InputField>(true);
                if (foundInputField != null)
                {
                    string textValue = foundInputField.text;
                    if (!string.IsNullOrWhiteSpace(textValue))
                    {
                        playerName = textValue.Trim();
                        Debug.Log("Nombre encontrado mediante búsqueda en tiempo de ejecución: '" + playerName + "'");
                    }
                }
            }
        }
        
        // 6. LEER NOMBRE GUARDADO PREVIAMENTE COMO RESPALDO
        if (playerName == "Player" && !string.IsNullOrEmpty(PlayerPrefs.GetString("LastPlayerName", "")))
        {
            string lastUsedName = PlayerPrefs.GetString("LastPlayerName");
            Debug.Log("Usando último nombre guardado: " + lastUsedName);
            playerName = lastUsedName;
        }
        
        // GUARDAR LA PUNTUACIÓN - Modificado para usar actualScore
        int scoreCount = PlayerPrefs.GetInt("ScoreCount", 0);
        PlayerPrefs.SetString("ScoreName_" + scoreCount, playerName);
        PlayerPrefs.SetInt("ScoreValue_" + scoreCount, currentScore); // Usar currentScore actualizado
        PlayerPrefs.SetInt("ScoreCount", scoreCount + 1);
        PlayerPrefs.SetString("LastPlayerName", playerName);
        
        // Asegurar que se guarda
        PlayerPrefs.Save();
        
        Debug.Log("¡Puntuación guardada correctamente!: " + playerName + " - " + currentScore);
        
        // Volver al menú principal
        ReturnToMainMenu();
    }
    
    // Reiniciar el nivel actual
    public void RestartGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Reiniciando el juego...");
        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Volver al menú principal
    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        
        // Añadir una flag para indicar que se debe actualizar el high score
        PlayerPrefs.SetInt("UpdateHighScore", 1);
        
        // NUEVO: Evitar que se procesen más eventos de botones
        GameObject eventSystem = GameObject.FindObjectOfType<EventSystem>()?.gameObject;
        if (eventSystem != null)
            eventSystem.SetActive(false);
        
        // NUEVO: Usar un pequeño retraso para evitar doble activación
        StartCoroutine(ReturnToMainMenuDelayed());
    }

    // NUEVO: Método para volver al menú principal con un pequeño retraso
    private IEnumerator ReturnToMainMenuDelayed()
    {
        // Esperar un pequeño tiempo para evitar que se procesen más eventos
        yield return new WaitForSeconds(0.1f);
        
        // Guardar cambios antes de cambiar de escena
        PlayerPrefs.Save();
        
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            SceneManager.LoadScene(0);
    }
}