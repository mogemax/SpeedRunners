using UnityEngine;
using UnityEngine.UI;

// Creamos una estructura para agrupar el Sprite del menú y su Prefab del juego
[System.Serializable]
public struct AspectoPersonaje {
    public Sprite spriteUI;
    public GameObject prefabJugador;
}

public class ControladorPanelJugador : MonoBehaviour {
    [Header("Configuración del Jugador")]
    public int numeroJugador = 1; // 1 para el azul, 2 para el rojo

    [Header("UI del Panel")]
    [SerializeField] private Image imagenPersonajeUI; // Donde se mostrará el sprite
    [SerializeField] private Button botonAspecto; // El botón físico para cambiar de aspecto

    [Header("Catálogo de Aspectos")]
    [SerializeField] private AspectoPersonaje[] aspectosDisponibles;

    private int indiceActual = 0;

    void Start() {
        // Mostramos el primer aspecto por defecto
        ActualizarVisual();
    }

    // Esta función va en el botón de ASPECTO
    public void CambiarAspecto() {
        if (aspectosDisponibles.Length == 0) return;

        indiceActual++;
        if (indiceActual >= aspectosDisponibles.Length) {
            indiceActual = 0; // Volvemos al inicio de la lista
        }

        ActualizarVisual();
    }

    private void ActualizarVisual() {
        if (aspectosDisponibles.Length == 0 || imagenPersonajeUI == null) return;

        // Cambiamos el sprite en la UI
        imagenPersonajeUI.sprite = aspectosDisponibles[indiceActual].spriteUI;

        // Guardamos el prefab seleccionado en los datos estáticos
        if (numeroJugador == 1)
            DatosSeleccion.prefabJugador1 = aspectosDisponibles[indiceActual].prefabJugador;
        else
            DatosSeleccion.prefabJugador2 = aspectosDisponibles[indiceActual].prefabJugador;
    }

    // Esta función la llamará el script del Botón de Listo para congelar/descongelar el aspecto
    public void BloquearCambioAspecto(bool bloquear) {
        if (botonAspecto != null) {
            botonAspecto.interactable = !bloquear;
        }
    }
}