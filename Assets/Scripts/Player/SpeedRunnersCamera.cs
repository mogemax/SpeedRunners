using UnityEngine;

/// <summary>
/// Cámara dinámica estilo SpeedRunners:
///   - Sigue al jugador en primer lugar (según RaceManager)
///   - Se aleja (zoom out) cuando los jugadores están muy separados
///   - Tiene un zoom mínimo para que el borde siempre presione al segundo
///   - Suavizado independiente para posición y zoom
///   - Soporta secciones verticales (la cámara sube y baja con saltos)
///
/// Requiere:
///   - Camera con este script en el mismo GameObject
///   - RaceManager en la escena
///   - ScreenBoundaryKiller (se comunica con él vía referencia directa)
/// </summary>
[RequireComponent(typeof(Camera))]
public class SpeedRunnersCamera : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  SINGLETON (la cámara es única)
    // ─────────────────────────────────────────────
    public static SpeedRunnersCamera Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN — POSICIÓN
    // ─────────────────────────────────────────────
    [Header("Posición")]
    [Tooltip("Offset fijo respecto al líder (útil para ver más hacia adelante)")]
    public Vector2 offset = new Vector2(3f, 1f);

    [Tooltip("Velocidad de suavizado de posición (mayor = más rápido)")]
    public float positionSmoothSpeed = 5f;

    [Tooltip("Límite vertical máximo hacia arriba desde el líder")]
    public float maxVerticalOffset = 4f;

    [Tooltip("Límite vertical máximo hacia abajo desde el líder")]
    public float minVerticalOffset = -2f;

    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN — ZOOM
    // ─────────────────────────────────────────────
    [Header("Zoom")]
    [Tooltip("Tamaño ortográfico base (zoom normal)")]
    public float baseOrthographicSize = 7f;

    [Tooltip("Zoom mínimo permitido (más cerca)")]
    public float minOrthographicSize = 5f;

    [Tooltip("Zoom máximo permitido (más lejos, cuando los jugadores están muy separados)")]
    public float maxOrthographicSize = 12f;

    [Tooltip("Qué porcentaje de la distancia entre jugadores afecta al zoom (0-1)")]
    [Range(0f, 1f)]
    public float zoomDistanceFactor = 0.25f;

    [Tooltip("Velocidad de suavizado del zoom")]
    public float zoomSmoothSpeed = 3f;

    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN — PROFUNDIDAD Z
    // ─────────────────────────────────────────────
    [Header("Z")]
    [Tooltip("Posición Z fija de la cámara (negativo para ver la escena 2D)")]
    public float cameraZ = -10f;

    // ─────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────
    [Header("Referencias")]
    public ScreenBoundaryKiller boundaryKiller;

    // ─────────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────────
    private Camera      _cam;
    private RaceManager _race;
    private Vector3     _targetPosition;
    private float       _targetSize;

    // ─────────────────────────────────────────────
    //  PROPIEDADES PÚBLICAS (leídas por ScreenBoundaryKiller)
    // ─────────────────────────────────────────────

    /// <summary>Bounds del frustum de la cámara en world space (2D).</summary>
    public Bounds CameraBounds
    {
        get
        {
            float h = _cam.orthographicSize;
            float w = h * _cam.aspect;
            return new Bounds(
                (Vector2)transform.position,
                new Vector2(w * 2f, h * 2f)
            );
        }
    }

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _cam         = GetComponent<Camera>();
        _targetSize  = baseOrthographicSize;
        _cam.orthographicSize = baseOrthographicSize;
    }

    private void Start()
    {
        _race = RaceManager.Instance;
        if (_race == null)
            Debug.LogError("[SpeedRunnersCamera] RaceManager no encontrado en escena.");
    }

    // ─────────────────────────────────────────────
    //  LATE UPDATE — siempre después de que los
    //  jugadores se hayan movido en Update/FixedUpdate
    // ─────────────────────────────────────────────
    private void LateUpdate()
    {
        if (_race == null) return;

        Transform leader = _race.LeaderTransform;
        if (leader == null) return;

        CalculateTargetPosition(leader);
        CalculateTargetZoom();
        ApplySmoothCamera();
    }

    // ─────────────────────────────────────────────
    //  CÁLCULO DE POSICIÓN OBJETIVO
    // ─────────────────────────────────────────────
    private void CalculateTargetPosition(Transform leader)
    {
        // Dirección del flip del líder para el offset horizontal
        var leaderMovement = leader.GetComponent<PlayerMovement>();
        float facingDir    = (leaderMovement != null && !leaderMovement.IsFacingRight) ? -1f : 1f;

        float targetX = leader.position.x + offset.x * facingDir;

        // Vertical: seguir al líder pero con límites suaves
        float verticalDelta = Mathf.Clamp(
            leader.position.y - transform.position.y,
            minVerticalOffset,
            maxVerticalOffset
        );
        float targetY = transform.position.y + verticalDelta + offset.y;

        _targetPosition = new Vector3(targetX, targetY, cameraZ);
    }

    // ─────────────────────────────────────────────
    //  CÁLCULO DE ZOOM OBJETIVO
    //  Se aleja cuando los jugadores están lejos
    //  entre sí para que el segundo siempre sea
    //  visible (o al menos presionado por el borde).
    // ─────────────────────────────────────────────
    private void CalculateTargetZoom()
    {
        var data = _race.RaceData;
        if (data == null || data.Count < 2)
        {
            _targetSize = baseOrthographicSize;
            return;
        }

        // Distancia entre los dos jugadores
        float dist = 0f;
        if (data[0] != null && data[1] != null && !data[0].IsEliminated && !data[1].IsEliminated)
        {
            dist = Vector2.Distance(
                data[0].Transform.position,
                data[1].Transform.position
            );
        }

        float desiredSize = baseOrthographicSize + dist * zoomDistanceFactor;
        _targetSize = Mathf.Clamp(desiredSize, minOrthographicSize, maxOrthographicSize);
    }

    // ─────────────────────────────────────────────
    //  APLICAR SUAVIZADO
    // ─────────────────────────────────────────────
    private void ApplySmoothCamera()
    {
        transform.position = Vector3.Lerp(
            transform.position, _targetPosition,
            positionSmoothSpeed * Time.deltaTime
        );

        _cam.orthographicSize = Mathf.Lerp(
            _cam.orthographicSize, _targetSize,
            zoomSmoothSpeed * Time.deltaTime
        );

        // Notificar al boundary killer que los bounds cambiaron
        boundaryKiller?.UpdateBounds(CameraBounds);
    }

    // ─────────────────────────────────────────────
    //  DEBUG GIZMO
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        Gizmos.color = Color.yellow;
        float h = _cam != null ? _cam.orthographicSize : baseOrthographicSize;
        float w = h * (_cam != null ? _cam.aspect : 1.78f);
        Gizmos.DrawWireCube(transform.position, new Vector3(w * 2f, h * 2f, 0f));
    }
}
