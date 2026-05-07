using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay visual de la "death zone":
///   - Aparece cuando el segundo jugador está cerca del borde de la cámara
///   - Parpadea en rojo con intensidad proporcional al peligro
///   - El borde se reduce visualmente conforme la cámara avanza
///
/// Setup en Canvas (Screen Space - Camera u Overlay):
///   - Un Panel con Image (color rojo) como hijo de Canvas — arrastrarlo a "borderImage"
///   - RectTransform del Panel debe cubrir toda la pantalla
///   - Este script en el Canvas o en un GameObject dedicado
///
/// El efecto se logra con 4 imágenes de borde (top, bottom, left, right)
/// o con una sola imagen en modo "Filled" — aquí usamos la versión de 4 bordes
/// para control fino de cada lado.
/// </summary>
public class CameraDeathZone : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN — BORDES UI
    // ─────────────────────────────────────────────
    [Header("Imágenes de borde (UI)")]
    [Tooltip("Image que cubre el borde izquierdo de la pantalla")]
    public Image leftBorder;
    [Tooltip("Image que cubre el borde derecho de la pantalla")]
    public Image rightBorder;
    [Tooltip("Image que cubre el borde superior")]
    public Image topBorder;
    [Tooltip("Image que cubre el borde inferior")]
    public Image bottomBorder;

    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN — APARIENCIA
    // ─────────────────────────────────────────────
    [Header("Apariencia")]
    [Tooltip("Color del borde de peligro")]
    public Color dangerColor = new Color(1f, 0f, 0f, 0.85f);

    [Tooltip("Ancho máximo del borde en píxeles (peligro máximo)")]
    public float maxBorderWidth = 80f;

    [Tooltip("Ancho mínimo del borde cuando empieza a aparecer")]
    public float minBorderWidth = 0f;

    [Tooltip("Velocidad del parpadeo (Hz)")]
    public float blinkFrequency = 3f;

    [Tooltip("Velocidad de suavizado del ancho del borde")]
    public float borderSmoothSpeed = 4f;

    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN — UMBRAL DE PELIGRO
    // ─────────────────────────────────────────────
    [Header("Umbral de activación")]
    [Tooltip("El borde aparece cuando el segundo jugador está dentro de este " +
             "porcentaje del borde de la cámara (0 = en el borde, 1 = siempre activo)")]
    [Range(0f, 1f)]
    public float dangerZonePercent = 0.3f;

    // ─────────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────────
    private SpeedRunnersCamera _srCam;
    private RaceManager        _race;
    private float              _currentWidth;
    private float              _targetWidth;
    private float              _blinkTimer;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Start()
    {
        _srCam = SpeedRunnersCamera.Instance;
        _race  = RaceManager.Instance;

        if (_srCam == null) Debug.LogError("[CameraDeathZone] SpeedRunnersCamera no encontrada.");
        if (_race  == null) Debug.LogError("[CameraDeathZone] RaceManager no encontrado.");

        SetBorderWidth(0f, alpha: 0f);
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (_srCam == null || _race == null) return;

        float dangerLevel = CalculateDangerLevel();
        UpdateBorderWidth(dangerLevel);
        UpdateBlink(dangerLevel);
    }

    // ─────────────────────────────────────────────
    //  CALCULAR NIVEL DE PELIGRO (0-1)
    //  0 = sin peligro, 1 = jugador en el borde mismo
    // ─────────────────────────────────────────────
    private float CalculateDangerLevel()
    {
        // Buscar al segundo jugador (puesto 2)
        RaceManager.PlayerRaceData secondPlayer = null;
        foreach (var d in _race.RaceData)
        {
            if (!d.IsEliminated && d.Place == 2)
            {
                secondPlayer = d;
                break;
            }
        }

        if (secondPlayer == null) return 0f;

        Bounds camBounds = _srCam.CameraBounds;
        Vector2 playerPos = secondPlayer.Transform.position;

        // Distancia al borde más cercano (en cada eje)
        float distLeft   = playerPos.x - camBounds.min.x;
        float distRight  = camBounds.max.x - playerPos.x;
        float distBottom = playerPos.y - camBounds.min.y;
        float distTop    = camBounds.max.y - playerPos.y;

        float minDist = Mathf.Min(distLeft, distRight, distBottom, distTop);

        // Zona de peligro = porcentaje del half-size de la cámara
        float camHalfWidth  = camBounds.size.x * 0.5f;
        float dangerDistance = camHalfWidth * dangerZonePercent;

        if (minDist > dangerDistance) return 0f;

        // Normalizar: 0 = en el límite de la zona peligrosa, 1 = en el borde
        return 1f - Mathf.Clamp01(minDist / dangerDistance);
    }

    // ─────────────────────────────────────────────
    //  ACTUALIZAR ANCHO DEL BORDE
    // ─────────────────────────────────────────────
    private void UpdateBorderWidth(float dangerLevel)
    {
        _targetWidth = Mathf.Lerp(minBorderWidth, maxBorderWidth, dangerLevel);
        _currentWidth = Mathf.Lerp(_currentWidth, _targetWidth, borderSmoothSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  PARPADEO
    // ─────────────────────────────────────────────
    private void UpdateBlink(float dangerLevel)
    {
        if (dangerLevel < 0.01f)
        {
            SetBorderWidth(_currentWidth, alpha: 0f);
            return;
        }

        // Parpadeo: seno entre 0 y 1, frecuencia proporcional al peligro
        float freq  = blinkFrequency * (1f + dangerLevel);  // más rápido cuando más peligro
        _blinkTimer += Time.deltaTime * freq * Mathf.PI * 2f;
        float blink = (Mathf.Sin(_blinkTimer) + 1f) * 0.5f;  // 0-1

        // Alpha mínimo 0.2 para que no desaparezca del todo
        float alpha = Mathf.Lerp(0.2f, dangerColor.a, blink) * dangerLevel;

        SetBorderWidth(_currentWidth, alpha);
    }

    // ─────────────────────────────────────────────
    //  APLICAR A LAS IMÁGENES DE BORDE
    // ─────────────────────────────────────────────
    private void SetBorderWidth(float width, float alpha)
    {
        Color c = dangerColor;
        c.a = alpha;

        ApplyToBorder(leftBorder,   width, c, isHorizontal: true);
        ApplyToBorder(rightBorder,  width, c, isHorizontal: true);
        ApplyToBorder(topBorder,    width, c, isHorizontal: false);
        ApplyToBorder(bottomBorder, width, c, isHorizontal: false);
    }

    private void ApplyToBorder(Image img, float width, Color color, bool isHorizontal)
    {
        if (img == null) return;
        img.color = color;

        RectTransform rt = img.rectTransform;
        Vector2 size = rt.sizeDelta;

        if (isHorizontal)
            size.x = width;
        else
            size.y = width;

        rt.sizeDelta = size;
    }

    // ─────────────────────────────────────────────
    //  SETUP AUTOMÁTICO DE BORDES
    //  Llama esto desde el editor con un botón si
    //  quieres que el script cree los 4 paneles por ti.
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("Crear bordes automáticamente en Canvas")]
    private void CreateBordersInEditor()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("Pon este script dentro de un Canvas."); return; }

        leftBorder   = CreateBorderPanel(canvas.transform, "LeftBorder",
                           new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(80f, 0f));
        rightBorder  = CreateBorderPanel(canvas.transform, "RightBorder",
                           new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(80f, 0f));
        topBorder    = CreateBorderPanel(canvas.transform, "TopBorder",
                           new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 80f));
        bottomBorder = CreateBorderPanel(canvas.transform, "BottomBorder",
                           new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f));

        Debug.Log("[CameraDeathZone] Bordes creados. Ajusta las anchuras en el Inspector.");
    }

    private Image CreateBorderPanel(Transform parent, string name,
        Vector2 anchor, Vector2 pivot, Vector2 extraSize)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot     = pivot;

        // Tamaño: stretch en el eje largo, ancho fijo en el corto
        bool isVerticalStretch = extraSize.x == 0f;
        if (isVerticalStretch)
        {
            rt.anchorMin = new Vector2(anchor.x, 0f);
            rt.anchorMax = new Vector2(anchor.x, 1f);
            rt.sizeDelta = new Vector2(80f, 0f);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, anchor.y);
            rt.anchorMax = new Vector2(1f, anchor.y);
            rt.sizeDelta = new Vector2(0f, 80f);
        }

        rt.anchoredPosition = Vector2.zero;

        var img   = go.GetComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0f);  // empieza transparente
        return img;
    }
#endif
}
