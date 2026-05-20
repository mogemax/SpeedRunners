using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(DistanceJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerGrapple : MonoBehaviour {
    [Header("Configuración del Gancho")]
    [Tooltip("Rango de referencia (no limita el vuelo; lo usa el proyectil como safety)")]
    public float grappleRange = 10f;
    public LayerMask grappleLayer;
    public float swingForce = 15f;
    public float maxSwingSpeed = 18f;

    [Header("Origen de la cuerda")]
    [Tooltip("Offset desde el centro del jugador para simular la posición de la mano")]
    public Vector2 ropeOriginOffset = new Vector2(0.2f, 0.3f);

    [Header("Proyectil")]
    [Tooltip("Prefab del proyectil (opcional). Si no se asigna, se crea uno en runtime.")]
    public GameObject hookProjectilePrefab;
    [Tooltip("Velocidad de vuelo del gancho en unidades/segundo")]
    public float hookTravelSpeed = 25f;
    [Tooltip("Sprite de la punta del gancho para creación en runtime")]
    public Sprite hookSprite;

    [Header("Launch desde suelo")]
    [Tooltip("Impulso aplicado hacia el punto de agarre cuando el hook conecta estando en el suelo")]
    public float groundLaunchImpulse = 10f;

    public bool IsGrappling { get; private set; }

    private Vector2 RopeOrigin {
        get {
            float xDir = _movement.IsFacingRight ? 1f : -1f;
            return (Vector2)transform.position + new Vector2(ropeOriginOffset.x * xDir, ropeOriginOffset.y);
        }
    }

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
        _joint.autoConfigureDistance = false;
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

        _anim?.OnHookshotStart();

        Vector2 origin = RopeOrigin;
        _activeProjectile = SpawnHookProjectile();
        _activeProjectile.Launch(this, origin, dir, hookTravelSpeed);

        // Activar la cuerda negra desde el momento del lanzamiento
        _line.startColor = RopeColorNormal;
        _line.endColor   = RopeColorNormal;
        _line.SetPosition(0, origin);
        _line.SetPosition(1, origin);
        _line.enabled = true;
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
        _joint.autoConfigureDistance = false;
        _joint.connectedAnchor = _grapplePoint;
        _joint.distance = Vector2.Distance(transform.position, _grapplePoint);
        _joint.enabled = true;
        _line.enabled = true;
        IsGrappling = true;
        _rb.gravityScale = 1f;

        if (_movement.IsGrounded) {
            Vector2 rawDir = (_grapplePoint - (Vector2)transform.position).normalized;
            Vector2 launchDir = (rawDir + Vector2.up * 0.35f).normalized;
            _rb.AddForce(launchDir * groundLaunchImpulse, ForceMode2D.Impulse);
        }

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
            _line.SetPosition(0, RopeOrigin);
            _line.SetPosition(1, _grapplePoint);
        } else if (_activeProjectile != null) {
            _line.SetPosition(0, RopeOrigin);
            _line.SetPosition(1, _activeProjectile.transform.position);
        } else if (_showingFailRope) {
            _line.SetPosition(0, RopeOrigin);
        }
    }

    private void FixedUpdate() {
        if (!IsGrappling) return;

        float h = _movement.HorizontalInput;
        if (Mathf.Abs(h) > 0.01f) {
            Vector2 ropeDir = (_grapplePoint - (Vector2)transform.position).normalized;
            Vector2 perp = new Vector2(-ropeDir.y, ropeDir.x);
            if (Vector2.Dot(perp, Vector2.right * h) < 0f) perp = -perp;
            _rb.AddForce(perp * swingForce, ForceMode2D.Force);
        }

        float speed = _rb.linearVelocity.magnitude;
        if (speed > maxSwingSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSwingSpeed;
    }
}
