using UnityEngine;
/// <summary>
/// Controla el Animator del personaje.
/// Todas las transiciones de animación pasan por aquí.
///
/// Nombres de estados en el Animator (deben coincidir exactamente):
///   Stand, Run, Skid, Straight-Jump, Long-Jump, Double-Jump,
///   Straight-Fall, Long-Fall, Double-Jump-Fall,
///   Slide, Roll, Hookshot, Running-Hook, Swing,
///   Wall-Hang, Flip, Grabbed, Spiked, Tumble, Taunt
/// </summary>
[RequireComponent(typeof(Animator))]

public class PlayerAnimatorController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  PARÁMETROS DEL ANIMATOR — todos hasheados
    //  (nunca usar strings crudos: si hay un typo
    //   Unity no lanza error, simplemente no funciona)
    // ─────────────────────────────────────────────
    private static readonly int SpeedX        = Animator.StringToHash("SpeedX");
    private static readonly int SpeedY        = Animator.StringToHash("SpeedY");
    private static readonly int IsGrounded    = Animator.StringToHash("IsGrounded");
    private static readonly int IsSliding     = Animator.StringToHash("IsSliding");
    private static readonly int IsSkidding    = Animator.StringToHash("IsSkidding");
    private static readonly int IsDoubleJump  = Animator.StringToHash("IsDoubleJump");
    private static readonly int IsLongJump    = Animator.StringToHash("IsLongJump");
    private static readonly int IsLongFall    = Animator.StringToHash("IsLongFall");    // FIX: era string crudo
    private static readonly int IsRunningHook = Animator.StringToHash("IsRunningHook"); // FIX: era string crudo
    private static readonly int IsHookActive  = Animator.StringToHash("IsHookActive");
    private static readonly int IsSwinging    = Animator.StringToHash("IsSwinging");
    private static readonly int IsWallHang    = Animator.StringToHash("IsWallHang");
    private static readonly int IsGrabbed     = Animator.StringToHash("IsGrabbed");
    private static readonly int TriggerJump   = Animator.StringToHash("TriggerJump");
    private static readonly int TriggerLand   = Animator.StringToHash("TriggerLand");
    private static readonly int TriggerRoll   = Animator.StringToHash("TriggerRoll");
    private static readonly int TriggerHurt   = Animator.StringToHash("TriggerHurt");
    private static readonly int TriggerDeath  = Animator.StringToHash("TriggerDeath");
    private static readonly int TriggerTaunt  = Animator.StringToHash("TriggerTaunt");
    private static readonly int HurtType      = Animator.StringToHash("HurtType");
    aaaaa
    // ─────────────────────────────────────────────
    //  TIPO DE DAÑO
    //  Mapea a la animación de hurt correcta.
    //  El valor entero debe coincidir con lo que
    //  configures como condición en el Animator.
    //    0 = Spiked   → animación "Spiked"
    //    1 = Tumble   → animación "Tumble"
    //    2 = Grabbed  → animación "Grabbed"
    // ─────────────────────────────────────────────
    public enum HurtAnimationType { Spiked = 0, Tumble = 1, Grabbed = 2 }
    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN
    // ─────────────────────────────────────────────
    [Header("Umbrales")]
    [Tooltip("Velocidad Y negativa a partir de la cual se activa Long-Fall")]
    public float longFallThreshold = -8f;
    [Tooltip("Velocidad X mínima para considerar que corre al lanzar el gancho")]
    public float runningHookThreshold = 3f;
    [Tooltip("Velocidad X mínima para considerar hard landing (Roll)")]
    public float hardLandingThreshold = 8f;
    // ─────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────
    private Animator         _animator;
    private PlayerMovement   _movement;
    private Rigidbody2D      _rb;
    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<PlayerMovement>();
        _rb       = GetComponent<Rigidbody2D>();
    }
    // ─────────────────────────────────────────────
    //  UPDATE — parámetros continuos cada frame
    //
    //  Solo se actualiza lo que cambia frame a frame.
    //  Los eventos puntuales (salto, daño, etc.) van
    //  en sus propios métodos públicos más abajo.
    // ─────────────────────────────────────────────
    private void Update()
    {
        UpdateLocomotionParameters();
        UpdateAirParameters();
    }
    private void UpdateLocomotionParameters()
    {
        _animator.SetFloat(SpeedX,     Mathf.Abs(_rb.linearVelocity.x));
        _animator.SetBool (IsGrounded, _movement.IsGrounded);
        _animator.SetBool (IsSliding,  _movement.IsSliding);   // Update ya cubre OnSlideStart/End
        _animator.SetBool (IsSkidding, _movement.IsSkidding);
    }
    private void UpdateAirParameters()
    {
        _animator.SetFloat(SpeedY, _rb.linearVelocity.y);
        // Long-Fall: caída rápida con velocidad horizontal suficiente
        bool isLongFall = _rb.linearVelocity.y < longFallThreshold
                        && Mathf.Abs(_rb.linearVelocity.x) > 3f;
        _animator.SetBool(IsLongFall, isLongFall);
    }
    // ─────────────────────────────────────────────
    //  SALTO — llamado desde PlayerMovement
    // ─────────────────────────────────────────────
    /// <summary>
    /// Dispara la animación de salto correcta.
    /// PlayerMovement decide si es doble salto y si hay impulso horizontal.
    /// </summary>
    public void OnJump(bool isDoubleJump, bool isLongJump)
    {
        _animator.SetBool(IsDoubleJump, isDoubleJump);
        _animator.SetBool(IsLongJump,   isLongJump);
        _animator.SetTrigger(TriggerJump);
    }
    // ─────────────────────────────────────────────
    //  ATERRIZAJE — llamado desde PlayerMovement
    // ─────────────────────────────────────────────
    /// <summary>
    /// Decide entre aterrizaje normal (TriggerLand) o
    /// aterrizaje con roll (TriggerRoll) según la velocidad horizontal.
    /// </summary>
    public void OnLand()
    {
        if (Mathf.Abs(_rb.linearVelocity.x) > hardLandingThreshold)
            _animator.SetTrigger(TriggerRoll);
        else
            _animator.SetTrigger(TriggerLand);
    }
    // ─────────────────────────────────────────────
    //  DAÑO — llamado desde PlayerHealth
    // ─────────────────────────────────────────────
    /// <summary>
    /// Setea el tipo de daño ANTES de disparar el trigger,
    /// así el Animator sabe a qué estado transicionar.
    /// </summary>
    public void OnHurt(HurtAnimationType hurtType)
    {
        _animator.SetInteger(HurtType,    (int)hurtType);
        _animator.SetTrigger(TriggerHurt);
    }
    /// <summary>Reproduce animación de muerte.</summary>
    public void OnDeath()
    {
        _animator.SetTrigger(TriggerDeath);
    }
    // ─────────────────────────────────────────────
    //  AGARRADO POR OTRO JUGADOR — llamado desde PlayerHealth
    // ─────────────────────────────────────────────
    /// <summary>
    /// Activa/desactiva el estado Grabbed.
    /// FIX: ahora setea HurtType = Grabbed antes del trigger,
    /// antes disparaba TriggerHurt con el tipo anterior.
    /// </summary>
    public void OnGrabbed(bool grabbed)
    {
        _animator.SetBool(IsGrabbed, grabbed);
        if (grabbed)
        {
            // FIX: setear el tipo correcto antes del trigger
            _animator.SetInteger(HurtType,    (int)HurtAnimationType.Grabbed);
            _animator.SetTrigger(TriggerHurt);
        }
    }
    // ─────────────────────────────────────────────
    //  GANCHO — llamado desde PlayerHook (pendiente)
    // ─────────────────────────────────────────────
    /// <summary>Activar animación de hookshot al lanzar el gancho.</summary>
    public void OnHookshotStart()
    {
        _animator.SetBool(IsHookActive,  true);
        bool isRunning = _movement.IsGrounded
                        && Mathf.Abs(_rb.linearVelocity.x) > runningHookThreshold;
        _animator.SetBool(IsRunningHook, isRunning);
    }
    /// <summary>Gancho conectado — el jugador se columpia.</summary>
    public void OnSwingStart()
    {
        _animator.SetBool(IsSwinging, true);
    }
    /// <summary>Gancho soltado — volver a animaciones normales.</summary>
    public void OnHookshotEnd()
    {
        _animator.SetBool(IsHookActive,  false);
        _animator.SetBool(IsSwinging,    false);
        _animator.SetBool(IsRunningHook, false);
    }

    // ─────────────────────────────────────────────
    //  WALL HANG — llamado desde PlayerHook o PlayerMovement
    // ─────────────────────────────────────────────
    public void OnWallHangStart() => _animator.SetBool(IsWallHang, true);
    public void OnWallHangEnd()   => _animator.SetBool(IsWallHang, false);

    // ─────────────────────────────────────────────
    //  TAUNT — llamado desde PlayerMovement (input especial)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Solo se puede hacer taunt parado en suelo (sin velocidad).
    /// </summary>
    public void OnTaunt()
    {
        if (_movement.IsGrounded && Mathf.Abs(_rb.linearVelocity.x) < 0.5f)
            _animator.SetTrigger(TriggerTaunt);
    }

}
