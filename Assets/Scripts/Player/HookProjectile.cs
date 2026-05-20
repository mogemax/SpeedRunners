using System.Collections;
using UnityEngine;

public class HookProjectile : MonoBehaviour {
    [Header("Trayectoria")]
    [Tooltip("Distancia máxima antes de auto-destruirse silenciosamente")]
    public float maxDistance = 50f;
    [Tooltip("Offset de rotación para corregir la orientación natural del sprite (grados)")]
    public float rotationOffset = 0f;

    [Header("Animación de ruptura")]
    public Sprite[] breakSprites;
    public float breakFrameRate = 20f;

    private PlayerGrapple _owner;
    private Vector2 _origin;
    private Vector2 _velocity;
    private bool _done;
    private Collider2D[] _ownerColliders;

    private SpriteRenderer _sr;

    private void Awake() {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void Launch(PlayerGrapple owner, Vector2 origin, Vector2 direction, float speed) {
        _owner          = owner;
        _origin         = origin;
        _velocity       = direction.normalized * speed;
        _done           = false;
        _ownerColliders = owner.GetComponentsInChildren<Collider2D>();
        transform.position = origin;
        float initialAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, initialAngle + rotationOffset);
    }

    private void Update() {
        if (_done) return;

        if (Vector2.Distance(_origin, transform.position) >= maxDistance) {
            SilentDestroy();
            return;
        }

        Vector2 prevPos = transform.position;
        Vector2 delta   = _velocity * Time.deltaTime;
        transform.position = prevPos + delta;

        // Rotar sprite en la dirección de movimiento
        if (_velocity.sqrMagnitude > 0.001f) {
            float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }

        // Detectar colisiones a lo largo del movimiento de este frame.
        // CircleCastAll devuelve hits ordenados por distancia (el más cercano primero).
        float dist = delta.magnitude;
        if (dist > 0.001f) {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(prevPos, 0.15f, delta.normalized, dist);
            foreach (RaycastHit2D hit in hits) {
                if (hit.collider.isTrigger) continue;
                if (IsOwnerCollider(hit.collider)) continue;

                if (hit.collider.GetComponent<GrappleSurface>() != null) {
                    // Superficie grappleable → conectar
                    _done = true;
                    _owner.ConnectGrapple(hit.point);
                    _owner.OnProjectileFinished();
                    Destroy(gameObject);
                    return;
                } else {
                    // Superficie sólida sin GrappleSurface → fallo
                    _done = true;
                    transform.position = hit.point;
                    StartCoroutine(PlayBreak());
                    return;
                }
            }
        }
    }

    private bool IsOwnerCollider(Collider2D col) {
        if (_ownerColliders == null) return false;
        foreach (Collider2D c in _ownerColliders)
            if (c == col) return true;
        return false;
    }

    public void Cancel() {
        _done = true;
        Destroy(gameObject);
    }

    private void SilentDestroy() {
        _done = true;
        _owner.OnProjectileFinished();
        Destroy(gameObject);
    }

    // Reservado para impactos con superficies no grappleables en el futuro
    private IEnumerator PlayBreak() {
        // Resetear el estado del gancho inmediatamente — la animación es solo visual
        _owner.OnProjectileFinished();
        _owner.ShowFailRope((Vector2)transform.position);

        if (_sr != null && breakSprites != null && breakSprites.Length > 0) {
            float interval = 1f / breakFrameRate;
            foreach (Sprite s in breakSprites) {
                if (_sr == null) break;
                _sr.sprite = s;
                yield return new WaitForSeconds(interval);
            }
        } else {
            yield return new WaitForSeconds(0.25f);
        }

        _owner.HideFailRope();
        if (_sr != null) _sr.enabled = false;
        Destroy(gameObject);
    }
}
