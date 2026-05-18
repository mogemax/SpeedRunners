using UnityEngine;

public class MovimientoYRotacion : MonoBehaviour {
    [Header("Configuración de Rotación")]
    [SerializeField] private float anguloMaximo = 720f; // Ejemplo: 2 vueltas completas
    [SerializeField] private float velocidadGiro = 1f;

    [Header("Configuración de Movimiento")]
    [SerializeField] private float amplitudMovimiento = 50f;
    [SerializeField] private float velocidadMovimiento = 2f;

    private Vector2 posicionInicial;

    void Start() {
        if (GetComponent<RectTransform>() != null)
            posicionInicial = GetComponent<RectTransform>().anchoredPosition;
        else
            posicionInicial = transform.localPosition;
    }

    void Update() {
        // 1. Rotación segura con PingPong
        // Usamos un multiplicador moderado para evitar saltos bruscos
        float anguloZ = Mathf.PingPong(Time.time * velocidadGiro * 50f, anguloMaximo);

        // El Quaternion.Euler es sensible; si el valor es NaN, lo ignoramos
        if (!float.IsNaN(anguloZ)) {
            transform.localRotation = Quaternion.Euler(0, 0, anguloZ);
        }

        // 2. Movimiento horizontal
        float desplazamientoX = Mathf.Sin(Time.time * velocidadMovimiento) * amplitudMovimiento;

        if (GetComponent<RectTransform>() != null) {
            RectTransform rect = GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(posicionInicial.x + desplazamientoX, rect.anchoredPosition.y);
        }
    }
}