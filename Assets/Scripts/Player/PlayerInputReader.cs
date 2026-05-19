using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Captura el estado del Input System y lo expone como propiedades de solo lectura.
/// No toma ninguna decision de gameplay — solo lee y almacena.
/// Cualquier script del jugador (PlayerMovement, PlayerHook, etc.) lee de aqui.
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    public float MoveInput      { get; protected set; }
    public bool  JumpHeld       { get; protected set; }
    public bool  JumpPressed    { get; protected set; }
    public bool  SlidePressed   { get; protected set; }
    public bool  BoostHeld      { get; protected set; }
    public bool  UseItemPressed { get; protected set; }

    // ─────────────────────────────────────────────
    //  CALLBACKS DEL INPUT SYSTEM
    //  Unity los invoca via SendMessages desde PlayerInput
    // ─────────────────────────────────────────────

    public void OnMove(InputValue value)
        => MoveInput = value.Get<Vector2>().x;

    public void OnJump(InputValue value)
    {
        if (value.isPressed) { JumpPressed = true; JumpHeld = true; }
        else                   JumpHeld = false;
    }

    public void OnSlide(InputValue value)
    {
        if (value.isPressed) SlidePressed = true;
    }

    public void OnBoost(InputValue value)
    {
        BoostHeld = value.isPressed;
    }

    public void OnUseItem(InputValue value)
    {
        if (value.isPressed) UseItemPressed = true;
    }

    // ─────────────────────────────────────────────
    //  RESET — llamado por PlayerMovement al congelar
    // ─────────────────────────────────────────────

    public void ResetInput()
    {
        MoveInput      = 0f;
        JumpPressed    = false;
        JumpHeld       = false;
        SlidePressed   = false;
        BoostHeld      = false;
        UseItemPressed = false;
    }

    // ─────────────────────────────────────────────
    //  CONSUMO DE FLAGS DE UN SOLO FRAME
    //  JumpPressed y SlidePressed se limpian en LateUpdate
    //  despues de que PlayerMovement los lee en Update.
    // ─────────────────────────────────────────────
    private void LateUpdate()
    {
        JumpPressed    = false;
        SlidePressed   = false;
        UseItemPressed = false;
    }
}
