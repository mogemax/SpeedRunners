using UnityEngine;
using UnityEngine.EventSystems; // Necesario para detectar el ratón
using UnityEngine.UI;           // ¡NUEVO! Necesario para controlar el componente Button
using TMPro;                    // Necesario para tus textos TextMeshPro

public class BotonInteractivo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Efecto de Aumento")]
    [SerializeField] private float multiplicadorEscala = 1.1f;
    [SerializeField] private float velocidadAnimacion = 15f;

    private Vector3 escalaOriginal;
    private Vector3 escalaObjetivo;

    [Header("Texto Descriptivo")]
    [SerializeField] private TextMeshProUGUI panelDeTexto;
    [TextArea(2, 4)]
    [SerializeField] private string descripcionDelBoton;

    private Button botonComponente; // Para guardar la referencia al botón físico

    void Start() {
        escalaOriginal = transform.localScale;
        escalaObjetivo = escalaOriginal;

        if (panelDeTexto != null)
            panelDeTexto.text = "";

        // Buscamos automáticamente el componente Button en este mismo objeto
        botonComponente = GetComponent<Button>();
        if (botonComponente != null) {
            botonComponente.onClick.AddListener(ReproducirSonidoClick);
        }
    }

    private void ReproducirSonidoClick() {
        if (GestorAudioMenu.Instancia != null)
            GestorAudioMenu.Instancia.ReproducirClick();
    }

    void Update() {
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, Time.deltaTime * velocidadAnimacion);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        escalaObjetivo = escalaOriginal * multiplicadorEscala;

        if (panelDeTexto != null)
            panelDeTexto.text = descripcionDelBoton;

        if (GestorAudioMenu.Instancia != null)
            GestorAudioMenu.Instancia.ReproducirHover();
    }

    public void OnPointerExit(PointerEventData eventData) {
        escalaObjetivo = escalaOriginal;

        if (panelDeTexto != null)
            panelDeTexto.text = "";
    }

    // Apaga solo los efectos de este script (escala y texto descriptivo)
    public void ApagarScript() {
        transform.localScale = escalaOriginal;
        escalaObjetivo = escalaOriginal;

        if (panelDeTexto != null) {
            panelDeTexto.text = "Esperando...";
        }

        this.enabled = false;
    }

    // OPTION 2: Desaparece el botón de la pantalla por completo
    public void OcultarBotonPorCompleto() {
        if (panelDeTexto != null) {
            panelDeTexto.text = "";
        }

        // Desactiva el GameObject entero de la escena (hace "invisible" el botón)
        gameObject.SetActive(false);
    }
}