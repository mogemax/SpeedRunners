using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona vida, daño, stun e invencibilidad del personaje.
/// En SpeedRunners no hay HP tradicional: el daño causa stun/knockback
/// y si te quedas fuera de pantalla, pierdes una vida.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
// ─────────────────────────────────────────────
    //  CONFIGURACIÓN
    // ─────────────────────────────────────────────
    [Header("Vidas")]
    [Tooltip("Vidas al inicio de la partida")]
    public int maxLives = 3;
 
    [Header("Daño y Stun")]
    [Tooltip("Duración del stun al recibir daño normal (Spiked)")]
    public float stunDuration = 0.6f;
 
    [Tooltip("Duración del stun por daño fuerte (Tumble / Grabbed)")]
    public float heavyStunDuration = 1.2f;
 
    [Tooltip("Fuerza del knockback normal")]
    public float knockbackForce = 10f;
 
    [Tooltip("Fuerza del knockback fuerte")]
    public float heavyKnockbackForce = 18f;
 
    [Header("Invencibilidad")]
    [Tooltip("Segundos de invencibilidad tras recibir daño")]
    public float invincibilityDuration = 1.5f;
 
    [Tooltip("Frecuencia del parpadeo durante invencibilidad")]
    public float blinkFrequency = 0.1f;
 
    [Header("Respawn")]
    [Tooltip("Segundos de espera antes de reaparecer")]
    public float respawnDelay = 2f;  // FIX: antes estaba hardcodeado en 2f
 
    // ─────────────────────────────────────────────
    //  ESTADO PÚBLICO
    // ─────────────────────────────────────────────
    public int  CurrentLives  { get; private set; }
    public bool IsDead        { get; private set; }
    public bool IsStunned     { get; private set; }
    public bool IsInvincible  { get; private set; }
 
    // ─────────────────────────────────────────────
    //  EVENTOS (el RaceManager los escucha)
    // ─────────────────────────────────────────────
    public event Action<int> OnLivesChanged;  // parámetro: vidas restantes
    public event Action      OnPlayerDied;
    public event Action      OnPlayerRespawn;
 
    // ─────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────
    private PlayerMovement           _movement;
    private PlayerAnimatorController _anim;
    private SpriteRenderer           _spriteRenderer;
 
    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _anim     = GetComponent<PlayerAnimatorController>();
 
        // FIX: GetComponent primero (mismo GO), luego hijos como fallback
        _spriteRenderer = GetComponent<SpriteRenderer>()
                       ?? GetComponentInChildren<SpriteRenderer>();
 
        CurrentLives = maxLives;
    }
 
    // ─────────────────────────────────────────────
    //  TIPOS DE DAÑO — API pública
    // ─────────────────────────────────────────────
 
    /// <summary>
    /// Daño por pinchos (Spiked).
    /// Knockback hacia arriba-atrás + stun corto.
    /// </summary>
    public void TakeSpikedDamage(Vector2 spikePosition)
    {
        if (IsInvincible || IsDead) return;
 
        // FIX: normalizar el vector resultante, no solo los componentes
        Vector2 knockDir = (((Vector2)transform.position - spikePosition).normalized + Vector2.up).normalized;
 
        ApplyDamage(knockDir, knockbackForce, stunDuration,
                    PlayerAnimatorController.HurtAnimationType.Spiked);
    }
 
    /// <summary>
    /// Daño fuerte (Tumble) — explosión o impacto de jugador.
    /// Knockback horizontal fuerte + stun largo.
    /// </summary>
    public void TakeHeavyDamage(Vector2 sourcePosition)
    {
        if (IsInvincible || IsDead) return;
 
        Vector2 knockDir = ((Vector2)transform.position - sourcePosition).normalized;
 
        ApplyDamage(knockDir, heavyKnockbackForce, heavyStunDuration,
                    PlayerAnimatorController.HurtAnimationType.Tumble);
    }
 
    /// <summary>
    /// Agarrado por otro jugador (Grabbed).
    /// No hay knockback inmediato — el agarrador controla la posición.
    /// </summary>
    public void TakeGrabbedDamage()
    {
        if (IsInvincible || IsDead) return;
 
        _movement.StopHorizontalMovement();
        _anim.OnGrabbed(true);
        StartCoroutine(StunCoroutine(heavyStunDuration));
 
        // FIX: activar invencibilidad para que otros hazards no dañen
        // al jugador mientras está siendo controlado por el agarrador
        StartInvincibility();
    }
 
    /// <summary>
    /// Liberar al personaje de un agarre con impulso.
    /// Llamado por el script del jugador que lo agarró.
    /// </summary>
    public void ReleaseFromGrab(Vector2 throwDirection, float throwForce)
    {
        _anim.OnGrabbed(false);
        _movement.ApplyKnockback(throwDirection, throwForce);
        // La invencibilidad ya está activa desde TakeGrabbedDamage,
        // no es necesario volver a iniciarla aquí.
    }
 
    /// <summary>
    /// Llamado por ScreenBoundaryKiller cuando el jugador
    /// queda fuera de los límites de la cámara.
    /// </summary>
    public void TakeOutOfScreenDeath()
    {
        if (IsDead) return;
        Die();
    }
 
    // ─────────────────────────────────────────────
    //  LÓGICA INTERNA
    // ─────────────────────────────────────────────
 
    private void ApplyDamage(
        Vector2 knockbackDir,
        float force,
        float stun,
        PlayerAnimatorController.HurtAnimationType hurtType)
    {
        _movement.ApplyKnockback(knockbackDir, force);
        _anim.OnHurt(hurtType);
        StartCoroutine(StunCoroutine(stun));
        StartInvincibility();
    }
 
    private IEnumerator StunCoroutine(float duration)
    {
        IsStunned = true;
        yield return new WaitForSeconds(duration);
        // Solo desactivar stun si el jugador sigue vivo —
        // Die() para todas las coroutines, así que si llegamos
        // aquí después de morir, IsDead ya es true y PlayerMovement
        // bloquea el input de todas formas.
        IsStunned = false;
    }
 
    private void StartInvincibility()
    {
        if (!IsInvincible)
            StartCoroutine(InvincibilityCoroutine());
    }
 
    private IEnumerator InvincibilityCoroutine()
    {
        IsInvincible = true;
 
        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = !_spriteRenderer.enabled;
 
            yield return new WaitForSeconds(blinkFrequency);
            elapsed += blinkFrequency;
        }
 
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;
 
        IsInvincible = false;
    }
 
    private void Die()
    {
        // FIX: cancelar todas las coroutines antes de cambiar el estado
        // para que StunCoroutine e InvincibilityCoroutine no sigan
        // modificando IsStunned / IsInvincible después de la muerte
        StopAllCoroutines();
 
        // Asegurar sprite visible al morir (puede estar oculto si estaba parpadeando)
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;
 
        IsDead       = true;
        IsStunned    = true;
        IsInvincible = false;
 
        // Bajar vidas solo si quedan — evita llegar a valores negativos
        if (CurrentLives > 0)
            CurrentLives--;
 
        _anim.OnDeath();
        OnLivesChanged?.Invoke(CurrentLives);
        OnPlayerDied?.Invoke();
 
        if (CurrentLives > 0)
            StartCoroutine(RespawnCoroutine());
        else
            Debug.Log($"[PlayerHealth] {gameObject.name} eliminado — sin vidas restantes.");
    }
 
    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay); // FIX: variable serializable
 
        IsDead    = false;
        IsStunned = false;
 
        // El GameManager debería mover al jugador al SpawnPoint correcto
        // antes o justo después de invocar OnPlayerRespawn.
        // Por ahora el reposicionamiento lo hace el GameManager al escuchar el evento.
        StartInvincibility();
        OnPlayerRespawn?.Invoke();
    }
 
    // ─────────────────────────────────────────────
    //  DETECCIÓN DE COLISIÓN — hazards en el mapa
    // ─────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInvincible || IsDead) return;
 
        if (other.CompareTag("Spike"))
            TakeSpikedDamage(other.transform.position);
        else if (other.CompareTag("HeavyHazard"))
            TakeHeavyDamage(other.transform.position);
    }
 
    // ─────────────────────────────────────────────
    //  DEBUG (solo en editor, identificado por nombre de GO)
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    [Header("Debug")]
    public bool showDebugGUI = false;
 
    private void OnGUI()
    {
        if (!showDebugGUI) return;
 
        // FIX: usar el índice del jugador para no solapar los paneles
        // El PlayerInput asigna un playerIndex automáticamente (0-3)
        var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        int idx = pi != null ? pi.playerIndex : 0;
 
        float yOffset = 10f + idx * 90f;
        GUILayout.BeginArea(new Rect(10, yOffset, 220, 80));
        GUILayout.Label($"[P{idx + 1}] Vidas: {CurrentLives}/{maxLives}");
        GUILayout.Label($"Stun: {IsStunned} | Inv: {IsInvincible}");
        GUILayout.Label($"Dead: {IsDead}");
        GUILayout.EndArea();
    }
#endif
}
