using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour {
    [Header("Referencias")]
    [Tooltip("Panel raíz del menú de pausa. Se activa/desactiva al pausar.")]
    [SerializeField] private GameObject panelPausa;

    [Header("Configuración")]
    [Tooltip("Tecla que abre/cierra la pausa.")]
    [SerializeField] private Key teclaPausa = Key.Escape;
    [SerializeField] private string escenaSalida = "MenuSeleccion";

    [Header("Sonido")]
    [Tooltip("Sonido que se reproduce al pausar/despausar.")]
    [SerializeField] private AudioClip sonidoPausa;
    [SerializeField, Range(0f, 1f)] private float volumenPausa = 1f;

    private bool enPausa;
    private AudioSource fuente;

    void Awake() {
        fuente = GetComponent<AudioSource>();
        if (fuente == null) fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
    }

    void Start() {
        if (panelPausa != null) panelPausa.SetActive(false);
        Time.timeScale = 1f;
        enPausa = false;
        OcultarCursor();
    }

    void Update() {
        var teclado = Keyboard.current;
        if (teclado == null) return;
        if (teclado[teclaPausa].wasPressedThisFrame) {
            if (enPausa) Continuar();
            else Pausar();
        }
    }

    private void ReproducirSonido() {
        if (sonidoPausa != null && fuente != null)
            fuente.PlayOneShot(sonidoPausa, volumenPausa);
    }

    private void Pausar() {
        enPausa = true;
        ReproducirSonido();
        Time.timeScale = 0f;
        MostrarCursor();
        if (panelPausa != null) panelPausa.SetActive(true);
    }

    public void Continuar() {
        enPausa = false;
        ReproducirSonido();
        Time.timeScale = 1f;
        OcultarCursor();
        if (panelPausa != null) panelPausa.SetActive(false);
    }

    public void Reiniciar() {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Salir() {
        Time.timeScale = 1f;
        MostrarCursor();
        SceneManager.LoadScene(escenaSalida);
    }

    private static void OcultarCursor() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private static void MostrarCursor() {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
