using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Reproduce una animación de explosión en world space y avisa cuando termina.
///
/// Setup en el prefab:
///   - SpriteRenderer (en este mismo GameObject)
///   - Animator con un AnimatorController que tenga UN estado llamado "Explosion"
///   - Este script
///
/// El RaceManager instancia el prefab en la posición del jugador eliminado,
/// llama a Play() y escucha OnExplosionComplete para continuar.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class ExplosionEffect : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN
    // ─────────────────────────────────────────────
    [Header("Animación")]
    [Tooltip("Nombre del estado en el Animator Controller. " +
             "Debe coincidir exactamente con el nombre del estado.")]
    public string animationStateName = "Explosion";

    [Header("Slow Motion")]
    [Tooltip("Escala de tiempo durante la explosión (0.1 = muy lento, 1 = normal)")]
    [Range(0.05f, 1f)]
    public float slowMotionScale = 0.25f;

    [Tooltip("Segundos (en tiempo real) que dura el slow motion tras iniciar la explosión")]
    public float slowMotionDuration = 1.2f;

    [Tooltip("Segundos (en tiempo real) para recuperar la velocidad normal suavemente")]
    public float slowMotionRecoveryTime = 0.4f;

    // ─────────────────────────────────────────────
    //  EVENTOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Se invoca cuando la animación de explosión terminó
    /// y el tiempo ya volvió a la normalidad.
    /// RaceManager escucha esto para continuar con la ronda.
    /// </summary>
    public event Action OnExplosionComplete;

    // ─────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────
    private Animator _animator;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    // ─────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Inicia la explosión. Llamado por RaceManager justo después
    /// de instanciar el prefab en la posición del jugador eliminado.
    /// </summary>
    public void Play()
    {
        StartCoroutine(ExplosionRoutine());
    }

    // ─────────────────────────────────────────────
    //  SECUENCIA
    // ─────────────────────────────────────────────
    private IEnumerator ExplosionRoutine()
    {
        // 1 — Reproducir animación
        _animator.Play(animationStateName, 0, 0f);

        // Esperar un frame para que el Animator registre el estado
        yield return null;

        // Obtener la duración real del clip que está corriendo
        float clipLength = GetCurrentClipLength();

        // 2 — Activar slow motion inmediatamente
        Time.timeScale = slowMotionScale;
        // fixedDeltaTime debe escalar con timeScale para que la física sea consistente
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 3 — Esperar la duración del slow motion (en tiempo real, no en tiempo de juego)
        yield return new WaitForSecondsRealtime(slowMotionDuration);

        // 4 — Recuperar velocidad normal suavemente
        yield return StartCoroutine(RecoverTimeScale());

        // 5 — Esperar a que la animación termine si aún no terminó
        // clipLength está en tiempo de juego; como hubo slow motion usamos tiempo real
        // En este punto el tiempo ya es normal, así que esperamos el resto del clip
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        float remainingNormalized   = 1f - stateInfo.normalizedTime;
        float remainingSeconds      = remainingNormalized * clipLength;

        if (remainingSeconds > 0f)
            yield return new WaitForSeconds(remainingSeconds);

        // 6 — Notificar y destruir
        OnExplosionComplete?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator RecoverTimeScale()
    {
        float startScale = Time.timeScale;
        float elapsed    = 0f;

        while (elapsed < slowMotionRecoveryTime)
        {
            elapsed        += Time.unscaledDeltaTime;
            float t         = elapsed / slowMotionRecoveryTime;
            Time.timeScale  = Mathf.Lerp(startScale, 1f, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    // ─────────────────────────────────────────────
    //  HELPER
    // ─────────────────────────────────────────────
    private float GetCurrentClipLength()
    {
        AnimatorClipInfo[] clips = _animator.GetCurrentAnimatorClipInfo(0);
        if (clips != null && clips.Length > 0)
            return clips[0].clip.length;

        Debug.LogWarning("[ExplosionEffect] No se encontró clip en el Animator. " +
                         "Usando duración de fallback (1s).");
        return 1f;
    }
}
