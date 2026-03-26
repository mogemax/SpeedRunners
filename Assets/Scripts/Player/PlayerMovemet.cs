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
    //  Estas variables se llenan por los callbacks
    //  del Input System, no por polling directo.
    // ─────────────────────────────────────────────
    private float _moveInput;       // -1, 0 o 1 (horizontal)
    private bool  _jumpPressed;     // true el frame que se presionó Jump
    private bool  _jumpHeld;        // true mientras Jump está sostenido
    private bool  _slidePressed;    // true el frame que se presionó Slide/Down

    // ─────────────────────────────────────────────
    //  PRIVADOS — Lógica interna
    // ─────────────────────────────────────────────
    private bool  _canDoubleJump;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private float _slideTimer;
    private bool  _wasGroundedLastFrame;

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
    //
    //  Unity llama estos métodos automáticamente
    //  si el componente PlayerInput usa el Behavior
    //  "Send Messages" o "Broadcast Messages".
    //
    //  Nombre del método = "On" + nombre de la Action
    //  en el Input Action Asset.
    //
    //  Actions necesarias en el Asset:
    //    - Move      (Value, Vector2)
    //    - Jump      (Button)
    //    - Slide     (Button)
    // ─────────────────────────────────────────────

    /// <summary>Action: "Move" (Vector2 — usamos solo el eje X)</summary>
    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>().x;
    }

    /// <summary>Action: "Jump"</summary>
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            _jumpPressed = true;  // consumido en Update el mismo frame
            _jumpHeld    = true;
        }
        else
        {
            _jumpHeld = false;
        }
    }

    /// <summary>Action: "Slide" (botón dedicado o Down en el stick/teclado)</summary>
    public void OnSlide(InputValue value)
    {
        if (value.isPressed)
            _slidePressed = true;  // consumido en Update el mismo frame
    }

    // ─────────────────────────────────────────────
    //  UPDATE — Lógica por frame
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (_health.IsDead || _health.IsStunned) return;

        // Exponer input para que el AnimatorController lo lea
        HorizontalInput = _moveInput;

        HandleFlip();
        HandleJumpBuffer();
        HandleCoyoteTime();
        HandleSlide();

        // Consumir flags de "presionado este frame"
        // para que no se ejecuten en frames posteriores
        _jumpPressed  = false;
        _slidePressed = false;
    }

    // ─────────────────────────────────────────────
    //  FIXED UPDATE — Física
    // ─────────────────────────────────────────────
    private void FixedUpdate()
    {
        CheckGrounded();
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
            // Durante slide: solo fricción, sin control del jugador
            float newVelX = Mathf.MoveTowards(
                _rb.linearVelocity.x, 0f, slideFriction * Time.fixedDeltaTime
            );
            _rb.linearVelocity = new Vector2(newVelX, _rb.linearVelocity.y);
            return;
        }

        float targetSpeed  = _moveInput * maxSpeed;
        float speedDiff    = targetSpeed - _rb.linearVelocity.x;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f
            ? (IsGrounded ? acceleration : acceleration * airControlMultiplier)
            : (IsGrounded ? deceleration : deceleration * airControlMultiplier);

        _rb.AddForce(new Vector2(speedDiff * accelRate, 0f), ForceMode2D.Force);

        // Clamp de velocidad máxima
        _rb.linearVelocity = new Vector2(
            Mathf.Clamp(_rb.linearVelocity.x, -maxSpeed, maxSpeed),
            _rb.linearVelocity.y
        );

        // Detectar skid: cambio de dirección brusco en suelo
        IsSkidding = IsGrounded
                     && Mathf.Abs(_rb.linearVelocity.x) > 2f
                     && _moveInput != 0
                     && Mathf.Sign(_moveInput) != Mathf.Sign(_rb.linearVelocity.x);
    }

    // ─────────────────────────────────────────────
    //  GRAVEDAD MODIFICADA
    //  Usa _jumpHeld en vez de Input.GetButton
    // ─────────────────────────────────────────────
    private void ApplyGravityModifiers()
    {
        if (_rb.linearVelocity.y < 0)
        {
            // Caída más pesada
            _rb.gravityScale = fallGravityMultiplier;
        }
        else if (_rb.linearVelocity.y > 0 && !_jumpHeld)
        {
            // Soltar Jump temprano = salto corto
            _rb.gravityScale = lowJumpMultiplier;
        }
        else
        {
            _rb.gravityScale = 1f;
        }
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
                // Primer salto (o coyote jump)
                PerformJump(jumpForce, isDoubleJump: false);
                _jumpBufferCounter = 0f;
                _coyoteTimeCounter = 0f;
            }
            else if (_canDoubleJump)
            {
                // Doble salto
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
        _anim.OnJump(isDoubleJump, isLongJump);
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
        // El animator lee IsSliding directamente desde Update() — no hay llamada manual
    }

    private void EndSlide()
    {
        IsSliding = false;
        // Idem — Update() propaga el false al Animator automáticamente
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
    //  API PÚBLICA — llamada desde otros scripts
    // ─────────────────────────────────────────────

    /// <summary>Aplica knockback al recibir daño.</summary>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }

    /// <summary>Detiene movimiento horizontal bruscamente (usado en stun).</summary>
    public void StopHorizontalMovement()
    {
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    // ─────────────────────────────────────────────
    //  GIZMOS (debug visual en el editor)
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}