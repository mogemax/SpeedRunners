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

    [Header("Posición")]
    [SerializeField] private LayerMask _groundMask = ~0;
    [Tooltip("Cuánto se eleva la bomba sobre el suelo para que su pivote (centro) " +
             "no quede medio enterrado. Típicamente la mitad de la altura del sprite.")]
    [SerializeField] private float _groundOffset = 0.3f;

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

        Vector3 spawnPos = FindGroundBelow(holder.transform.position);

        GameObject bombGO = Instantiate(_droppedBombPrefab,
                                        spawnPos,
                                        Quaternion.identity);
        _placedBomb = bombGO.GetComponent<DroppedBomb>();
        _isArmed    = true;

        NotifyIconChange(_armedIcon);
    }

    private Vector3 FindGroundBelow(Vector3 origin)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 10f, _groundMask);
        if (hit.collider != null)
            return new Vector3(origin.x, hit.point.y + _groundOffset, origin.z);
        return origin;
    }

    private void Detonate(PlayerPickupHolder holder)
    {
        _placedBomb?.Detonate(holder.gameObject);
        holder.ClearPickup();
    }
}
