using UnityEngine;
using UnityEngine.SceneManagement; 

public class MenuManager : MonoBehaviour
{
    [SerializeField] private string nombreDeLaSiguienteEscena;
    public void CargarEscena()
    {
        int i = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(nombreDeLaSiguienteEscena);
    }

    public void SalirDelJuego()
    {
        Application.Quit();
    }
}