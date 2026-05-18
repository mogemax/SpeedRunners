using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el movimiento del personaje: correr, saltar, doble salto y slide.
/// Lee el estado de input desde PlayerInputReader — no conoce teclas ni mandos.
///
/// Setup requerido en el GameObject:
///   - Rigidbody2D
///   - Collider2D
///   - PlayerInput (component de Unity, apuntando al Input Action Asset)
///   - PlayerInputReader
///   - PlayerAnimatorController
///   - PlayerHealth
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerAnimatorController))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  MOVIMIENTO
    // ─────────────────────────────────────────────
    [Header("Movimiento")]
    [Tooltip("Velocidad maxima horizontal")]
    public float maxSpeed = 12f;

    [Tooltip("Aceleracion lineal al correr (m/s²)")]
    public float acceleration = 25f;

    [Tooltip("Desaceleracion lineal al soltar input (m/s²)")]
    public float deceleration = 20f;

    [Tooltip("Multiplicador de control en el aire")]
    [Range(0f, 1f)]
    public float airControlMultiplier = 0.6f;

    // ─────────────────────────────────────────────
    //  SALTO
    // ─────────────────────────────────────────────
    [Header("Salto")]
    public float jumpForce = 16f;
    public float doubleJumpForce = 13f;

    [Tooltip("Multiplicador de gravedad al caer")]
    public float fallGravityMultiplier = 2.5f;

    [Tooltip("Multiplicador de gravedad al soltar salto temprano")]
    public float lowJumpMultiplier = 2f;

    [Tooltip("Segundos que puedes saltar tras salir de plataforma")]
    public float coyoteTime = 0.12f;

    [Tooltip("Segundos de buffer de input de salto antes de tocar suelo")]
    public float jumpBufferTime = 0.15f;

    // ─────────────────────────────────────────────
    //  SLIDE
    // ─────────────────────────────────────────────
    [Header("Slide")]
    public float slideBoostSpeed = 18f;
    public float slideDuration = 0.5f;

    [Tooltip("Desaceleracion durante el slide (m/s²)")]
    public float slideFriction = 3f;

    // ─────────────────────────────────────────────
    //  GROUND CHECK
    // ─────────────────────────────────────────────
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    // ─────────────────────────────────────────────
    //  PENDIENTES
    // ─────────────────────────────────────────────
    [Header("Pendientes")]
    [Tooltip("Angulo maximo de pendiente transitable (grados)")]
    public float maxSlopeAngle = 50f;

    [Tooltip("Distancia del raycast de deteccion de pendiente")]
    public float slopeCheckDistance = 0.3f;

    // ─────────────────────────────────────────────
    //  ESTADO PUBLICO
    // ─────────────────────────────────────────────
    public bool  IsGrounded     { get; private set; }
    public bool  IsSliding      { get; private set; }
    public bool  IsFacingRight  { get; private set; } = true;
    public bool  IsSkidding     { get; private set; }
    public bool  IsOnSlope      { get; private set; }
    public float HorizontalInput { get; private set; }

    // ─────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────
    private Rigidbody2D              _rb;
    private PlayerInputReader        _input;
    private PlayerAnimatorController _anim;
    private PlayerHealth             _health;
    private PlayerGrapple            _grapple;
    private PlayerStamina            _stamina;

    // ─────────────────────────────────────────────
    //  ESTADO INTERNO — salto
    // ─────────────────────────────────────────────
    private bool  _canDoubleJump;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private bool  _wasGroundedLastFrame;

    // ─────────────────────────────────────────────
    //  ESTADO INTERNO — slide
    // ─────────────────────────────────────────────
    private float _slideTimer;

    // ─────────────────────────────────────────────
    //  ESTADO INTERNO — pendientes
    // ─────────────────────────────────────────────
    private Vector2 _slopeNormal = Vector2.up;

    // ─────────────────────────────────────────────
    //  FREEZE
    // ─────────────────────────────────────────────
    private bool _isFrozen = false;

    public void SetFrozen(bool frozen)
    {
        _isFrozen = frozen;

        if (frozen)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _input.ResetInput();
        }
    }

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _rb      = GetComponent<Rigidbody2D>();
        _input   = GetComponent<PlayerInputReader>();
        _anim    = GetComponent<PlayerAnimatorController>();
        _health  = GetComponent<PlayerHealth>();
        _grapple = GetComponent<PlayerGrapple>();
        _stamina = GetComponent<PlayerStamina>();
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (_health.IsDead || _health.IsStunned || _isFrozen) return;

        HorizontalInput = _input.MoveInput;

        HandleFlip();
        HandleJumpBuffer();
        HandleCoyoteTime();
        HandleSlide();
    }

    // ─────────────────────────────────────────────
    //  FIXED UPDATE
    // ─────────────────────────────────────────────
    private void FixedUpdate()
    {
        CheckGrounded();
        DetectSlope();

        if (_isFrozen) return;

        ApplyMovement();
        ApplyGravityModifiers();
    }

    // ─────────────────────────────────────────────
    //  DETECCION DE SUELO
    // ─────────────────────────────────────────────
    private void CheckGrounded()
    {
        _wasGroundedLastFrame = IsGrounded;
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (IsGrounded && !_wasGroundedLastFrame)
        {
            _canDoubleJump = true;
            _anim.OnLand();
        }
    }

    // ─────────────────────────────────────────────
    //  DETECCION DE PENDIENTE
    //  Raycast hacia abajo desde groundCheck para obtener
    //  la normal del suelo. Solo se ejecuta cuando hay suelo.
    // ─────────────────────────────────────────────
    private void DetectSlope()
    {
        if (!IsGrounded)
        {
            IsOnSlope    = false;
            _slopeNormal = Vector2.up;
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position, Vector2.down, slopeCheckDistance, groundLayer
        );

        if (hit.collider != null)
        {
            _slopeNormal = hit.normal;
            float angle  = Vector2.Angle(hit.normal, Vector2.up);
            IsOnSlope    = angle > 1f && angle <= maxSlopeAngle;
        }
        else
        {
            _slopeNormal = Vector2.up;
            IsOnSlope    = false;
        }
    }

    // ─────────────────────────────────────────────
    //  MOVIMIENTO HORIZONTAL
    // ─────────────────────────────────────────────
    private void ApplyMovement()
    {
        if (IsSliding)
        {
            float slidingX = Mathf.MoveTowards(
                _rb.linearVelocity.x, 0f, slideFriction * Time.fixedDeltaTime
            );
            _rb.linearVelocity = new Vector2(slidingX, _rb.linearVelocity.y);
            return;
        }

        if (IsGrounded && IsOnSlope)
            ApplyMovementOnSlope();
        else
            ApplyMovementFlat();

        IsSkidding = IsGrounded
                     && Mathf.Abs(_rb.linearVelocity.x) > 2f
                     && _input.MoveInput != 0
                     && Mathf.Sign(_input.MoveInput) != Mathf.Sign(_rb.linearVelocity.x);
    }

    private void ApplyMovementFlat()
    {
        float staminaMult = _stamina != null ? _stamina.GetSpeedMultiplier() : 1f;
        float targetSpeed = _input.MoveInput * maxSpeed * staminaMult;

        float accelRate = Mathf.Abs(_input.MoveInput) > 0.01f
            ? (IsGrounded ? acceleration : acceleration * airControlMultiplier)
            : (IsGrounded ? deceleration : deceleration * airControlMultiplier);

        float newX = Mathf.MoveTowards(
            _rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime
        );
        _rb.linearVelocity = new Vector2(newX, _rb.linearVelocity.y);
    }

    private void ApplyMovementOnSlope()
    {
        // Tangente de la pendiente apuntando hacia la derecha
        // Si normal = (nx, ny) → tangente = (ny, -nx)
        Vector2 slopeTangent = new Vector2(_slopeNormal.y, -_slopeNormal.x);

        // Velocidad objetivo a lo largo de la tangente
        float staminaMult = _stamina != null ? _stamina.GetSpeedMultiplier() : 1f;
        Vector2 targetVelocity = slopeTangent * (_input.MoveInput * maxSpeed * staminaMult);

        float accelRate = Mathf.Abs(_input.MoveInput) > 0.01f ? acceleration : deceleration;

        Vector2 newVelocity = Vector2.MoveTowards(
            _rb.linearVelocity, targetVelocity, accelRate * Time.fixedDeltaTime
        );

        // Sin input: anular Y para evitar que resbale por gravedad
        if (Mathf.Abs(_input.MoveInput) < 0.01f)
            newVelocity.y = 0f;

        _rb.linearVelocity = newVelocity;
    }

    // ─────────────────────────────────────────────
    //  GRAVEDAD MODIFICADA
    //  En pendiente con suelo: gravedad desactivada
    //  para evitar que Rigidbody luche contra la normal.
    // ─────────────────────────────────────────────
    private void ApplyGravityModifiers()
    {
        if (_grapple != null && _grapple.IsGrappling) return;

        if (IsGrounded && IsOnSlope && !IsSliding)
        {
            _rb.gravityScale = 0f;
            return;
        }

        if (_rb.linearVelocity.y < 0)
            _rb.gravityScale = fallGravityMultiplier;
        else if (_rb.linearVelocity.y > 0 && !_input.JumpHeld)
            _rb.gravityScale = lowJumpMultiplier;
        else
            _rb.gravityScale = 1f;
    }

    // ─────────────────────────────────────────────
    //  COYOTE TIME
    // ─────────────────────────────────────────────
    private void HandleCoyoteTime()
    {
        if (IsGrounded)
            _coyoteTimeCounter = coyoteTime;
        else
            _coyoteTimeCounter -= Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    //  JUMP BUFFER + EJECUCION DE SALTO
    // ─────────────────────────────────────────────
    private void HandleJumpBuffer()
    {
        if (_input.JumpPressed)
            _jumpBufferCounter = jumpBufferTime;

        _jumpBufferCounter -= Time.deltaTime;

        if (_jumpBufferCounter > 0f)
        {
            if (_coyoteTimeCounter > 0f)
            {
                PerformJump(jumpForce, isDoubleJump: false);
                _jumpBufferCounter = 0f;
                _coyoteTimeCounter = 0f;
            }
            else if (_canDoubleJump)
            {
                PerformJump(doubleJumpForce, isDoubleJump: true);
                _canDoubleJump     = false;
                _jumpBufferCounter = 0f;
            }
        }
    }

    private void PerformJump(float force, bool isDoubleJump)
    {
        // Restaurar gravedad antes de saltar para que el impulso no
        // quede anulado por gravityScale = 0 en pendiente
        _rb.gravityScale = 1f;

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        bool isLongJump = Mathf.Abs(_rb.linearVelocity.x) > maxSpeed * 0.5f;
        _anim.NotifyJump(isDoubleJump, isLongJump);
    }

    // ─────────────────────────────────────────────
    //  SLIDE
    // ─────────────────────────────────────────────
    private void HandleSlide()
    {
        if (_input.SlidePressed && IsGrounded && !IsSliding && Mathf.Abs(_rb.linearVelocity.x) > 2f)
            StartSlide();

        if (IsSliding)
        {
            _slideTimer -= Time.deltaTime;
            if (_slideTimer <= 0f || !IsGrounded)
                EndSlide();
        }
    }

    private void StartSlide()
    {
        IsSliding   = true;
        _slideTimer = slideDuration;

        float dir = IsFacingRight ? 1f : -1f;
        _rb.linearVelocity = new Vector2(slideBoostSpeed * dir, _rb.linearVelocity.y);
    }

    private void EndSlide()
    {
        IsSliding = false;
    }

    // ─────────────────────────────────────────────
    //  FLIP DE SPRITE
    // ─────────────────────────────────────────────
    private void HandleFlip()
    {
        if      (_input.MoveInput > 0 && !IsFacingRight) Flip();
        else if (_input.MoveInput < 0 &&  IsFacingRight) Flip();
    }

    private void Flip()
    {
        IsFacingRight        = !IsFacingRight;
        Vector3 scale        = transform.localScale;
        scale.x             *= -1f;
        transform.localScale = scale;
    }

    // ─────────────────────────────────────────────
    //  API PUBLICA
    // ─────────────────────────────────────────────

    public void ApplyKnockback(Vector2 direction, float force)
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }

    public void StopHorizontalMovement()
    {
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(groundCheck.position, Vector2.down * slopeCheckDistance);
    }
}
