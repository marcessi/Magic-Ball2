using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

    public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu, playMenu, creditsMenu;
    public void ShowMain()    { mainMenu.SetActive(true); playMenu.SetActive(false); creditsMenu.SetActive(false); }
    public void ShowPlay()    { mainMenu.SetActive(false); playMenu.SetActive(true); creditsMenu.SetActive(false); }
    public void ShowCredits(){ mainMenu.SetActive(false); playMenu.SetActive(false); creditsMenu.SetActive(true); }
}

   