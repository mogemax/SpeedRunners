using UnityEngine;

public class RotacionSimple : MonoBehaviour {
    [Header("Configuración de Oscilación")]
    [SerializeField] private float anguloMinimo = 0f;
    [SerializeField] private float anguloMaximo = 450f; // Ahora puedes poner 450 sin problemas
    [SerializeField] private float velocidadGiro = 2f;

    void Update() {
        float rango = anguloMaximo - anguloMinimo;
        // Mathf.PingPong maneja el vaivén, y nos aseguramos de que el tiempo no sature
        float t = Time.time * velocidadGiro;
        float anguloActual = Mathf.PingPong(t * 45f, rango);

        // Aplicamos la rotación usando Euler de forma segura
        transform.localRotation = Quaternion.Euler(0, 0, anguloMinimo + anguloActual);
    }
}