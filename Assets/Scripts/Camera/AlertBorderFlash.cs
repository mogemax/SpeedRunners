using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AlertBorderFlash : MonoBehaviour
{
    public enum GrowthAxis { None, Vertical, Horizontal }

    [Header("Colores")]
    public Color colorA = Color.red;
    public Color colorB = Color.black;

    [Header("Animación de parpadeo")]
    [Tooltip("Hz — cuántas veces por segundo alterna entre los dos colores.")]
    public float frequency = 1f;

    [Tooltip("Si está apagado, vuelve al color original y no anima. " +
             "CameraShrinkOverTime lo enciende cuando arranca el achicamiento.")]
    public bool flashing = false;

    [Header("Engrosamiento hacia el centro")]
    [Tooltip("Eje en el que el borde crece hacia el centro de la pantalla. " +
             "Vertical = borde superior/inferior (crece en alto). " +
             "Horizontal = borde izquierdo/derecho (crece en ancho). " +
             "None = no crece.")]
    public GrowthAxis growthAxis = GrowthAxis.Vertical;

    [Tooltip("Multiplicador del grosor cuando el achicamiento llega a su máximo (t=1). " +
             "Ej: 4 = el borde será 4 veces más grueso al final.")]
    public float endThicknessMultiplier = 4f;

    private Image _img;
    private RectTransform _rect;
    private Color _originalColor;
    private Vector2 _originalSize;

    private void Awake()
    {
        _img = GetComponent<Image>();
        _rect = GetComponent<RectTransform>();
        _originalColor = _img.color;
        _originalSize = _rect.sizeDelta;
    }

    private void OnDisable()
    {
        if (_img != null) _img.color = _originalColor;
        if (_rect != null) _rect.sizeDelta = _originalSize;
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

    public void SetGrowth(float t)
    {
        if (growthAxis == GrowthAxis.None || _rect == null) return;

        Vector2 size = _originalSize;
        float mult = Mathf.Lerp(1f, endThicknessMultiplier, Mathf.Clamp01(t));

        if (growthAxis == GrowthAxis.Vertical) size.y = _originalSize.y * mult;
        else size.x = _originalSize.x * mult;

        _rect.sizeDelta = size;
    }

    public void ResetGrowth()
    {
        if (_rect != null) _rect.sizeDelta = _originalSize;
    }
}
