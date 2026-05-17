using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Setup en la escena:
//   Canvas (Screen Space - Overlay)
//     └─ WinnerDisplay  ← este GameObject
//          ├─ Image
//          └─ WinnerDisplayController
//
// En el Inspector asigna los sprites de cada jugador en "Players > Sprites".
// No necesita Animator ni Animator Controller.
[RequireComponent(typeof(Image))]
public class WinnerDisplayController : MonoBehaviour
{
    [Header("Sprites de victoria por jugador (en orden de índice)")]
    public WinnerPlayerData[] players;

    [Header("Velocidad de animación")]
    public float frameRate = 12f;

    [Header("Fondo deslizante (opcional)")]
    [Tooltip("RectTransform hijo con el asset de fondo. Se desliza hasta posición (0,0) antes de los frames.")]
    public RectTransform background;
    [Tooltip("Offset inicial del fondo respecto a su posición final. Para entrada de izquierda a derecha usar X negativo (ej: -1200, 0).")]
    public Vector2 backgroundHiddenOffset = new Vector2(-1200f, 0f);
    [Tooltip("Duración del slide-in en segundos.")]
    public float backgroundSlideDuration = 0.3f;
    public AnimationCurve backgroundSlideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public event Action OnAnimationComplete;

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

    public void Show(int winnerIndex)
    {
        if (players == null || winnerIndex >= players.Length || players[winnerIndex] == null)
        {
            Debug.LogWarning("[WinnerDisplayController] Sin datos para jugador " + winnerIndex);
            OnAnimationComplete?.Invoke();
            return;
        }

        gameObject.SetActive(true);
        StartCoroutine(PlayAnimation(players[winnerIndex].sprites));
    }

    public void Hide()
    {
        if (background != null)
            background.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private IEnumerator PlayAnimation(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("[WinnerDisplayController] Sin sprites asignados.");
            OnAnimationComplete?.Invoke();
            yield break;
        }

        yield return SlideInBackground();

        float interval = 1f / frameRate;

        foreach (Sprite frame in sprites)
        {
            _image.sprite = frame;
            yield return new WaitForSeconds(interval);
        }

        OnAnimationComplete?.Invoke();
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
            t += Time.deltaTime;
            float k = backgroundSlideCurve.Evaluate(Mathf.Clamp01(t / backgroundSlideDuration));
            background.anchoredPosition = Vector2.LerpUnclamped(start, end, k);
            yield return null;
        }

        background.anchoredPosition = end;
    }
}

[Serializable]
public class WinnerPlayerData
{
    public Sprite[] sprites;
}
