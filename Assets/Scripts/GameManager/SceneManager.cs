using UnityEngine;
using UnityEngine.SceneManagement; 

public class MenuManager : MonoBehaviour
{
   
    public void CargarEscena()
    {
        int i = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(i+1);
    }

    public void SalirDelJuego()
    {
        Application.Quit();
    }
}
