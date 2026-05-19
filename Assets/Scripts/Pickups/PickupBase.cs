using System;
using UnityEngine;

/// <summary>
/// Base para todos los pickups. Cada pickup maneja su propio estado interno
/// y notifica al HUD cuando su ícono cambia.
/// </summary>
public abstract class PickupBase : MonoBehaviour
{
    public event Action<Sprite> OnIconChanged;

    /// <summary>Ícono inicial que se muestra en la HUD al recoger el pickup.</summary>
    public abstract Sprite InitialIcon { get; }

    /// <summary>
    /// Llamado cada vez que el jugador presiona UseItem mientras tiene este pickup.
    /// El pickup decide internamente qué hacer según su estado.
    /// </summary>
    public abstract void Activate(PlayerPickupHolder holder);

    protected void NotifyIconChange(Sprite newIcon) => OnIconChanged?.Invoke(newIcon);
}
