using UnityEngine;

/// <summary>
/// Checkpoint de carrera. Se registra en el RaceManager al iniciar
/// y notifica cuando un jugador lo cruza.
///
/// El SpawnPoint es el punto donde reaparecerán los jugadores
/// al inicio de una nueva ronda. Si no se asigna, se usa la
/// posición del propio checkpoint.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Configuración")]
    public int  checkpointIndex = 1;
    public bool isFinishLine    = false;

    [Header("Spawn")]
    [Tooltip("Posición de respawn para este checkpoint. " +
             "Si está vacío se usa transform.position de este objeto.")]
    public Transform spawnPoint;

    /// <summary>
    /// Posición donde deben aparecer los jugadores al reiniciar
    /// la ronda desde este checkpoint.
    /// </summary>
    public Vector3 SpawnPosition => spawnPoint != null
        ? spawnPoint.position
        : transform.position;

    private void Start()
    {
        if (RaceManager.Instance != null)
            RaceManager.Instance.RegisterCheckpoint(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();

        if (health != null && RaceManager.Instance != null)
        {
            RaceManager.Instance.NotifyCheckpointReached(health, checkpointIndex, isFinishLine);
            Debug.Log($"[Checkpoint] {other.name} pasó por el punto {checkpointIndex}");
        }
    }

    private void OnDrawGizmos()
    {
        // Checkpoint: cyan normal, meta: amarillo
        Gizmos.color = isFinishLine ? Color.yellow : Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 4f, 0f));

        // SpawnPoint: esfera verde, para ver dónde aparecerán los jugadores
        Gizmos.color = Color.green;
        Vector3 sp = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.DrawWireSphere(sp, 0.3f);
    }
}
