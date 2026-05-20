using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Lector de input por GAMEPAD (control) para un jugador local.
/// Lee directamente desde Gamepad.current — no usa Player Input ni .inputactions.
/// Hereda de PlayerInputReader, así que PlayerMovement lo acepta sin cambios.
///
/// Setup:
///   - En el prefab del jugador QUITAR los componentes "Player Input" y
///     "PlayerInputReader" (o "PlayerInputReaderKeyboardP2") que tuviera antes.
///   - Agregar este componente y, si quieres, cambiar los botones en el Inspector.
///   - Conectar un control compatible con XInput / DualShock / DualSense.
///
/// Mapeo por defecto (gamepad estilo Xbox):
///   - Mover:    Stick izquierdo (eje X) + D-Pad izq/der como respaldo
///   - Saltar:   Botón South (A en Xbox / X en PS)
///   - Slide:    Botón East  (B en Xbox / O en PS)
///   - Boost:    RightShoulder (RB / R1)
///   - UseItem:  Botón West  (X en Xbox / Cuadrado en PS)
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerInputReaderGamepad : PlayerInputReader
{
    [Header("Botones del Gamepad (Input System)")]
    [Tooltip("Si está activo, también se lee el stick izquierdo para el movimiento horizontal.")]
    public bool useLeftStick = true;

    [Tooltip("Zona muerta del stick izquierdo para evitar drift.")]
    [Range(0f, 0.9f)]
    public float stickDeadzone = 0.2f;

    public GamepadButton leftButton    = GamepadButton.DpadLeft;
    public GamepadButton rightButton   = GamepadButton.DpadRight;
    public GamepadButton jumpButton    = GamepadButton.South;        // A / X
    public GamepadButton slideButton   = GamepadButton.East;         // B / O
    public GamepadButton boostButton   = GamepadButton.RightShoulder;// RB / R1
    public GamepadButton useItemButton = GamepadButton.West;         // X / Cuadrado
    // Hookshot: Right Trigger (leído como ButtonControl, activo al superar el umbral)

    private void Update()
    {
        var gp = Gamepad.current;
        if (gp == null) return;

        // ── Eje horizontal: D-Pad + (opcional) stick izquierdo ──
        float move = 0f;
        if (gp[leftButton].isPressed)  move -= 1f;
        if (gp[rightButton].isPressed) move += 1f;

        if (useLeftStick)
        {
            float stickX = gp.leftStick.x.ReadValue();
            if (Mathf.Abs(stickX) > stickDeadzone)
                move = Mathf.Clamp(move + stickX, -1f, 1f);
        }
        MoveInput = move;

        // ── Salto: held + edge-press ──
        JumpHeld = gp[jumpButton].isPressed;
        if (gp[jumpButton].wasPressedThisFrame)
            JumpPressed = true;

        // ── Slide: held + edge-press ──
        SlideHeld = gp[slideButton].isPressed;
        if (gp[slideButton].wasPressedThisFrame)
            SlidePressed = true;

        // ── Boost: held ──
        BoostHeld = gp[boostButton].isPressed;

        // ── UseItem: edge-press ──
        if (gp[useItemButton].wasPressedThisFrame)
            UseItemPressed = true;

        // ── Hookshot: Right Trigger ──
        HookshotHeld = gp.rightTrigger.isPressed;
        if (gp.rightTrigger.wasPressedThisFrame)
            HookshotPressed = true;
    }
}
