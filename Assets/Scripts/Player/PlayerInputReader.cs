using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Captura el estado del Input System y lo expone como propiedades de solo lectura.
/// No toma ninguna decision de gameplay — solo lee y almacena.
/// Cualquier script del jugador (PlayerMovement, PlayerHook, etc.) lee de aqui.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputReader : MonoBehaviour
{
    public float MoveInput    { get; private set; }
    public bool  JumpHeld     { get; private set; }
    public bool  JumpPressed  { get; private set; }
    public bool  SlidePressed { get; private set; }

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

    // ─────────────────────────────────────────────
    //  RESET — llamado por PlayerMovement al congelar
    // ─────────────────────────────────────────────

    public void ResetInput()
    {
        MoveInput    = 0f;
        JumpPressed  = false;
        JumpHeld     = false;
        SlidePressed = false;
    }

    // ─────────────────────────────────────────────
    //  CONSUMO DE FLAGS DE UN SOLO FRAME
    //  JumpPressed y SlidePressed se limpian en LateUpdate
    //  despues de que PlayerMovement los lee en Update.
    // ─────────────────────────────────────────────
    private void LateUpdate()
    {
        JumpPressed  = false;
        SlidePressed = false;
    }
}
