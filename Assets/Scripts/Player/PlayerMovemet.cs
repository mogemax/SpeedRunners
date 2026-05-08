using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el movimiento del personaje: correr, saltar, doble salto y slide.
/// Compatible con el nuevo Input System de Unity — soporta hasta 4 jugadores
/// con teclado o mando sin conflictos entre ellos.
///
/// Setup requerido en el GameObject:
///   - Rigidbody2D
///   - Collider2D
///   - PlayerInput (component de Unity, apuntando al Input Action Asset)
///   - PlayerAnimatorController
///   - PlayerHealth
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerAnimatorController))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  PARÁMETROS DE MOVIMIENTO
    // ─────────────────────────────────────────────
    [Header("Movimiento")]
    [Tooltip("Velocidad máxima horizontal")]
    public float maxSpeed = 12f;

    [Tooltip("Qué tan rápido acelera el personaje")]
    public float acceleration = 25f;

    [Tooltip("Qué tan rápido desacelera al soltar input")]
    public float deceleration = 20f;

    [Tooltip("Multiplicador de velocidad en el aire (menos control)")]
    [Range(0f, 1f)]
    public float airControlMultiplier = 0.6f;

    // ─────────────────────────────────────────────
    //  PARÁMETROS DE SALTO
    // ─────────────────────────────────────────────
    [Header("Salto")]
    [Tooltip("Fuerza del primer salto")]
    public float jumpForce = 16f;

    [Tooltip("Fuerza del doble salto (un poco menor)")]
    public float doubleJumpForce = 13f;

    [Tooltip("Multiplicador de gravedad al caer (caída más pesada)")]
    public float fallGravityMultiplier = 2.5f;

    [Tooltip("Multiplicador de gravedad al soltar salto temprano")]
    public float lowJumpMultiplier = 2f;

    [Tooltip("Tiempo de coyote (segundos que puedes saltar tras salir de plataforma)")]
    public float coyoteTime = 0.12f;

    [Tooltip("Buffer de input de salto (segundos antes de tocar suelo)")]
    public float jumpBufferTime = 0.15f;

    // ─────────────────────────────────────────────
    //  PARÁMETROS DE SLIDE
    // ─────────────────────────────────────────────
    [Header("Slide")]
    [Tooltip("Velocidad adicional al iniciar slide")]
    public float slideBoostSpeed = 18f;

    [Tooltip("Duración máxima del slide")]
    public float slideDuration = 0.5f;

    [Tooltip("Fricción durante el slide")]
    public float slideFriction = 3f;

    // ─────────────────────────────────────────────
    //  DETECCIÓN DE SUELO
    // ─────────────────────────────────────────────
    [Header("Ground Check")]
    [Tooltip("Punto desde donde se detecta el suelo")]
    public Transform groundCheck;

    [Tooltip("Radio de detección de suelo")]
    public float groundCheckRadius = 0.15f;

    [Tooltip("Layers que se consideran suelo")]
    public LayerMask groundLayer;

    // ─────────────────────────────────────────────
    //  ESTADO PÚBLICO (leído por otros scripts)
    // ─────────────────────────────────────────────
    public bool IsGrounded       { get; private set; }
    public bool IsSliding        { get; private set; }
    public bool IsFacingRight    { get; private set; } = true;
    public bool IsSkidding       { get; private set; }
    public float HorizontalInput { get; private set; }

    // ─────────────────────────────────────────────
    //  PRIVADOS — Referencias
    // ─────────────────────────────────────────────
    private Rigidbody2D              _rb;
    private PlayerAnimatorController _anim;
    private PlayerHealth             _health;

    // ─────────────────────────────────────────────
    //  PRIVADOS — Estado de input
    // ─────────────────────────────────────────────
    private float _moveInput;
    private bool  _jumpPressed;
    private bool  _jumpHeld;
    private bool  _slidePressed;

    // ─────────────────────────────────────────────
    //  PRIVADOS — Lógica interna
    // ─────────────────────────────────────────────
    private bool  _canDoubleJump;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private float _slideTimer;
    private bool  _wasGroundedLastFrame;

    // ─────────────────────────────────────────────
    //  FREEZE — bloqueado por RaceManager durante
    //  el countdown entre rondas. La física sigue
    //  activa (el personaje cae normalmente) pero
    //  no se acepta ningún input del jugador.
    // ─────────────────────────────────────────────
    private bool _isFrozen = false;

    /// <summary>
    /// Congela o descongela el input del jugador.
    /// Llamado por RaceManager al inicio/fin del countdown.
    /// La física (gravedad, colisiones) sigue funcionando.
    /// </summary>
    public void SetFrozen(bool frozen)
    {
        _isFrozen = frozen;

        if (frozen)
        {
            // Detener velocidad horizontal al congelar
            // para que no siga deslizándose
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

            // Limpiar inputs buffereados para que al descongelar
            // no se ejecute un salto o slide que se presionó antes
            _jumpPressed  = false;
            _jumpHeld     = false;
            _slidePressed = false;
            _moveInput    = 0f;
        }
    }

    // ─────────────────────────────────────────────
    //  INICIALIZACIÓN
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _rb     = GetComponent<Rigidbody2D>();
        _anim   = GetComponent<PlayerAnimatorController>();
        _health = GetComponent<PlayerHealth>();
    }

    // ─────────────────────────────────────────────
    //  CALLBACKS DEL INPUT SYSTEM
    // ─────────────────────────────────────────────

    public void OnMove(InputValue value)
    {
        // Si está congelado ignoramos el input de movimiento
        if (_isFrozen) return;
        _moveInput = value.Get<Vector2>().x;
    }

    public void OnJump(InputValue value)
    {
        if (_isFrozen) return;

        if (value.isPressed)
        {
            _jumpPressed = true;
            _jumpHeld    = true;
        }
        else
        {
            _jumpHeld = false;
        }
    }

    public void OnSlide(InputValue value)
    {
        if (_isFrozen) return;
        if (value.isPressed)
            _slidePressed = true;
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (_health.IsDead || _isFrozen) return;

        HorizontalInput = _moveInput;

        HandleFlip();
        HandleJumpBuffer();
        HandleCoyoteTime();
        HandleSlide();

        _jumpPressed  = false;
        _slidePressed = false;
    }

    // ─────────────────────────────────────────────
    //  FIXED UPDATE
    // ─────────────────────────────────────────────
    private void FixedUpdate()
    {
        CheckGrounded();

        // Durante freeze solo chequeamos suelo —
        // no aplicamos movimiento ni gravedad modificada
        if (_isFrozen) return;

        ApplyMovement();
        ApplyGravityModifiers();
    }

    // ─────────────────────────────────────────────
    //  DETECCIÓN DE SUELO
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
    //  MOVIMIENTO HORIZONTAL
    // ─────────────────────────────────────────────
    private void ApplyMovement()
    {
        if (IsSliding)
        {
            float newVelX = Mathf.MoveTowards(
                _rb.linearVelocity.x, 0f, slideFriction * Time.fixedDeltaTime
            );
            _rb.linearVelocity = new Vector2(newVelX, _rb.linearVelocity.y);
            return;
        }

        float targetSpeed = _moveInput * maxSpeed;
        float speedDiff   = targetSpeed - _rb.linearVelocity.x;

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

    // ─────────────────────────────────────────────
    //  GRAVEDAD MODIFICADA
    // ─────────────────────────────────────────────
    private void ApplyGravityModifiers()
    {
        if (_rb.linearVelocity.y < 0)
            _rb.gravityScale = fallGravityMultiplier;
        else if (_rb.linearVelocity.y > 0 && !_jumpHeld)
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
    //  JUMP BUFFER + EJECUCIÓN DE SALTO
    // ─────────────────────────────────────────────
    private void HandleJumpBuffer()
    {
        if (_jumpPressed)
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
        if (_slidePressed && IsGrounded && !IsSliding && Mathf.Abs(_rb.linearVelocity.x) > 2f)
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
        if      (_moveInput > 0 && !IsFacingRight) Flip();
        else if (_moveInput < 0 &&  IsFacingRight) Flip();
    }

    private void Flip()
    {
        IsFacingRight        = !IsFacingRight;
        Vector3 scale        = transform.localScale;
        scale.x             *= -1f;
        transform.localScale = scale;
    }

    // ─────────────────────────────────────────────
    //  API PÚBLICA
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
    }
}
