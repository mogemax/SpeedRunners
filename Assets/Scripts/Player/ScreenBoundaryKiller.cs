using UnityEngine;

/// <summary>
/// Detecta cuándo un jugador sale de los límites de la cámara
/// y le aplica muerte instantánea (TakeOutOfScreenDeath).
///
/// La cámara le notifica los bounds actuales cada frame (UpdateBounds).
/// Este script solo comprueba la posición de los jugadores contra esos bounds.
///
/// Setup:
///   - En el mismo GameObject que SpeedRunnersCamera
///   - SpeedRunnersCamera arrastra esta referencia en el Inspector
/// </summary>
public class ScreenBoundaryKiller : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN
    // ─────────────────────────────────────────────
    [Header("Márgenes de gracia")]
    [Tooltip("Píxeles de margen extra FUERA del borde antes de matar " +
             "(evita muertes al borde justo del frame)")]
    public float gracePaddingX = 0.5f;
    public float gracePaddingY = 1f;

    [Tooltip("Segundos que el jugador puede estar fuera antes de morir " +
             "(0 = muerte inmediata)")]
    public float outOfBoundsGraceTime = 0.1f;

    // ─────────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────────
    private Bounds          _cameraBounds;
    private RaceManager     _race;

    // Temporizador por jugador (índice 0 y 1)
    private float[] _outOfBoundsTimer;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Start()
    {
        _race = RaceManager.Instance;
        if (_race == null)
        {
            Debug.LogError("[ScreenBoundaryKiller] RaceManager no encontrado.");
            return;
        }

        _outOfBoundsTimer = new float[_race.RaceData.Count];
    }

    // ─────────────────────────────────────────────
    //  API — llamado por SpeedRunnersCamera cada LateUpdate
    // ─────────────────────────────────────────────
    public void UpdateBounds(Bounds newBounds)
    {
        _cameraBounds = newBounds;
    }

    // ─────────────────────────────────────────────
    //  UPDATE — comproba jugadores contra bounds
    // ─────────────────────────────────────────────
    private void LateUpdate()
    {
        if (_race == null || _race.RaceData == null) return;

        // Bounds con padding de gracia (más generoso)
        Bounds killBounds = new Bounds(
            _cameraBounds.center,
            _cameraBounds.size + new Vector3(gracePaddingX * 2f, gracePaddingY * 2f, 0f)
        );

        for (int i = 0; i < _race.RaceData.Count; i++)
        {
            var data = _race.RaceData[i];
            if (data.IsEliminated || data.Health.IsDead) continue;

            bool isOutside = !killBounds.Contains(data.Transform.position);

            if (isOutside)
            {
                _outOfBoundsTimer[i] += Time.deltaTime;

                if (_outOfBoundsTimer[i] >= outOfBoundsGraceTime)
                {
                    _outOfBoundsTimer[i] = 0f;
                    data.Health.TakeOutOfScreenDeath();
                    Debug.Log($"[ScreenBoundaryKiller] {data.Transform.name} eliminado por salir de pantalla.");
                }
            }
            else
            {
                // Reset del temporizador si volvió dentro
                _outOfBoundsTimer[i] = 0f;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  DEBUG GIZMO — muestra la zona de muerte
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireCube(
            _cameraBounds.center,
            _cameraBounds.size + new Vector3(gracePaddingX * 2f, gracePaddingY * 2f, 0f)
        );
    }
}
