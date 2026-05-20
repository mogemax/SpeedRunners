using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lector de input para un SEGUNDO jugador local que polea teclas directamente
/// desde Keyboard.current (Input System). Hereda de PlayerInputReader, así que
/// PlayerMovement (que tiene RequireComponent&lt;PlayerInputReader&gt;) lo acepta sin cambios.
///
/// Setup:
///   - Duplicar el GameObject del jugador
///   - En el duplicado QUITAR los componentes "Player Input" y "PlayerInputReader"
///   - Agregar este componente y configurar las teclas en el Inspector
///   - Asignar el segundo jugador en el array "players" de RaceManager
///
/// Pensado solo para testing local — no participa del Input System ni del Player Input.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerInputReaderKeyboardP2 : PlayerInputReader
{
    [Header("Teclas (Input System / Keyboard.current)")]
    public Key leftKey    = Key.LeftArrow;
    public Key rightKey   = Key.RightArrow;
    public Key jumpKey    = Key.UpArrow;
    public Key slideKey   = Key.DownArrow;
    public Key boostKey   = Key.RightShift;
    public Key useItemKey = Key.Numpad1;

    // Bloqueamos los callbacks del PlayerInput (SendMessages). Si el GameObject
    // tiene además un componente PlayerInput, su input — incluido el del
    // gamepad — no debe afectar a este jugador, que se mueve solo por las
    // teclas configuradas arriba. Sin estos overrides, presionar gamepad
    // disparaba JumpPressed en este lector y hacía saltar a P2 también.
    public override void OnMove(InputValue value)    { }
    public override void OnJump(InputValue value)    { }
    public override void OnSlide(InputValue value)   { }
    public override void OnBoost(InputValue value)   { }
    public override void OnUseItem(InputValue value) { }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Eje horizontal
        float move = 0f;
        if (kb[leftKey].isPressed)  move -= 1f;
        if (kb[rightKey].isPressed) move += 1f;
        MoveInput = move;

        // Salto: held + edge-press
        JumpHeld = kb[jumpKey].isPressed;
        if (kb[jumpKey].wasPressedThisFrame)
            JumpPressed = true;

        // Slide: edge-press
        if (kb[slideKey].wasPressedThisFrame)
            SlidePressed = true;

        // Boost: held
        BoostHeld = kb[boostKey].isPressed;

        // UseItem: edge-press
        if (kb[useItemKey].wasPressedThisFrame)
            UseItemPressed = true;
    }
}
