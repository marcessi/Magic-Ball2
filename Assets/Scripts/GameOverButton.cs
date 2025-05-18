using UnityEngine;
using UnityEngine.UI;

public class GameOverButton : MonoBehaviour
{
    public enum ButtonType
    {
        Restart,
        MainMenu,
        SaveScore
    }

    public ButtonType type;

    void OnEnable()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            
            switch (type)
            {
                case ButtonType.Restart:
                    btn.onClick.AddListener(OnRestartClick);
                    break;
                    
                case ButtonType.MainMenu:
                    btn.onClick.AddListener(OnMainMenuClick);
                    break;
                    
                case ButtonType.SaveScore:
                    btn.onClick.AddListener(OnSaveScoreClick);
                    break;
            }
            
            Debug.Log($"Botón {gameObject.name} configurado con tipo: {type}");
        }
        else
        {
            Debug.LogError($"No se encontró componente Button en {gameObject.name}");
        }
    }

    private void OnRestartClick()
    {
        Debug.Log("Botón RESTART presionado - ejecutando acción");
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMainMenuClick()
    {
        Debug.Log("Botón MENU PRINCIPAL presionado - ejecutando acción");
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void OnSaveScoreClick()
    {
        Debug.Log("Botón GUARDAR PUNTUACIÓN presionado - ejecutando acción");
        MenuController mc = FindObjectOfType<MenuController>();
        if (mc != null)
            mc.SaveHighscore();
        else
        {
            // Fallback por si no se encuentra el MenuController
            string defaultName = "Player";
            int score = PlayerPrefs.GetInt("CurrentScore", 0);
            
            // Guardar la puntuación
            int scoreCount = PlayerPrefs.GetInt("ScoreCount", 0);
            PlayerPrefs.SetString("ScoreName_" + scoreCount, defaultName);
            PlayerPrefs.SetInt("ScoreValue_" + scoreCount, score);
            PlayerPrefs.SetInt("ScoreCount", scoreCount + 1);
            PlayerPrefs.Save();
            
            // Volver al menú principal
            if (GameManager.Instance != null)
                GameManager.Instance.GoToMainMenu();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}