using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Setup en la escena:
//   Canvas (Screen Space - Overlay)
//     └─ CountdownDisplay  ← este GameObject
//          ├─ Image
//          └─ CountdownDisplayController
//
// En el Inspector asigna en "Frames" los sprites en orden: 3, 2, 1, GO!
// Cada sprite se muestra durante "Seconds Per Frame" segundos.
[RequireComponent(typeof(Image))]
public class CountdownDisplayController : MonoBehaviour
{
    [Header("Sprites de la cuenta regresiva (en orden: 3, 2, 1, GO!)")]
    public Sprite[] frames;

    [Header("Duración de cada sprite (segundos reales)")]
    public float secondsPerFrame = 1f;

    [Header("Fondo deslizante (opcional)")]
    [Tooltip("RectTransform hijo con el asset de fondo. Se desliza hasta posición (0,0) antes de los frames.")]
    public RectTransform background;
    [Tooltip("Offset inicial del fondo. Para entrada de arriba hacia abajo usar Y positivo (ej: 0, 600).")]
    public Vector2 backgroundHiddenOffset = new Vector2(0f, 600f);
    [Tooltip("Duración del slide-in en segundos reales.")]
    public float backgroundSlideDuration = 0.3f;
    public AnimationCurve backgroundSlideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public event Action<int> OnTick;
    public event Action OnCountdownComplete;

    private Image _image;
    private Vector2 _backgroundFinalPos;

    private void Awake()
    {
        _image = GetComponent<Image>();
        if (background != null)
        {
            _backgroundFinalPos = background.anchoredPosition;
            background.gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    public IEnumerator PlayRoutine()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("[CountdownDisplayController] Sin sprites asignados.");
            OnCountdownComplete?.Invoke();
            yield break;
        }

        gameObject.SetActive(true);

        yield return SlideInBackground();

        for (int i = 0; i < frames.Length; i++)
        {
            _image.sprite = frames[i];
            OnTick?.Invoke(i);
            yield return new WaitForSecondsRealtime(secondsPerFrame);
        }

        if (background != null)
            background.gameObject.SetActive(false);
        gameObject.SetActive(false);
        OnCountdownComplete?.Invoke();
    }

    private IEnumerator SlideInBackground()
    {
        if (background == null || backgroundSlideDuration <= 0f)
            yield break;

        background.gameObject.SetActive(true);

        Vector2 start = _backgroundFinalPos + backgroundHiddenOffset;
        Vector2 end   = _backgroundFinalPos;
        float   t     = 0f;

        background.anchoredPosition = start;

        while (t < backgroundSlideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = backgroundSlideCurve.Evaluate(Mathf.Clamp01(t / backgroundSlideDuration));
            background.anchoredPosition = Vector2.LerpUnclamped(start, end, k);
            yield return null;
        }

        background.anchoredPosition = end;
    }

    public void Hide()
    {
        StopAllCoroutines();
        if (background != null)
            background.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}
