using UnityEngine;

/// <summary>
/// Pickup de dos fases:
///   Idle  → primera activación → coloca DroppedBomb en el mapa, muestra ícono del botón.
///   Armed → segunda activación → detona la bomba y se limpia del jugador.
/// </summary>
public class TimeBombPickup : PickupBase
{
    [Header("Íconos HUD")]
    [SerializeField] private Sprite _idleIcon;
    [SerializeField] private Sprite _armedIcon;

    [Header("Prefabs")]
    [SerializeField] private GameObject _droppedBombPrefab;

    public override Sprite InitialIcon => _idleIcon;

    private DroppedBomb _placedBomb;
    private bool _isArmed;

    public override void Activate(PlayerPickupHolder holder)
    {
        if (!_isArmed)
            PlaceBomb(holder);
        else
            Detonate(holder);
    }

    private void PlaceBomb(PlayerPickupHolder holder)
    {
        if (_droppedBombPrefab == null)
        {
            Debug.LogError("[TimeBombPickup] DroppedBomb prefab no asignado.");
            return;
        }

        GameObject bombGO = Instantiate(_droppedBombPrefab,
                                        holder.transform.position,
                                        Quaternion.identity);
        _placedBomb = bombGO.GetComponent<DroppedBomb>();
        _isArmed    = true;

        NotifyIconChange(_armedIcon);
    }

    private void Detonate(PlayerPickupHolder holder)
    {
        _placedBomb?.Detonate(holder.gameObject);
        holder.ClearPickup();
    }
}
