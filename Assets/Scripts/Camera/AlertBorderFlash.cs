using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AlertBorderFlash : MonoBehaviour
{
    [Header("Colores")]
    public Color colorA = Color.red;
    public Color colorB = Color.black;

    [Header("Animación")]
    [Tooltip("Hz — cuántas veces por segundo alterna entre los dos colores.")]
    public float frequency = 1f;

    [Tooltip("Si está apagado, vuelve al color original y no anima. " +
             "CameraShrinkOverTime lo enciende cuando arranca el achicamiento.")]
    public bool flashing = false;

    private Image _img;
    private Color _originalColor;

    private void Awake()
    {
        _img = GetComponent<Image>();
        _originalColor = _img.color;
    }

    private void OnDisable()
    {
        if (_img != null) _img.color = _originalColor;
    }

    private void Update()
    {
        if (!flashing) return;

        float t = (Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) + 1f) * 0.5f;
        _img.color = Color.Lerp(colorA, colorB, t);
    }

    public void StartFlashing()
    {
        flashing = true;
    }

    public void StopFlashing()
    {
        flashing = false;
        if (_img != null) _img.color = _originalColor;
    }
}
