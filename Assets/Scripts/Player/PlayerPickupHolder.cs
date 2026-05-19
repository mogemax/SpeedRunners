using System;
using UnityEngine;

/// <summary>
/// Vive en el Player. Gestiona el pickup actual: asignación, activación y limpieza.
/// Notifica al HUD mediante OnPickupChanged cuando el ícono cambia.
/// </summary>
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerPickupHolder : MonoBehaviour
{
    public PickupBase CurrentPickup { get; private set; }

    /// <summary>Fires con el sprite del nuevo ícono, o null cuando se pierde el pickup.</summary>
    public event Action<Sprite> OnPickupChanged;

    private PlayerInputReader _input;

    private void Awake() => _input = GetComponent<PlayerInputReader>();

    private void Update()
    {
        if (_input.UseItemPressed && CurrentPickup != null)
            CurrentPickup.Activate(this);
    }

    /// <summary>
    /// Entrega un pickup al jugador instanciando su prefab como hijo de este transform.
    /// Si el jugador ya tiene uno, lo ignora.
    /// </summary>
    public void GivePickup(PickupBase pickupPrefab)
    {
        if (CurrentPickup != null) return;
        if (pickupPrefab == null) return;

        CurrentPickup = Instantiate(pickupPrefab, transform);
        CurrentPickup.OnIconChanged += OnPickupIconChanged;

        OnPickupChanged?.Invoke(CurrentPickup.InitialIcon);
    }

    /// <summary>Limpia el pickup actual. Llamado por el pickup mismo al consumirse.</summary>
    public void ClearPickup()
    {
        if (CurrentPickup == null) return;

        CurrentPickup.OnIconChanged -= OnPickupIconChanged;
        Destroy(CurrentPickup.gameObject);
        CurrentPickup = null;

        OnPickupChanged?.Invoke(null);
    }

    private void OnPickupIconChanged(Sprite icon) => OnPickupChanged?.Invoke(icon);
}
