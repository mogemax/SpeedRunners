using UnityEngine;

/// <summary>
/// La bomba estática que queda en el mundo tras la primera activación de TimeBombPickup.
/// Detona por orden del jugador que la colocó.
/// </summary>
public class DroppedBomb : MonoBehaviour
{
    [Header("Explosión")]
    [SerializeField] private float _explosionRadius = 5f;
    [SerializeField] private float _knockbackForce = 20f;
    [SerializeField] private float _selfBoostForce = 15f;
    [SerializeField] private LayerMask _affectedLayers;

    [Header("Efectos")]
    [SerializeField] private GameObject _explosionEffectPrefab;
    [SerializeField] private GameObject _bombTriggerEffectPrefab;

    private bool _detonated;

    /// <summary>
    /// Detona la bomba. Aplica fuerzas a jugadores y rompe BreakableCrates en radio.
    /// </summary>
    /// <param name="detonatingPlayer">El jugador que apretó el botón (recibe boost, no knockback).</param>
    public void Detonate(GameObject detonatingPlayer)
    {
        if (_detonated) return;
        _detonated = true;

        if (_explosionEffectPrefab != null)
            Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);

        if (_bombTriggerEffectPrefab != null && detonatingPlayer != null)
            Instantiate(_bombTriggerEffectPrefab, detonatingPlayer.transform.position,
                        Quaternion.identity, detonatingPlayer.transform);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, _affectedLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<PlayerMovement>(out var movement))
            {
                bool isSelf = hit.gameObject == detonatingPlayer;

                if (isSelf)
                {
                    float dirX = movement.IsFacingRight ? 1f : -1f;
                    movement.ApplyKnockback(new Vector2(dirX, 0.3f), _selfBoostForce);
                }
                else
                {
                    Vector2 dir = (Vector2)(hit.transform.position - transform.position);
                    movement.ApplyKnockback(dir.normalized, _knockbackForce);
                }

                continue;
            }

            if (hit.TryGetComponent<BreakableCrate>(out var crate))
            {
                crate.Break();
                continue;
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
