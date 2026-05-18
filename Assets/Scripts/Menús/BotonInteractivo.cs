using UnityEngine;
using UnityEngine.EventSystems; // Necesario para detectar el ratón
using TMPro; // Asegúrate de estar usando TextMeshPro para tus textos

public class BotonInteractivo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Efecto de Aumento")]
    [SerializeField] private float multiplicadorEscala = 1.1f; // 1.1 significa 10% más grande
    [SerializeField] private float velocidadAnimacion = 15f;

    private Vector3 escalaOriginal;
    private Vector3 escalaObjetivo;

    [Header("Texto Descriptivo")]
    [SerializeField] private TextMeshProUGUI panelDeTexto; // El texto general donde aparecerá el mensaje
    [TextArea(2, 4)]
    [SerializeField] private string descripcionDelBoton; // Lo que dirá este botón en específico

    void Start() {
        // Guardamos el tamaño original para poder volver a la normalidad
        escalaOriginal = transform.localScale;
        escalaObjetivo = escalaOriginal;

        // Limpiamos el texto al iniciar por si acaso
        if (panelDeTexto != null)
            panelDeTexto.text = "";
    }

    void Update() {
        // Lerp hace que el cambio de tamaño sea fluido y no de golpe
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, Time.deltaTime * velocidadAnimacion);
    }

    // Se ejecuta cuando el ratón ENTRA al botón
    public void OnPointerEnter(PointerEventData eventData) {
        escalaObjetivo = escalaOriginal * multiplicadorEscala;

        if (panelDeTexto != null)
            panelDeTexto.text = descripcionDelBoton;
    }

    // Se ejecuta cuando el ratón SALE del botón
    public void OnPointerExit(PointerEventData eventData) {
        escalaObjetivo = escalaOriginal;

        if (panelDeTexto != null)
            panelDeTexto.text = "";
    }
}