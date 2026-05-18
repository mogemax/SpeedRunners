using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDController : MonoBehaviour
{
    [Header("Jugadores (asignar en Inspector)")]
    public PlayerStamina[] playerStaminas = new PlayerStamina[2];

    [Header("Retratos")]
    public Image[] portraitImages = new Image[2];

    [Header("Barras de Stamina (Image con fillMethod = Horizontal)")]
    public Image[] staminaFillImages = new Image[2];

    [Header("Iconos de victoria por ronda")]
    public RoundWinSlot[] playerRoundWins = new RoundWinSlot[2];

    [Serializable]
    public class RoundWinSlot
    {
        [Tooltip("Un icono por ronda posible (ej. 3 si winsToWin = 3)")]
        public Image[] icons;
    }

    // ─────────────────────────────────────────────
    //  Delegates almacenados para poder desuscribirse
    // ─────────────────────────────────────────────
    private Action<float, float>[] _staminaHandlers;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _staminaHandlers = new Action<float, float>[playerStaminas.Length];

        for (int i = 0; i < playerStaminas.Length; i++)
        {
            int idx = i;
            _staminaHandlers[i] = (current, max) => UpdateStaminaBar(idx, current, max);
        }

        // Inicializar barras a vacío
        for (int i = 0; i < staminaFillImages.Length; i++)
        {
            if (staminaFillImages[i] != null)
                staminaFillImages[i].fillAmount = 0f;
        }

        // Apagar todos los iconos de victoria
        foreach (var slot in playerRoundWins)
        {
            if (slot?.icons == null) continue;
            foreach (var icon in slot.icons)
                if (icon != null) icon.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        for (int i = 0; i < playerStaminas.Length; i++)
        {
            if (playerStaminas[i] != null)
                playerStaminas[i].OnStaminaChanged += _staminaHandlers[i];
        }

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRoundEnd        += OnRoundEnd;
            RaceManager.Instance.OnCountdownFinished += OnRoundStart;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < playerStaminas.Length; i++)
        {
            if (playerStaminas[i] != null)
                playerStaminas[i].OnStaminaChanged -= _staminaHandlers[i];
        }

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRoundEnd        -= OnRoundEnd;
            RaceManager.Instance.OnCountdownFinished -= OnRoundStart;
        }
    }

    // ─────────────────────────────────────────────
    //  STAMINA BAR
    // ─────────────────────────────────────────────
    private void UpdateStaminaBar(int playerIndex, float current, float max)
    {
        if (playerIndex >= staminaFillImages.Length) return;
        if (staminaFillImages[playerIndex] == null)  return;

        staminaFillImages[playerIndex].fillAmount = max > 0f ? current / max : 0f;
    }

    // ─────────────────────────────────────────────
    //  ROUND EVENTS
    // ─────────────────────────────────────────────
    private void OnRoundEnd(int winnerIndex)
    {
        ActivateNextWinIcon(winnerIndex);
        gameObject.SetActive(false);
    }

    private void OnRoundStart()
    {
        gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────
    //  WIN ICONS
    // ─────────────────────────────────────────────
    private void ActivateNextWinIcon(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerRoundWins.Length) return;

        var slot = playerRoundWins[playerIndex];
        if (slot?.icons == null) return;

        foreach (var icon in slot.icons)
        {
            if (icon != null && !icon.gameObject.activeSelf)
            {
                icon.gameObject.SetActive(true);
                return;
            }
        }
    }
}
