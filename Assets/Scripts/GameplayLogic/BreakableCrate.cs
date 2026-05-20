using UnityEngine;

/// <summary>
/// Caja destructible de tres capas (backStar / frontStar / cubo animado).
/// Al romperse reproduce el efecto BoxPop y spawnea un PickupCollectible.
/// Completamente independiente de FallingCrate.
/// </summary>
public class BreakableCrate : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _pickupCollectiblePrefab;
    [SerializeField] private GameObject _boxPopEffectPrefab;

    [Header("Respawn")]
    [SerializeField] private bool _respawns = true;
    [SerializeField] private float _respawnTime = 8f;

    private SpriteRenderer[] _renderers;
    private Collider2D _collider;
    private bool _isBroken;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _collider  = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isBroken) return;
        if (collision.gameObject.TryGetComponent<PlayerMovement>(out _))
            Break();
    }

    /// <summary>Rompe la caja. Puede ser llamado externamente (ej. DroppedBomb).</summary>
    public void Break()
    {
        if (_isBroken) return;
        _isBroken = true;

        if (_boxPopEffectPrefab != null)
            Instantiate(_boxPopEffectPrefab, transform.position, Quaternion.identity);

        if (_pickupCollectiblePrefab != null)
            Instantiate(_pickupCollectiblePrefab, transform.position, Quaternion.identity);

        SetVisible(false);
        _collider.enabled = false;

        if (_respawns)
            Invoke(nameof(Respawn), _respawnTime);
        else
            Destroy(gameObject);
    }

    private void Respawn()
    {
        _isBroken = false;
        SetVisible(true);
        _collider.enabled = true;
    }

    private void SetVisible(bool visible)
    {
        foreach (var sr in _renderers)
            if (sr != null) sr.enabled = visible;
    }
}
