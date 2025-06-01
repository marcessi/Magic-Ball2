using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

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
        
        // Obtener el nombre del jugador si hay un campo de entrada
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            playerName = playerNameInput.text;
        }
        
        // Guardar puntuación en PlayerPrefs
        int scoreCount = PlayerPrefs.GetInt("ScoreCount", 0);
        PlayerPrefs.SetString("ScoreName_" + scoreCount, playerName);
        PlayerPrefs.SetInt("ScoreValue_" + scoreCount, currentScore);
        PlayerPrefs.SetInt("ScoreCount", scoreCount + 1);
        PlayerPrefs.Save();
        
        Debug.Log("Puntuación guardada: " + playerName + " - " + currentScore);
        
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
        
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            SceneManager.LoadScene(0);
    }
}