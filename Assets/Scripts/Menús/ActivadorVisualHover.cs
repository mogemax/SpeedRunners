using UnityEngine;
using UnityEngine.EventSystems; // Necesario para detectar el puntero del ratón

public class ActivadorVisualHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Objetos a Mostrar/Ocultar")]
    [Tooltip("Arrastra aquí los objetos (Textos TMP, Imágenes, Sprites) que quieres que aparezcan.")]
    [SerializeField] private GameObject[] objetosVisuales;

    void Start() {
        // Nos aseguramos de que los objetos empiecen apagados al iniciar la escena
        ApagarObjetos();
    }

    // Se dispara en el instante en que el ratón entra al área del objeto
    public void OnPointerEnter(PointerEventData eventData) {
        foreach (GameObject obj in objetosVisuales) {
            if (obj != null) {
                obj.SetActive(true);
            }
        }
    }

    // Se dispara en el instante en que el ratón sale del área
    public void OnPointerExit(PointerEventData eventData) {
        ApagarObjetos();
    }

    // Método auxiliar para no repetir código
    private void ApagarObjetos() {
        foreach (GameObject obj in objetosVisuales) {
            if (obj != null) {
                obj.SetActive(false);
            }
        }
    }
}