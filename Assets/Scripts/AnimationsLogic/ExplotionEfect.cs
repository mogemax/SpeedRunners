using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ExplosionEffect : MonoBehaviour
{
    [Header("Sprites de la explosión (en orden)")]
    public Sprite[] frames;

    [Header("Velocidad")]
    public float frameRate = 18f;

    [Header("Slow Motion")]
    [Range(0.05f, 1f)]
    public float slowMotionScale = 0.25f;
    public float slowMotionDuration = 1.2f;
    public float slowMotionRecoveryTime = 0.4f;

    public event Action OnExplosionComplete;

    private Image         _image;
    private RectTransform _rect;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect  = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    // Recibe la posición mundial del jugador eliminado y la convierte
    // a coordenadas de Canvas para que la explosión aparezca ahí en pantalla.
    public void Play(Vector3 worldPos)
    {
        PositionOnCanvas(worldPos);
        gameObject.SetActive(true);
        StartCoroutine(ExplosionRoutine());
    }

    private void PositionOnCanvas(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Posición en píxeles de pantalla
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);

        // Clampear para que la explosión no quede cortada en el borde
        float halfW = _rect.sizeDelta.x * 0.5f;
        float halfH = _rect.sizeDelta.y * 0.5f;
        screenPos.x = Mathf.Clamp(screenPos.x, halfW, Screen.width  - halfW);
        screenPos.y = Mathf.Clamp(screenPos.y, halfH, Screen.height - halfH);

        // Convertir screen point → local point en el Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out Vector2 localPoint
        );

        _rect.localPosition = localPoint;
    }

    private IEnumerator ExplosionRoutine()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("[ExplosionEffect] Asigna sprites en el campo 'Frames'.");
            gameObject.SetActive(false);
            OnExplosionComplete?.Invoke();
            yield break;
        }

        Time.timeScale      = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        float interval = 1f / frameRate;

        foreach (Sprite frame in frames)
        {
            _image.sprite = frame;
            yield return new WaitForSecondsRealtime(interval);
        }

        yield return StartCoroutine(RecoverTimeScale());

        gameObject.SetActive(false);
        OnExplosionComplete?.Invoke();
    }

    private IEnumerator RecoverTimeScale()
    {
        float start   = Time.timeScale;
        float elapsed = 0f;
        while (elapsed < slowMotionRecoveryTime)
        {
            elapsed             += Time.unscaledDeltaTime;
            Time.timeScale       = Mathf.Lerp(start, 1f, elapsed / slowMotionRecoveryTime);
            Time.fixedDeltaTime  = 0.02f * Time.timeScale;
            yield return null;
        }
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
