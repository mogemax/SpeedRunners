using UnityEngine;
 
/// <summary>
/// Elimina al jugador que lleve más de graceTime segundos
/// fuera del área visible de la cámara.
/// Usa WorldToViewportPoint para que los bordes coincidan
/// exactamente con lo que se ve en pantalla, sin importar
/// el zoom ni el offset de SpeedRunnersCamera.
/// </summary>
public class ScreenBoundaryKiller : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Segundos fuera de pantalla antes de morir")]
    public float graceTime = 0.3f;

    [Tooltip("Margen extra en los bordes (0 = borde exacto de cámara)")]
    [Range(0f, 0.1f)]
    public float borderPadding = 0.02f;

    private Camera _cam;
    private float[] _deathTimers = new float[2];

    private void Start() => _cam = GetComponent<Camera>();

    private void Update()
    {
        if (RaceManager.Instance == null || !RaceManager.Instance.raceIsActive) return;

        var players = RaceManager.Instance.RaceData;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].IsEliminated) continue;

            // Convertir posición del jugador a coordenadas de viewport:
            // (0,0) = esquina inferior izquierda de la cámara
            // (1,1) = esquina superior derecha de la cámara
            Vector3 vp = _cam.WorldToViewportPoint(players[i].Transform.position);

            bool outOfBounds = vp.x < -borderPadding || vp.x > 1f + borderPadding ||
                                vp.y < -borderPadding || vp.y > 1f + borderPadding;

            if (outOfBounds)
            {
                _deathTimers[i] += Time.deltaTime;

                if (_deathTimers[i] >= graceTime)
                    players[i].Health.Die();
            }
            else
            {
                // Volvió a entrar antes del límite — resetear timer
                _deathTimers[i] = 0f;
            }
        }
    }
}