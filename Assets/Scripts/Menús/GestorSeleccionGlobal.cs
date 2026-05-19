using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GestorSeleccionGlobal : MonoBehaviour {
    [Header("Botones de Listo")]
    [SerializeField] private BotonListo botonListoJugador1;
    [SerializeField] private BotonListo botonListoJugador2;

    [Header("Configuración de Escena")]
    [SerializeField] private string nombreEscenaNivel;

    public void VerificarJugadoresListos() {
        if (botonListoJugador1 == null || botonListoJugador2 == null) return;

        // Condición estricta: Ambos scripts de botón tienen que devolver true
        if (botonListoJugador1.EstaListo && botonListoJugador2.EstaListo) {
            IniciarPartida();
        }
    }

    private void IniciarPartida() {
        Debug.Log("¡Ambos listos! Cargando juego...");
        SceneManager.LoadScene(nombreEscenaNivel);
    }
}