using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración SpeedRunners")]
    public int maxLives = 1;
    public int currentLives;
    public bool IsDead { get; private set; }
    public bool IsInvincible { get; private set; }
    public bool IsStunned { get; private set; }

    public event Action OnDied;

    private void Awake()
    {
        currentLives = maxLives;
    }

    public void TakeOutOfScreenDeath()
    {
        if (IsDead) return;
        Die();
    }

    public void Die()
    {
        if (IsDead) return;

        IsDead = true;
        currentLives = 0;

        OnDied?.Invoke();

        gameObject.SetActive(false);
        Debug.Log($"{gameObject.name} eliminado.");
    }

    /// <summary>
    /// Revive al jugador para una nueva ronda.
    /// Llamado por RaceManager después de teleportar al jugador
    /// al checkpoint de spawn. Resetea IsDead y las vidas.
    /// </summary>
    public void Revive()
    {
        IsDead       = false;
        IsInvincible = false;
        currentLives = maxLives;
        Debug.Log($"[PlayerHealth] {gameObject.name} revivido para nueva ronda.");
    }

    public void TakeSpikedDamage(Vector2 hazardPos) { }
    public void TakeHeavyDamage(Vector2 hazardPos) { Die(); }
}
