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

    [Header("Slot de Pickup")]
    public Image[] pickupIcons = new Image[2];
    public PlayerPickupHolder[] pickupHolders = new PlayerPickupHolder[2];

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
    private Action<Sprite>[]       _pickupHandlers;

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

        _pickupHandlers = new Action<Sprite>[pickupHolders.Length];
        for (int i = 0; i < pickupHolders.Length; i++)
        {
            int idx = i;
            _pickupHandlers[i] = sprite => UpdatePickupIcon(idx, sprite);
        }

        // Inicializar barras a vacío
        for (int i = 0; i < staminaFillImages.Length; i++)
        {
            if (staminaFillImages[i] != null)
                staminaFillImages[i].fillAmount = 0f;
        }

        // Ocultar iconos de pickup
        foreach (var icon in pickupIcons)
            if (icon != null) icon.gameObject.SetActive(false);

        // Apagar todos los iconos de victoria
        foreach (var slot in playerRoundWins)
        {
            if (slot?.icons == null) continue;
            foreach (var icon in slot.icons)
                if (icon != null) icon.gameObject.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────
    //  AUTO-BIND DE JUGADORES
    // ─────────────────────────────────────────────
    /// <summary>
    /// Llamado por GeneradorJugadores después de spawnear cada jugador.
    /// Extrae y registra los componentes necesarios del player en el slot indicado.
    /// </summary>
    public void BindPlayer(int index, GameObject player)
    {
        if (index < 0 || index >= playerStaminas.Length || player == null) return;

        // Desuscribir referencias anteriores si existían
        if (_staminaHandlers != null && playerStaminas[index] != null)
            playerStaminas[index].OnStaminaChanged -= _staminaHandlers[index];
        if (_pickupHandlers != null && pickupHolders[index] != null)
            pickupHolders[index].OnPickupChanged -= _pickupHandlers[index];

        playerStaminas[index] = player.GetComponent<PlayerStamina>();
        pickupHolders[index]  = player.GetComponent<PlayerPickupHolder>();

        // Suscribir nuevas referencias
        if (_staminaHandlers != null && playerStaminas[index] != null)
            playerStaminas[index].OnStaminaChanged += _staminaHandlers[index];
        if (_pickupHandlers != null && pickupHolders[index] != null)
            pickupHolders[index].OnPickupChanged += _pickupHandlers[index];
    }

    private void Start()
    {
        AutoBindIfNeeded();

        // Suscripción al RaceManager va aquí (no en OnEnable) porque
        // RaceManager.Awake puede no haber corrido todavía cuando OnEnable
        // dispara — depende del orden del archivo de escena. Start corre
        // después de TODOS los Awakes, así que Instance está garantizado.
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRoundEnd          += OnRoundEnd;
            RaceManager.Instance.OnCountdownFinished += OnRoundStart;
            Debug.Log("[PlayerHUDController] Suscrito a RaceManager en Start().");
        }
        else
        {
            Debug.LogError("[PlayerHUDController] Start: RaceManager.Instance sigue null. " +
                           "Verifica que haya un RaceManager en la escena.");
        }
    }

    private void OnDestroy()
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRoundEnd          -= OnRoundEnd;
            RaceManager.Instance.OnCountdownFinished -= OnRoundStart;
        }
    }

    private void AutoBindIfNeeded()
    {
        bool anyMissing = false;
        for (int i = 0; i < pickupHolders.Length; i++)
            if (pickupHolders[i] == null || playerStaminas[i] == null) { anyMissing = true; break; }

        if (!anyMissing) return;

        // P1 usa PlayerInputReader (teclado), P2 usa PlayerInputReaderGamepad (control).
        var holders = FindObjectsByType<PlayerPickupHolder>(FindObjectsSortMode.None);
        foreach (var holder in holders)
        {
            int index = holder.GetComponent<PlayerInputReaderGamepad>() != null ? 1 : 0;
            BindPlayer(index, holder.gameObject);
        }
    }

    private void OnEnable()
    {
        if (_staminaHandlers == null || _pickupHandlers == null) return;

        for (int i = 0; i < playerStaminas.Length; i++)
        {
            if (playerStaminas[i] != null)
                playerStaminas[i].OnStaminaChanged += _staminaHandlers[i];
        }

        for (int i = 0; i < pickupHolders.Length; i++)
        {
            if (pickupHolders[i] != null)
                pickupHolders[i].OnPickupChanged += _pickupHandlers[i];
        }

        // Suscripciones al RaceManager NO se manejan aquí — Start() lo hace
        // porque Awake del RaceManager puede no haber corrido todavía
        // cuando OnEnable dispara durante la carga de escena.
    }

    private void OnDisable()
    {
        if (_staminaHandlers != null)
        {
            for (int i = 0; i < playerStaminas.Length; i++)
            {
                if (playerStaminas[i] != null)
                    playerStaminas[i].OnStaminaChanged -= _staminaHandlers[i];
            }
        }

        if (_pickupHandlers != null)
        {
            for (int i = 0; i < pickupHolders.Length; i++)
            {
                if (pickupHolders[i] != null)
                    pickupHolders[i].OnPickupChanged -= _pickupHandlers[i];
            }
        }

        // Desuscripciones del RaceManager las hace OnDestroy, no aquí, para
        // que sobrevivan a SetActive(false) si algo apaga este GameObject.
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
        Debug.Log($"[PlayerHUDController] OnRoundEnd recibido. winnerIndex={winnerIndex}");
        // No desactivamos el HUD completo: el WinnerDisplay cubre la pantalla
        // por encima durante la animación de victoria. Desactivar este GameObject
        // dispara OnDisable (que rompe re-suscripciones) y los iconos de victoria
        // recién activados quedan ocultos para siempre si algo falla en la
        // reactivación. Es más simple y robusto dejarlo siempre visible.
        ActivateNextWinIcon(winnerIndex);
    }

    private void OnRoundStart()
    {
        // Reactivación defensiva: por si algo (un Animator, otro script, una
        // versión anterior del prefab) dejó el HUD apagado, lo prendemos al
        // arrancar la ronda.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────
    //  PICKUP ICON
    // ─────────────────────────────────────────────
    private void UpdatePickupIcon(int playerIndex, Sprite sprite)
    {
        if (playerIndex >= pickupIcons.Length) return;
        var icon = pickupIcons[playerIndex];
        if (icon == null) return;

        if (sprite != null)
        {
            icon.sprite = sprite;
            icon.gameObject.SetActive(true);
        }
        else
        {
            icon.gameObject.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────
    //  WIN ICONS
    // ─────────────────────────────────────────────
    private void ActivateNextWinIcon(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerRoundWins.Length)
        {
            Debug.LogWarning($"[PlayerHUDController] ActivateNextWinIcon: playerIndex {playerIndex} " +
                             $"fuera de rango. playerRoundWins.Length = {playerRoundWins.Length}");
            return;
        }

        var slot = playerRoundWins[playerIndex];
        if (slot?.icons == null)
        {
            Debug.LogWarning($"[PlayerHUDController] ActivateNextWinIcon: slot o slot.icons " +
                             $"es null en index {playerIndex}");
            return;
        }

        Debug.Log($"[PlayerHUDController] ActivateNextWinIcon player {playerIndex}: " +
                  $"{slot.icons.Length} iconos disponibles. Buscando primer inactivo…");

        for (int i = 0; i < slot.icons.Length; i++)
        {
            var icon = slot.icons[i];
            if (icon == null)
            {
                Debug.LogWarning($"  - icono [{i}]: REFERENCIA NULL");
                continue;
            }
            bool active = icon.gameObject.activeSelf;
            Debug.Log($"  - icono [{i}] = {icon.gameObject.name}, activeSelf={active}");
            if (!active)
            {
                icon.gameObject.SetActive(true);
                Debug.Log($"[PlayerHUDController] ACTIVADO icono [{i}] = {icon.gameObject.name}");
                return;
            }
        }
        Debug.LogWarning($"[PlayerHUDController] No quedan iconos inactivos para player {playerIndex}.");
    }
}