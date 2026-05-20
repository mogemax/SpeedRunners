using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lee input de teclado via polling directo (Keyboard.current).
/// Soporta dos layouts simultáneamente: WASD y flechas.
/// Hereda de MonoBehaviour — PlayerMovement lo acepta sin cambios.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour {
    [Header("Layout A — WASD")]
    public Key leftKeyA     = Key.A;
    public Key rightKeyA    = Key.D;
    public Key jumpKeyA     = Key.Space;
    public Key slideKeyA    = Key.LeftShift;
    public Key hookshotKeyA = Key.X;
    public Key useItemKeyA  = Key.E;
    public Key boostKeyA    = Key.Z;

    [Header("Layout B — Flechas / Numpad")]
    public Key leftKeyB     = Key.LeftArrow;
    public Key rightKeyB    = Key.RightArrow;
    public Key jumpKeyB     = Key.UpArrow;
    public Key slideKeyB    = Key.RightShift;
    public Key hookshotKeyB = Key.Numpad0;
    public Key useItemKeyB  = Key.Numpad1;
    public Key boostKeyB    = Key.Numpad2;

    // ── Estado público ────────────────────────────────────────────────────
    public float MoveInput       { get; protected set; }
    public bool  JumpHeld        { get; protected set; }
    public bool  JumpPressed     { get; protected set; }
    public bool  SlideHeld       { get; protected set; }
    public bool  SlidePressed    { get; protected set; }
    public bool  BoostHeld       { get; protected set; }
    public bool  UseItemPressed  { get; protected set; }
    public bool  HookshotHeld    { get; protected set; }
    public bool  HookshotPressed { get; protected set; }

    private void Update() {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Movimiento: ambos layouts, el que esté presionado gana
        float move = 0f;
        if (kb[leftKeyA].isPressed  || kb[leftKeyB].isPressed)  move -= 1f;
        if (kb[rightKeyA].isPressed || kb[rightKeyB].isPressed) move += 1f;
        MoveInput = move;

        // Salto
        JumpHeld = kb[jumpKeyA].isPressed || kb[jumpKeyB].isPressed;
        if (kb[jumpKeyA].wasPressedThisFrame || kb[jumpKeyB].wasPressedThisFrame)
            JumpPressed = true;

        // Slide
        SlideHeld = kb[slideKeyA].isPressed || kb[slideKeyB].isPressed;
        if (kb[slideKeyA].wasPressedThisFrame || kb[slideKeyB].wasPressedThisFrame)
            SlidePressed = true;

        // Hookshot
        HookshotHeld = kb[hookshotKeyA].isPressed || kb[hookshotKeyB].isPressed;
        if (kb[hookshotKeyA].wasPressedThisFrame || kb[hookshotKeyB].wasPressedThisFrame)
            HookshotPressed = true;

        // UseItem
        if (kb[useItemKeyA].wasPressedThisFrame || kb[useItemKeyB].wasPressedThisFrame)
            UseItemPressed = true;

        // Boost
        BoostHeld = kb[boostKeyA].isPressed || kb[boostKeyB].isPressed;
    }

    private void LateUpdate() {
        JumpPressed     = false;
        SlidePressed    = false;
        HookshotPressed = false;
        UseItemPressed  = false;
    }

    public void ResetInput() {
        MoveInput       = 0f;
        JumpPressed     = false;
        JumpHeld        = false;
        SlideHeld       = false;
        SlidePressed    = false;
        BoostHeld       = false;
        UseItemPressed  = false;
        HookshotHeld    = false;
        HookshotPressed = false;
    }
}
