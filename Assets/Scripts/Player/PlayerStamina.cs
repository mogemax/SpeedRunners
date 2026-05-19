using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    public float maxStamina          = 100f;
    public float drainRate           = 25f;   // por segundo al mantener boost
    public float speedBoostMultiplier = 1.4f;

    public float Current    { get; private set; }
    public bool  IsBoosting { get; private set; }

    public event Action<float, float> OnStaminaChanged; // (current, max)

    private PlayerInputReader _input;

    private void Awake()
    {
        _input  = GetComponent<PlayerInputReader>();
        Current = 0f;
    }

    private void OnEnable()
    {
        if (RaceManager.Instance != null)
            RaceManager.Instance.OnCountdownFinished += ResetStamina;
    }

    private void OnDisable()
    {
        if (RaceManager.Instance != null)
            RaceManager.Instance.OnCountdownFinished -= ResetStamina;
    }

    private void Update()
    {
        IsBoosting = _input.BoostHeld && Current > 0f;

        if (IsBoosting)
        {
            Current = Mathf.Max(0f, Current - drainRate * Time.deltaTime);
            OnStaminaChanged?.Invoke(Current, maxStamina);
        }
    }

    public void Recharge(float amount)
    {
        float prev = Current;
        Current = Mathf.Min(maxStamina, Current + amount);
        if (!Mathf.Approximately(Current, prev))
            OnStaminaChanged?.Invoke(Current, maxStamina);
    }

    public float GetSpeedMultiplier() => IsBoosting ? speedBoostMultiplier : 1f;

    private void ResetStamina()
    {
        Current = 0f;
        OnStaminaChanged?.Invoke(Current, maxStamina);
    }
}
