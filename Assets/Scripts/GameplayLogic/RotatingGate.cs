using UnityEngine;
using System.Collections;

public class RotatingGate : MonoBehaviour
{
    [System.Serializable]
    public class GatePiece
    {
        [Tooltip("Transform de la pieza (hijo). Su posición = el eje de rotación.")]
        public Transform pieceTransform;

        [Tooltip("Ángulo objetivo en grados (eje Z). Puede ser positivo o negativo para invertir el giro.")]
        public float targetAngle = 90f;

        [HideInInspector] public Quaternion initialRotation;
    }

    [Header("Piezas de la puerta")]
    [Tooltip("Cada hoja de la compuerta con su propio ángulo. Para una compuerta tipo trampilla, una pieza con +90 y otra con -90.")]
    public GatePiece[] gates;

    [Header("Tiempos")]
    [Tooltip("Duración del giro en segundos.")]
    public float rotationDuration = 0.4f;

    [Tooltip("Tiempo en segundos antes de que las puertas vuelvan a su posición inicial.")]
    public float resetTime = 3.0f;

    [Header("Feedback visual de la palanca (trigger)")]
    [Tooltip("SpriteRenderer de la palanca/trigger que cambia de sprite al activarse.")]
    public SpriteRenderer triggerRenderer;

    [Tooltip("Sprite cuando la palanca NO ha sido activada.")]
    public Sprite spriteInicial;

    [Tooltip("Sprite cuando la palanca FUE activada por el jugador.")]
    public Sprite spriteActivado;

    private bool _isTriggered = false;

    private void Awake()
    {
        if (gates != null)
        {
            foreach (var g in gates)
            {
                if (g != null && g.pieceTransform != null)
                    g.initialRotation = g.pieceTransform.localRotation;
            }
        }

        if (triggerRenderer != null && spriteInicial != null)
            triggerRenderer.sprite = spriteInicial;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isTriggered) return;

        var player = other.attachedRigidbody != null
            ? other.attachedRigidbody.GetComponent<PlayerMovement>()
            : other.GetComponentInParent<PlayerMovement>();

        if (player == null) return;

        _isTriggered = true;
        StartCoroutine(GateCycle());
    }

    private IEnumerator GateCycle()
    {
        if (triggerRenderer != null && spriteActivado != null)
            triggerRenderer.sprite = spriteActivado;

        yield return RotateAll(toTarget: true);

        yield return new WaitForSeconds(resetTime);

        yield return RotateAll(toTarget: false);

        if (triggerRenderer != null && spriteInicial != null)
            triggerRenderer.sprite = spriteInicial;

        _isTriggered = false;
    }

    private IEnumerator RotateAll(bool toTarget)
    {
        if (gates == null || gates.Length == 0) yield break;

        if (rotationDuration <= 0f)
        {
            foreach (var g in gates)
            {
                if (g == null || g.pieceTransform == null) continue;
                g.pieceTransform.localRotation = toTarget
                    ? g.initialRotation * Quaternion.Euler(0f, 0f, g.targetAngle)
                    : g.initialRotation;
            }
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);

            foreach (var g in gates)
            {
                if (g == null || g.pieceTransform == null) continue;
                Quaternion from = toTarget ? g.initialRotation : g.initialRotation * Quaternion.Euler(0f, 0f, g.targetAngle);
                Quaternion to = toTarget ? g.initialRotation * Quaternion.Euler(0f, 0f, g.targetAngle) : g.initialRotation;
                g.pieceTransform.localRotation = Quaternion.Slerp(from, to, t);
            }
            yield return null;
        }

        foreach (var g in gates)
        {
            if (g == null || g.pieceTransform == null) continue;
            g.pieceTransform.localRotation = toTarget
                ? g.initialRotation * Quaternion.Euler(0f, 0f, g.targetAngle)
                : g.initialRotation;
        }
    }
}
