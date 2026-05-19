using UnityEngine;
using UnityEngine.EventSystems; // Necesario para detectar el ratón

public class IndicadoresBoton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Sprites / Indicadores Flanqueantes")]
    [Tooltip("Arrastra el GameObject del primer sprite (ej. Flecha Izquierda)")]
    [SerializeField] private GameObject indicadorIzquierdo;

    [Tooltip("Arrastra el GameObject del segundo sprite (ej. Flecha Derecha)")]
    [SerializeField] private GameObject indicadorDerecho;

    void Start() {
        // Nos aseguramos de que los dos sprites empiecen apagados al iniciar el juego
        AlternarIndicadores(false);
    }

    // Se ejecuta automáticamente cuando el ratón ENTRA al área del botón
    public void OnPointerEnter(PointerEventData eventData) {
        AlternarIndicadores(true);
    }

    // Se ejecuta automáticamente cuando el ratón SALE del área del botón
    public void OnPointerExit(PointerEventData eventData) {
        AlternarIndicadores(false);
    }

    // Función auxiliar para activar/desactivar ambos objetos de golpe
    private void AlternarIndicadores(bool activar) {
        if (indicadorIzquierdo != null) indicadorIzquierdo.SetActive(activar);
        if (indicadorDerecho != null) indicadorDerecho.SetActive(activar);
    }

    // Por si necesitas apagarlos por código desde otro lado en algún momento
    public void ForzarApagarIndicadores() {
        AlternarIndicadores(false);
    }
}