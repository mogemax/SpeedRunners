using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpeedRunnersCamera))]
public class CameraShrinkOverTime : MonoBehaviour
{
    [Header("Tiempos")]
    [Tooltip("Segundos desde el inicio de la ronda hasta que la cámara empieza a achicarse. " +
             "Bájalo a algo como 5 para probar sin esperar.")]
    public float timeUntilShrinkStarts = 180f;

    [Tooltip("Cuántos segundos tarda en llegar de su tamaño normal a minSizeAllowed.")]
    public float shrinkDuration = 30f;

    [Header("Tamaño")]
    [Tooltip("Tamaño mínimo al que llegará la cámara (orthographicSize). " +
             "Cuanto más bajo, menos verán los jugadores.")]
    public float minSizeAllowed = 2.5f;

    [Header("Bordes de alerta")]
    [Tooltip("Componentes AlertBorderFlash en las Images que forman el marco " +
             "(típicamente 4: top, bottom, left, right). El script los enciende " +
             "cuando arranca el achicamiento y modula su frecuencia.")]
    public AlertBorderFlash[] alertBorders;

    [Tooltip("Frecuencia (Hz) del parpadeo al inicio del achicamiento.")]
    public float flashStartFreq = 1f;

    [Tooltip("Frecuencia (Hz) del parpadeo cuando la cámara llega al mínimo.")]
    public float flashEndFreq = 4f;

    [Header("Sprite de aviso (ScreenSpace Canvas)")]
    [Tooltip("GameObject del canvas que se activa cuando arranca el achicamiento.")]
    public GameObject warningSprite;

    [Tooltip("Segundos que se mantiene visible el sprite antes de ocultarse.")]
    public float warningSpriteDuration = 2f;

    private SpeedRunnersCamera _srCam;
    private float _originalMinSize;
    private float _originalMaxSize;
    private Vector2 _originalOffset;

    private float _roundTimer;
    private bool _timerActive;
    private bool _shrinkTriggered;
    private Coroutine _warningSpriteRoutine;

    private void Awake()
    {
        _srCam = GetComponent<SpeedRunnersCamera>();
        _originalMinSize = _srCam.minSize;
        _originalMaxSize = _srCam.maxSize;
        _originalOffset = _srCam.offset;
    }

    private void OnEnable()
    {
        if (RaceManager.Instance != null)
            Subscribe();
        else
            StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (RaceManager.Instance == null) return;
        RaceManager.Instance.OnCountdownFinished -= HandleCountdownFinished;
        RaceManager.Instance.OnRoundEnd -= HandleRoundEnd;
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (RaceManager.Instance == null) yield return null;
        Subscribe();
    }

    private void Subscribe()
    {
        RaceManager.Instance.OnCountdownFinished += HandleCountdownFinished;
        RaceManager.Instance.OnRoundEnd += HandleRoundEnd;
        ResetVisuals();
    }

    private void HandleCountdownFinished()
    {
        _roundTimer = 0f;
        _shrinkTriggered = false;
        _timerActive = true;
        ResetVisuals();
    }

    private void HandleRoundEnd(int _)
    {
        _timerActive = false;
        _shrinkTriggered = false;
        _srCam.minSize = _originalMinSize;
        _srCam.maxSize = _originalMaxSize;
        _srCam.offset = _originalOffset;
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        StopAllBorders();
        if (warningSprite != null) warningSprite.SetActive(false);
        if (_warningSpriteRoutine != null)
        {
            StopCoroutine(_warningSpriteRoutine);
            _warningSpriteRoutine = null;
        }
    }

    private void Update()
    {
        if (!_timerActive) return;

        _roundTimer += Time.deltaTime;

        if (!_shrinkTriggered && _roundTimer >= timeUntilShrinkStarts)
        {
            _shrinkTriggered = true;
            StartAllBorders();
            if (warningSprite != null)
                _warningSpriteRoutine = StartCoroutine(ShowWarningSprite());
        }

        if (!_shrinkTriggered) return;

        float shrinkElapsed = _roundTimer - timeUntilShrinkStarts;
        float t = Mathf.Clamp01(shrinkElapsed / Mathf.Max(0.0001f, shrinkDuration));

        float newMin = Mathf.Lerp(_originalMinSize, minSizeAllowed, t);
        float newMax = Mathf.Lerp(_originalMaxSize, minSizeAllowed, t);
        _srCam.minSize = newMin;
        _srCam.maxSize = newMax;

        float offsetScale = Mathf.Lerp(1f, minSizeAllowed / Mathf.Max(0.0001f, _originalMaxSize), t);
        _srCam.offset = _originalOffset * offsetScale;

        float freq = Mathf.Lerp(flashStartFreq, flashEndFreq, t);
        SetAllBordersFrequency(freq);
        SetAllBordersGrowth(t);
    }

    private void StartAllBorders()
    {
        if (alertBorders == null) return;
        foreach (var b in alertBorders)
            if (b != null) b.StartFlashing();
    }

    private void StopAllBorders()
    {
        if (alertBorders == null) return;
        foreach (var b in alertBorders)
        {
            if (b == null) continue;
            b.StopFlashing();
            b.ResetGrowth();
        }
    }

    private void SetAllBordersFrequency(float freq)
    {
        if (alertBorders == null) return;
        foreach (var b in alertBorders)
            if (b != null) b.frequency = freq;
    }

    private void SetAllBordersGrowth(float t)
    {
        if (alertBorders == null) return;
        foreach (var b in alertBorders)
            if (b != null) b.SetGrowth(t);
    }

    private IEnumerator ShowWarningSprite()
    {
        warningSprite.SetActive(true);
        yield return new WaitForSeconds(warningSpriteDuration);
        warningSprite.SetActive(false);
        _warningSpriteRoutine = null;
    }
}
