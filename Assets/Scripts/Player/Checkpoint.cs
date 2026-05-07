using UnityEngine;

/// <summary>
/// Coloca este script en un GameObject con Collider2D (trigger)
/// en cada checkpoint del nivel.
///
/// Setup:
///   - Collider2D marcado como "Is Trigger"
///   - checkpointIndex: número secuencial (1, 2, 3…)
///   - El último checkpoint marca isFinishLine = true
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Número de orden de este checkpoint (empieza en 1)")]
    public int checkpointIndex = 1;

    [Tooltip("¿Es la línea de meta?")]
    public bool isFinishLine = false;

    // ─────────────────────────────────────────────
    //  INIT — auto-registro en RaceManager
    // ─────────────────────────────────────────────
    private void Awake()
    {
        // El RaceManager puede no existir todavía en Awake,
        // así que usamos Start para registrarse.
    }

    private void Start()
    {
        if (RaceManager.Instance != null)
            RaceManager.Instance.RegisterCheckpoint(this);
        else
            Debug.LogWarning($"[Checkpoint] RaceManager no encontrado en escena. " +
                                $"Checkpoint {checkpointIndex} no registrado.");
    }

    // ─────────────────────────────────────────────
    //  TRIGGER
    // ─────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        RaceManager.Instance?.NotifyCheckpointReached(health, checkpointIndex, isFinishLine);

        // Visual debug — opcional, quitar en producción
        Debug.Log($"[Checkpoint {checkpointIndex}] alcanzado por {other.name}" +
                    (isFinishLine ? " — META" : ""));
    }

    // ─────────────────────────────────────────────
    //  GIZMO — visible en Scene View
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = isFinishLine ? Color.yellow : Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 3f, 0f));

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.8f,
            isFinishLine ? "META" : $"CP {checkpointIndex}"
        );
#endif
    }
}
