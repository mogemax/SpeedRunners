using UnityEngine;

/// <summary>
/// Objeto que aparece en el mundo al romper una BreakableCrate.
/// Al tocarlo, entrega el pickup al jugador si este no tiene uno.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PickupCollectible : MonoBehaviour
{
    [SerializeField] private PickupBase _pickupPrefab;
    [SerializeField] private float _lifetime = 10f;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
        Destroy(gameObject, _lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerPickupHolder>(out var holder)) return;
        if (holder.CurrentPickup != null) return;

        holder.GivePickup(_pickupPrefab);
        Destroy(gameObject);
    }
}
