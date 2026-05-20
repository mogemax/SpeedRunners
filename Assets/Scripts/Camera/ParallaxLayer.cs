using System.Collections;
using UnityEngine;

/// <summary>
/// Mueve esta capa de fondo en función del desplazamiento de la cámara,
/// creando efecto de parallax.
///
///   parallaxFactor = 0  → capa completamente fija a la cámara (skybox, fondo sólido)
///   parallaxFactor = 0..1 → parallax con profundidad variable
///   parallaxFactor = 1  → capa fija en el mundo (sin parallax)
///
/// Con scaleWithZoom = true, la capa se escala proporcionalmente al
/// orthographicSize actual para mantener cobertura completa a cualquier zoom.
/// Establece referenceSize al orthographicSize al que diseñaste el arte.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax")]
    [Tooltip("0 = fijo a la cámara | 1 = fijo en el mundo")]
    [Range(0f, 1f)]
    public float parallaxFactorX = 0f;

    [Tooltip("Parallax vertical. Generalmente 0 para fondos horizontales.")]
    [Range(0f, 1f)]
    public float parallaxFactorY = 0f;

    [Header("Tiling")]
    [Tooltip("Repetir la textura horizontalmente cuando se aleja de la cámara.")]
    public bool enableTiling = false;

    [Tooltip("Ancho del sprite en unidades de mundo al referenceSize (escala sin modificar). " +
             "Con scaleWithZoom activo el tile se ajusta automáticamente al zoom actual.")]
    public float tileWidth = 20f;

    [Header("Escala con zoom")]
    [Tooltip("Escala la capa proporcionalmente al zoom para que siempre cubra el viewport.")]
    public bool scaleWithZoom = true;

    [Tooltip("orthographicSize al que se diseñó esta capa. " +
             "Usa el mismo valor que minSize en SpeedRunnersCamera.")]
    public float referenceSize = 5f;

    [Header("Referencia")]
    [Tooltip("Dejar vacío para usar Camera.main.")]
    public Camera targetCamera;

    private Vector3 _lastCameraPos;
    private Vector3 _initialScale;
    private bool    _initialized;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        _initialScale = transform.localScale;
        StartCoroutine(InitAfterFirstFrame());
    }

    // Espera a que SpeedRunnersCamera haya completado su primer LateUpdate,
    // luego snapea la capa a la posición de la cámara (preservando Z para sorting)
    // y activa el tracking. La posición en el Editor es irrelevante en runtime.
    private IEnumerator InitAfterFirstFrame()
    {
        yield return null;
        Vector3 camPos = targetCamera.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        _lastCameraPos     = camPos;
        _initialized       = true;
    }

    private void LateUpdate()
    {
        if (!_initialized) return;

        Vector3 delta = targetCamera.transform.position - _lastCameraPos;

        float moveX = delta.x * (1f - parallaxFactorX);
        float moveY = delta.y * (1f - parallaxFactorY);

        transform.position += new Vector3(moveX, moveY, 0f);

        _lastCameraPos = targetCamera.transform.position;

        if (scaleWithZoom)
            ApplyZoomScale();

        if (enableTiling)
            HandleTiling();
    }

    private void ApplyZoomScale()
    {
        float scale = targetCamera.orthographicSize / Mathf.Max(0.001f, referenceSize);
        transform.localScale = _initialScale * scale;
    }

    private void HandleTiling()
    {
        // El ancho real del tile incluye la escala inicial del objeto
        // y, si scaleWithZoom está activo, la escala de zoom actual.
        float scaledTileWidth = tileWidth * _initialScale.x;
        if (scaleWithZoom)
            scaledTileWidth *= targetCamera.orthographicSize / Mathf.Max(0.001f, referenceSize);

        float distFromCamera = transform.position.x - targetCamera.transform.position.x;

        if (Mathf.Abs(distFromCamera) >= scaledTileWidth)
        {
            float jump = Mathf.Sign(distFromCamera) * scaledTileWidth;
            transform.position = new Vector3(
                transform.position.x - jump,
                transform.position.y,
                transform.position.z
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!enableTiling) return;

        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.4f);
        Vector3 center = new Vector3(transform.position.x, transform.position.y, 0f);
        Gizmos.DrawWireCube(center, new Vector3(tileWidth, 10f, 0f));
    }
}
