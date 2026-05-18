using UnityEngine;
using System.Collections;

public class RotatingGate : MonoBehaviour
{
    [Header("Rotación de la puerta")]
    [Tooltip("Transform del sprite que rota (hijo). Su pivote define el eje de giro.")]
    public Transform gateTransform;

    [Tooltip("Ángulo objetivo en grados (eje Z). Puede ser cualquier valor, no solo 90/180.")]
    public float targetAngle = 90f;

    [Tooltip("Duración del giro en segundos.")]
    public float rotationDuration = 0.4f;

    [Header("Reset")]
    [Tooltip("Tiempo en segundos antes de que la puerta vuelva a su posición inicial.")]
    public float resetTime = 3.0f;

    [Header("Feedback visual de la palanca (trigger)")]
    [Tooltip("SpriteRenderer de la palanca/trigger que cambia de sprite al activarse.")]
    public SpriteRenderer triggerRenderer;

    [Tooltip("Sprite cuando la palanca NO ha sido activada.")]
    public Sprite spriteInicial;

    [Tooltip("Sprite cuando la palanca FUE activada por el jugador.")]
    public Sprite spriteActivado;

    private Quaternion _initialRotation;
    private bool _isTriggered = false;

    private void Awake()
    {
        if (gateTransform != null)
            _initialRotation = gateTransform.localRotation;

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

        Quaternion targetRot = _initialRotation * Quaternion.Euler(0f, 0f, targetAngle);
        yield return RotateGate(_initialRotation, targetRot, rotationDuration);

        yield return new WaitForSeconds(resetTime);

        yield return RotateGate(gateTransform.localRotation, _initialRotation, rotationDuration);

        if (triggerRenderer != null && spriteInicial != null)
            triggerRenderer.sprite = spriteInicial;

        _isTriggered = false;
    }

    private IEnumerator RotateGate(Quaternion from, Quaternion to, float duration)
    {
        if (gateTransform == null) yield break;

        if (duration <= 0f)
        {
            gateTransform.localRotation = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            gateTransform.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }
        gateTransform.localRotation = to;
    }
}
