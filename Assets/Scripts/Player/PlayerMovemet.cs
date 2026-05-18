using UnityEngine;
using UnityEngine.InputSystem;

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
    public float maxSpeed = 12f;
    public float acceleration = 25f;
    public float deceleration = 20f;
    [Range(0f, 1f)]
    public float airControlMultiplier = 0.6f;

    // ─────────────────────────────────────────────
    //  SALTO
    // ─────────────────────────────────────────────
    [Header("Salto")]
    public float jumpForce = 16f;
    public float doubleJumpForce = 13f;
    public float fallGravityMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;

    // ─────────────────────────────────────────────
    //  WALL JUMP / WALL SLIDE  ← NUEVO
    // ─────────────────────────────────────────────
    [Header("Wall Jump")]
    [Tooltip("Transform en el lado del personaje que detecta paredes")]
    public Transform wallCheck;

    [Tooltip("Radio del OverlapCircle para detectar pared")]
    public float wallCheckRadius = 0.15f;

    [Tooltip("Layer de las paredes (puede ser la misma que groundLayer)")]
    public LayerMask wallLayer;

    [Tooltip("Velocidad horizontal al saltar de la pared (alejándose)")]
    public float wallJumpForceX = 10f;

    [Tooltip("Velocidad vertical al saltar de la pared")]
    public float wallJumpForceY = 16f;

    [Tooltip("Velocidad máxima de deslizamiento hacia abajo en la pared")]
    public float wallSlideSpeed = 2f;

    [Tooltip("Tiempo (s) en que el input horizontal queda bloqueado tras el wall jump, " +
             "para que el personaje se separe bien de la pared")]
    public float wallJumpControlLockTime = 0.2f;

    // ─────────────────────────────────────────────
    //  SLIDE
    // ─────────────────────────────────────────────
    [Header("Slide")]
    public float slideBoostSpeed = 18f;
    public float slideDuration = 0.5f;
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
    public float maxSlopeAngle = 50f;
    public float slopeCheckDistance = 0.3f;

    // ─────────────────────────────────────────────
    //  ESTADO PUBLICO
    // ─────────────────────────────────────────────
    public bool IsGrounded { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsFacingRight { get; private set; } = true;
    public bool IsSkidding { get; private set; }
    public bool IsOnSlope { get; private set; }
    public bool IsWallSliding { get; private set; }   // ← NUEVO: útil para el Animator
    public float HorizontalInput { get; private set; }

    // ─────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────
    private Rigidbody2D _rb;
    private PlayerInputReader _input;
    private PlayerAnimatorController _anim;
    private PlayerHealth _health;
    private PlayerGrapple _grapple;

    // ─────────────────────────────────────────────
    //  ESTADO INTERNO — salto
    // ─────────────────────────────────────────────
    private bool _canDoubleJump;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private bool _wasGroundedLastFrame;

    // ─────────────────────────────────────────────
    //  ESTADO INTERNO — slide
    // ─────────────────────────────────────────────
    private float _slideTimer;

    // ─────────────────────────────────────────────
    //  ESTADO INTERNO — wall jump  ← NUEVO
    // ─────────────────────────────────────────────
    private bool _isTouchingWall;
    private float _wallJumpLockTimer;   // mientras > 0 el input H está bloqueado

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
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputReader>();
        _anim = GetComponent<PlayerAnimatorController>();
        _health = GetComponent<PlayerHealth>();
        _grapple = GetComponent<PlayerGrapple>();
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

        // Bajar el lock timer del wall jump
        if (_wallJumpLockTimer > 0f)
            _wallJumpLockTimer -= Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    //  FIXED UPDATE
    // ─────────────────────────────────────────────
    private void FixedUpdate()
    {
        CheckGrounded();
        CheckWall();          // ← NUEVO
        DetectSlope();

        if (_isFrozen) return;

        HandleWallSlide();    // ← NUEVO
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
    //  DETECCION DE PARED  ← NUEVO
    //  El wallCheck debe estar en el lado del personaje
    //  (hijo del GameObject, offset en X hacia el frente).
    //  Como el sprite se flipea con localScale.x, el
    //  wallCheck se mueve automáticamente con él.
    // ─────────────────────────────────────────────
    private void CheckWall()
    {
        if (wallCheck == null) { _isTouchingWall = false; return; }

        _isTouchingWall = !IsGrounded &&
                          Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);
    }

    // ─────────────────────────────────────────────
    //  WALL SLIDE  ← NUEVO
    //  Limita la velocidad de caída mientras se toca
    //  la pared. También recarga el doble salto.
    // ─────────────────────────────────────────────
    private void HandleWallSlide()
    {
        // Condición: tocando pared, cayendo, sin estar en el suelo
        bool shouldWallSlide = _isTouchingWall && _rb.linearVelocity.y < 0f;

        IsWallSliding = shouldWallSlide;

        if (IsWallSliding)
        {
            // Recargar doble salto al entrar en wall slide
            _canDoubleJump = true;

            // Limitar la velocidad de caída al wallSlideSpeed
            if (_rb.linearVelocity.y < -wallSlideSpeed)
            {
                _rb.linearVelocity = new Vector2(
                    _rb.linearVelocity.x,
                    -wallSlideSpeed
                );
            }
        }
    }

    // ─────────────────────────────────────────────
    //  DETECCION DE PENDIENTE
    // ─────────────────────────────────────────────
    private void DetectSlope()
    {
        if (!IsGrounded)
        {
            IsOnSlope = false;
            _slopeNormal = Vector2.up;
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position, Vector2.down, slopeCheckDistance, groundLayer
        );

        if (hit.collider != null)
        {
            _slopeNormal = hit.normal;
            float angle = Vector2.Angle(hit.normal, Vector2.up);
            IsOnSlope = angle > 1f && angle <= maxSlopeAngle;
        }
        else
        {
            _slopeNormal = Vector2.up;
            IsOnSlope = false;
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

        // Durante el lock del wall jump no se aplica input horizontal,
        // el impulso ya se encarga de alejarte de la pared
        if (_wallJumpLockTimer > 0f) return;   // ← NUEVO

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
        float targetSpeed = _input.MoveInput * maxSpeed;

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
        Vector2 slopeTangent = new Vector2(_slopeNormal.y, -_slopeNormal.x);
        Vector2 targetVelocity = slopeTangent * (_input.MoveInput * maxSpeed);

        float accelRate = Mathf.Abs(_input.MoveInput) > 0.01f ? acceleration : deceleration;

        Vector2 newVelocity = Vector2.MoveTowards(
            _rb.linearVelocity, targetVelocity, accelRate * Time.fixedDeltaTime
        );

        if (Mathf.Abs(_input.MoveInput) < 0.01f)
            newVelocity.y = 0f;

        _rb.linearVelocity = newVelocity;
    }

    // ─────────────────────────────────────────────
    //  GRAVEDAD MODIFICADA
    // ─────────────────────────────────────────────
    private void ApplyGravityModifiers()
    {
        if (_grapple != null && _grapple.IsGrappling) return;

        if (IsGrounded && IsOnSlope && !IsSliding)
        {
            _rb.gravityScale = 0f;
            return;
        }

        // Wall slide: reducir gravedad para que el deslizamiento
        // quede controlado por la velocidad que fijamos arriba
        if (IsWallSliding)                    // ← NUEVO
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
            // ── Wall jump tiene prioridad sobre salto normal ── NUEVO
            if (IsWallSliding)
            {
                PerformWallJump();
                _jumpBufferCounter = 0f;
                return;
            }

            if (_coyoteTimeCounter > 0f)
            {
                PerformJump(jumpForce, isDoubleJump: false);
                _jumpBufferCounter = 0f;
                _coyoteTimeCounter = 0f;
            }
            else if (_canDoubleJump)
            {
                PerformJump(doubleJumpForce, isDoubleJump: true);
                _canDoubleJump = false;
                _jumpBufferCounter = 0f;
            }
        }
    }

    private void PerformJump(float force, bool isDoubleJump)
    {
        _rb.gravityScale = 1f;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        bool isLongJump = Mathf.Abs(_rb.linearVelocity.x) > maxSpeed * 0.5f;
        _anim.NotifyJump(isDoubleJump, isLongJump);
    }

    // ─────────────────────────────────────────────
    //  WALL JUMP  ← NUEVO
    //  Lanza al personaje en diagonal opuesta a la pared.
    //  IsFacingRight indica hacia qué lado está mirando,
    //  o sea hacia qué lado está la pared → saltamos al opuesto.
    // ─────────────────────────────────────────────
    private void PerformWallJump()
    {
        _rb.gravityScale = 1f;

        // Dirección opuesta a la pared
        float directionAwayFromWall = IsFacingRight ? -1f : 1f;

        _rb.linearVelocity = new Vector2(
            directionAwayFromWall * wallJumpForceX,
            wallJumpForceY
        );

        // Bloquear input horizontal brevemente para que el arco
        // de salto se vea limpio y el jugador no "pegue" a la pared
        _wallJumpLockTimer = wallJumpControlLockTime;

        // Flip para mirar hacia donde va
        if ((directionAwayFromWall > 0 && !IsFacingRight) ||
            (directionAwayFromWall < 0 && IsFacingRight))
            Flip();

        _anim.NotifyJump(isDoubleJump: false, isLongJump: true);
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
        IsSliding = true;
        _slideTimer = slideDuration;
        float dir = IsFacingRight ? 1f : -1f;
        _rb.linearVelocity = new Vector2(slideBoostSpeed * dir, _rb.linearVelocity.y);
    }

    private void EndSlide() => IsSliding = false;

    // ─────────────────────────────────────────────
    //  FLIP DE SPRITE
    // ─────────────────────────────────────────────
    private void HandleFlip()
    {
        // No flipear mientras el wall jump lock está activo
        if (_wallJumpLockTimer > 0f) return;   // ← NUEVO

        if (_input.MoveInput > 0 && !IsFacingRight) Flip();
        else if (_input.MoveInput < 0 && IsFacingRight) Flip();
    }

    private void Flip()
    {
        IsFacingRight = !IsFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
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
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(groundCheck.position, Vector2.down * slopeCheckDistance);
        }

        // Visualizar el wallCheck en el editor  ← NUEVO
        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        }
    }
}