using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el movimiento del personaje: correr, saltar, doble salto y slide.
/// Compatible con el nuevo Input System de Unity.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerAnimatorController))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerMovement : MonoBehaviour {
    [Header("Movimiento")]
    public float maxSpeed = 12f;
    public float acceleration = 25f;
    public float deceleration = 20f;
    [Range(0f, 1f)]
    public float airControlMultiplier = 0.6f;

    [Header("Salto")]
    public float jumpForce = 16f;
    public float doubleJumpForce = 13f;
    public float fallGravityMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;

    [Header("Slide")]
    public float slideBoostSpeed = 18f;
    public float slideDuration = 0.5f;
    public float slideFriction = 3f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    // Estado Público
    public bool IsGrounded { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsFacingRight { get; private set; } = true;
    public bool IsSkidding { get; private set; }
    public float HorizontalInput { get; private set; }

    private Rigidbody2D _rb;
    private PlayerAnimatorController _anim;
    private PlayerHealth _health;

    private float _moveInput;
    private bool _jumpPressed;
    private bool _jumpHeld;
    private bool _slidePressed;

    private bool _canDoubleJump;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private float _slideTimer;
    private bool _wasGroundedLastFrame;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<PlayerAnimatorController>();
        _health = GetComponent<PlayerHealth>();
    }

    public void OnMove(InputValue value) {
        _moveInput = value.Get<Vector2>().x;
    }

    public void OnJump(InputValue value) {
        if (value.isPressed) {
            _jumpPressed = true;
            _jumpHeld = true;
        } else {
            _jumpHeld = false;
        }
    }

    public void OnSlide(InputValue value) {
        if (value.isPressed)
            _slidePressed = true;
    }

    private void Update() {
        if (_health.IsDead || _health.IsStunned) return;

        HorizontalInput = _moveInput;

        HandleFlip();
        HandleJumpBuffer();
        HandleCoyoteTime();
        HandleSlide();

        _jumpPressed = false;
        _slidePressed = false;
    }

    private void FixedUpdate() {
        CheckGrounded();
        ApplyMovement();
        ApplyGravityModifiers();
    }

    private void CheckGrounded() {
        _wasGroundedLastFrame = IsGrounded;
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (IsGrounded && !_wasGroundedLastFrame) {
            _canDoubleJump = true;
            _anim.OnLand();
        }
    }

    private void ApplyMovement() {
        if (IsSliding) {
            float newVelX = Mathf.MoveTowards(
                _rb.linearVelocity.x, 0f, slideFriction * Time.fixedDeltaTime
            );
            _rb.linearVelocity = new Vector2(newVelX, _rb.linearVelocity.y);
            return;
        }

        float targetSpeed = _moveInput * maxSpeed;
        float speedDiff = targetSpeed - _rb.linearVelocity.x;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f
            ? (IsGrounded ? acceleration : acceleration * airControlMultiplier)
            : (IsGrounded ? deceleration : deceleration * airControlMultiplier);

        _rb.AddForce(new Vector2(speedDiff * accelRate, 0f), ForceMode2D.Force);

        _rb.linearVelocity = new Vector2(
            Mathf.Clamp(_rb.linearVelocity.x, -maxSpeed, maxSpeed),
            _rb.linearVelocity.y
        );

        IsSkidding = IsGrounded
                     && Mathf.Abs(_rb.linearVelocity.x) > 2f
                     && _moveInput != 0
                     && Mathf.Sign(_moveInput) != Mathf.Sign(_rb.linearVelocity.x);
    }

    private void ApplyGravityModifiers() {
        if (_rb.linearVelocity.y < 0) {
            _rb.gravityScale = fallGravityMultiplier;
        } else if (_rb.linearVelocity.y > 0 && !_jumpHeld) {
            _rb.gravityScale = lowJumpMultiplier;
        } else {
            _rb.gravityScale = 1f;
        }
    }

    private void HandleCoyoteTime() {
        if (IsGrounded)
            _coyoteTimeCounter = coyoteTime;
        else
            _coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJumpBuffer() {
        if (_jumpPressed)
            _jumpBufferCounter = jumpBufferTime;

        _jumpBufferCounter -= Time.deltaTime;

        if (_jumpBufferCounter > 0f) {
            if (_coyoteTimeCounter > 0f) {
                PerformJump(jumpForce, isDoubleJump: false);
                _jumpBufferCounter = 0f;
                _coyoteTimeCounter = 0f;
            } else if (_canDoubleJump) {
                PerformJump(doubleJumpForce, isDoubleJump: true);
                _canDoubleJump = false;
                _jumpBufferCounter = 0f;
            }
        }
    }

    private void PerformJump(float force, bool isDoubleJump) {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        bool isLongJump = Mathf.Abs(_rb.linearVelocity.x) > maxSpeed * 0.5f;
        _anim.NotifyJump(isDoubleJump, isLongJump);
    }

    private void HandleSlide() {
        if (_slidePressed && IsGrounded && !IsSliding && Mathf.Abs(_rb.linearVelocity.x) > 2f)
            StartSlide();

        if (IsSliding) {
            _slideTimer -= Time.deltaTime;
            if (_slideTimer <= 0f || !IsGrounded)
                EndSlide();
        }
    }

    private void StartSlide() {
        IsSliding = true;
        _slideTimer = slideDuration;

        float dir = IsFacingRight ? 1f : -1f;
        _rb.linearVelocity = new Vector2(slideBoostSpeed * dir, _rb.linearVelocity.y);
    }

    private void EndSlide() {
        IsSliding = false;
    }

    private void HandleFlip() {
        if (_moveInput > 0 && !IsFacingRight) Flip();
        else if (_moveInput < 0 && IsFacingRight) Flip();
    }

    private void Flip() {
        IsFacingRight = !IsFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    public void ApplyKnockback(Vector2 direction, float force) {
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }

    public void StopHorizontalMovement() {
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected() {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}