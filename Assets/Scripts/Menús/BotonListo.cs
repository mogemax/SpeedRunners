using UnityEngine;
using TMPro;

public class BotonListo : MonoBehaviour {
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el objeto del panel (azul o rojo) al que pertenece este botón")]
    [SerializeField] private ControladorPanelJugador panelJugador;

    [Tooltip("El texto TMP de este mismo botón")]
    [SerializeField] private TextMeshProUGUI textoBoton;

    // Propiedad que el Gestor Global leerá
    public bool EstaListo { get; private set; } = false;

    void Start() {
        if (textoBoton == null)
            textoBoton = GetComponentInChildren<TextMeshProUGUI>();

        ActualizarTexto();
    }

    // Esta función la vinculas al OnClick() de este mismo botón
    public void AlternarEstadoListo() {
        EstaListo = !EstaListo;

        // Le ordenamos al panel que bloquee o libere el cambio de aspecto
        if (panelJugador != null) {
            panelJugador.BloquearCambioAspecto(EstaListo);
        }

        ActualizarTexto();

        // Le avisamos al gestor global que revise si ya pueden jugar
        GestorSeleccionGlobal gestorGlobal = FindFirstObjectByType<GestorSeleccionGlobal>();
        if (gestorGlobal != null) {
            gestorGlobal.VerificarJugadoresListos();
        }
    }

    private void ActualizarTexto() {
        if (textoBoton != null) {
            textoBoton.text = EstaListo ? "ESPERANDO..." : "LISTO!";
        }
    }
}