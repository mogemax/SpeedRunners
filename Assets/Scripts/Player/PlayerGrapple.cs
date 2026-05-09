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

    public bool IsGrappling { get; private set; }

    private DistanceJoint2D _joint;
    private LineRenderer _line;
    private Rigidbody2D _rb;
    private PlayerMovement _movementScript;
    private Vector2 _grapplePoint;

    private void Awake() {
        _joint = GetComponent<DistanceJoint2D>();
        _line = GetComponent<LineRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _movementScript = GetComponent<PlayerMovement>();

        _joint.enabled = false;
        // Mantiene la distancia fija (evita que la cuerda se encoja)
        _joint.maxDistanceOnly = false;
        _line.enabled = false;
    }

    public void OnGrapple(InputValue value) {
        if (value.isPressed) {
            TryStartGrapple();
        } else if (IsGrappling) {
            StopGrapple();
        }
    }

    private void TryStartGrapple() {
        Vector2 direction = _movementScript.IsFacingRight ? (Vector2.right + Vector2.up) : (Vector2.left + Vector2.up);

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.5f, direction.normalized, grappleRange, grappleLayer);

        if (hit.collider != null) {
            _grapplePoint = hit.point;
            _joint.connectedAnchor = _grapplePoint;
            _joint.distance = Vector2.Distance(transform.position, _grapplePoint);

            _joint.enabled = true;
            _line.enabled = true;
            IsGrappling = true;

            _rb.gravityScale = 1f;
        }
    }

    private void StopGrapple() {
        _joint.enabled = false;
        _line.enabled = false;
        IsGrappling = false;
    }

    private void Update() {
        if (IsGrappling) {
            _line.SetPosition(0, transform.position);
            _line.SetPosition(1, _grapplePoint);
        }
    }

    private void FixedUpdate() {
        if (IsGrappling) {
            float h = _movementScript.HorizontalInput;
            if (h != 0) {
                Vector2 ropeDir = (_grapplePoint - (Vector2)transform.position).normalized;
                Vector2 perpDir = new Vector2(ropeDir.y, -ropeDir.x);

                if (Mathf.Sign(h) != Mathf.Sign(perpDir.x)) perpDir *= -1;

                _rb.AddForce(perpDir * swingForce, ForceMode2D.Force);
            }

            if (_rb.linearVelocity.magnitude > maxSwingSpeed) {
                _rb.linearVelocity = _rb.linearVelocity.normalized * maxSwingSpeed;
            }
        }
    }
}