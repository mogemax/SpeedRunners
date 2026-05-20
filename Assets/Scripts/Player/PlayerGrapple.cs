using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(DistanceJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerGrapple : MonoBehaviour {
    [Header("Configuración del Gancho")]
    public float grappleRange = 10f;
    public LayerMask grappleLayer;
    public float swingForce = 15f;
    public float maxSwingSpeed = 18f;

    [Header("Proyectil")]
    [Tooltip("Prefab del proyectil (opcional). Si no se asigna, se crea uno en runtime.")]
    public GameObject hookProjectilePrefab;
    [Tooltip("Velocidad de vuelo del gancho en unidades/segundo")]
    public float hookTravelSpeed = 25f;
    [Tooltip("Sprite de la punta del gancho para creación en runtime")]
    public Sprite hookSprite;

    public bool IsGrappling { get; private set; }

    private DistanceJoint2D _joint;
    private LineRenderer _line;
    private Rigidbody2D _rb;
    private PlayerMovement _movement;
    private PlayerAnimatorController _anim;
    private PlayerInputReader _input;
    private Vector2 _grapplePoint;
    private HookProjectile _activeProjectile;
    private bool _showingFailRope;
    private Vector2 _failRopePoint;

    private static readonly Color RopeColorNormal = Color.black;
    private static readonly Color RopeColorFail   = Color.white;

    private void Awake() {
        _joint = GetComponent<DistanceJoint2D>();
        _line = GetComponent<LineRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement>();
        _anim = GetComponent<PlayerAnimatorController>();
        _input = GetComponent<PlayerInputReader>();

        _joint.enabled = false;
        _joint.maxDistanceOnly = false;
        _line.enabled = false;
        _line.startColor = RopeColorNormal;
        _line.endColor   = RopeColorNormal;
    }

    private void TryStartGrapple() {
        if (_activeProjectile != null || IsGrappling) return;

        Vector2 dir = _movement.IsFacingRight
            ? (Vector2.right + Vector2.up).normalized
            : (Vector2.left + Vector2.up).normalized;

        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position, 0.5f, dir, grappleRange, grappleLayer);

        _anim?.OnHookshotStart();

        bool willConnect = hit.collider != null;
        Vector2 target = willConnect
            ? hit.point
            : (Vector2)transform.position + dir * grappleRange;

        _activeProjectile = SpawnHookProjectile();
        _activeProjectile.Launch(this, (Vector2)transform.position, target, hookTravelSpeed, willConnect);
    }

    private HookProjectile SpawnHookProjectile() {
        if (hookProjectilePrefab != null) {
            var go = Instantiate(hookProjectilePrefab, transform.position, Quaternion.identity);
            return go.GetComponent<HookProjectile>();
        }

        var fallback = new GameObject("HookProjectile");
        fallback.transform.position = transform.position;
        if (hookSprite != null) {
            var sr = fallback.AddComponent<SpriteRenderer>();
            sr.sprite = hookSprite;
            sr.sortingOrder = 101;
        }
        return fallback.AddComponent<HookProjectile>();
    }

    // Llamado por HookProjectile cuando llega a la superficie de agarre
    public void ConnectGrapple(Vector2 point) {
        _grapplePoint = point;
        _joint.connectedAnchor = _grapplePoint;
        _joint.distance = Vector2.Distance(transform.position, _grapplePoint);
        _joint.enabled = true;
        _line.enabled = true;
        IsGrappling = true;
        _rb.gravityScale = 1f;
        _anim?.OnSwingStart();
    }

    public void StopGrapple() {
        if (_activeProjectile != null) {
            _activeProjectile.Cancel();
            _activeProjectile = null;
        }
        _joint.enabled = false;
        _line.enabled = false;
        IsGrappling = false;
        _anim?.OnHookshotEnd();
    }

    // Llamado por HookProjectile al llegar a una superficie inválida
    public void ShowFailRope(Vector2 point) {
        _failRopePoint = point;
        _showingFailRope = true;
        _line.startColor = RopeColorFail;
        _line.endColor   = RopeColorFail;
        _line.SetPosition(0, transform.position);
        _line.SetPosition(1, _failRopePoint);
        _line.enabled = true;
    }

    public void HideFailRope() {
        _showingFailRope = false;
        _line.enabled = false;
        _line.startColor = RopeColorNormal;
        _line.endColor   = RopeColorNormal;
    }

    // Llamado por HookProjectile cuando termina (conectó o falló)
    public void OnProjectileFinished() {
        _activeProjectile = null;
        if (!IsGrappling)
            _anim?.OnHookshotEnd();
    }

    private void Update() {
        if (_input != null) {
            if (_input.HookshotPressed)
                TryStartGrapple();
            else if (!_input.HookshotHeld && (IsGrappling || _activeProjectile != null))
                StopGrapple();
        }

        if (IsGrappling) {
            _line.SetPosition(0, transform.position);
            _line.SetPosition(1, _grapplePoint);
        } else if (_showingFailRope) {
            _line.SetPosition(0, transform.position);
        }
    }

    private void FixedUpdate() {
        if (!IsGrappling) return;

        float h = _movement.HorizontalInput;
        if (h != 0f) {
            Vector2 ropeDir = (_grapplePoint - (Vector2)transform.position).normalized;
            Vector2 perpDir = new Vector2(ropeDir.y, -ropeDir.x);
            if (Mathf.Sign(h) != Mathf.Sign(perpDir.x)) perpDir *= -1f;
            _rb.AddForce(perpDir * swingForce, ForceMode2D.Force);
        }

        if (_rb.linearVelocity.magnitude > maxSwingSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSwingSpeed;
    }
}
