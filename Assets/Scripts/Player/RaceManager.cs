using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Gestiona el estado de la carrera:
///   - Registra los dos jugadores
///   - Lleva la cuenta de checkpoints por jugador
///   - Determina quién va en primer lugar
///   - Expone eventos para que la cámara y la UI reaccionen
///
/// Setup en escena:
///   - Un GameObject vacío con este script (singleton)
///   - Los Checkpoint triggers en el nivel tienen el script Checkpoint.cs
///     y se auto-registran aquí al despertar
/// </summary>
public class RaceManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────
    public static RaceManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN
    // ─────────────────────────────────────────────
    [Header("Jugadores")]
    [Tooltip("Arrastra aquí los dos GameObjects de jugador en el editor")]
    public PlayerHealth[] players = new PlayerHealth[2];

    [Header("Carrera")]
    [Tooltip("Número total de checkpoints en el nivel (se llena automáticamente)")]
    [SerializeField] private int totalCheckpoints;

    // ─────────────────────────────────────────────
    //  ESTADO DE CARRERA POR JUGADOR
    // ─────────────────────────────────────────────
    public class PlayerRaceData
    {
        public PlayerHealth Health;
        public Transform    Transform;
        public int          CheckpointIndex;   // último checkpoint alcanzado (0 = inicio)
        public bool         IsEliminated;
        public int          Place;             // 1 = primero, 2 = segundo
    }

    private PlayerRaceData[] _raceData;

    // ─────────────────────────────────────────────
    //  PROPIEDADES PÚBLICAS
    // ─────────────────────────────────────────────

    /// <summary>Jugador que va en primer lugar (Transform). Null si no hay datos.</summary>
    public Transform LeaderTransform =>
        _raceData?.OrderBy(d => d.Place).FirstOrDefault(d => !d.IsEliminated)?.Transform;

    /// <summary>Todos los datos de carrera, ordenados por puesto.</summary>
    public IReadOnlyList<PlayerRaceData> RaceData => _raceData;

    // ─────────────────────────────────────────────
    //  EVENTOS
    // ─────────────────────────────────────────────
    public event System.Action<PlayerRaceData>   OnLeaderChanged;
    public event System.Action<PlayerRaceData>   OnPlayerEliminated;
    public event System.Action<PlayerRaceData>   OnRaceFinished;      // llegó a la meta

    // ─────────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────────
    private PlayerRaceData _lastLeader;
    private List<Checkpoint> _checkpoints = new();

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake()
    {
        // Singleton simple — solo una instancia por escena
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitRaceData();
    }

    private void InitRaceData()
    {
        _raceData = new PlayerRaceData[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            _raceData[i] = new PlayerRaceData
            {
                Health          = players[i],
                Transform       = players[i].transform,
                CheckpointIndex = 0,
                IsEliminated    = false,
                Place           = i + 1
            };

            // Escuchar muerte del jugador
            int captured = i;  // capturar para el closure
            players[i].OnPlayerDied += () => HandlePlayerDied(captured);
        }
    }

    private void Start()
    {
        UpdatePlaces();
    }

    // ─────────────────────────────────────────────
    //  REGISTRO DE CHECKPOINTS
    //  Los Checkpoint.cs llaman esto en su Awake()
    // ─────────────────────────────────────────────
    public void RegisterCheckpoint(Checkpoint cp)
    {
        if (!_checkpoints.Contains(cp))
        {
            _checkpoints.Add(cp);
            totalCheckpoints = _checkpoints.Count;
        }
    }

    // ─────────────────────────────────────────────
    //  CHECKPOINT ALCANZADO
    //  Llamado por Checkpoint.cs cuando un jugador
    //  pasa por el trigger.
    // ─────────────────────────────────────────────
    public void NotifyCheckpointReached(PlayerHealth player, int checkpointIndex, bool isFinishLine)
    {
        PlayerRaceData data = GetDataFor(player);
        if (data == null || data.IsEliminated) return;

        // Solo avanzar hacia adelante — nunca retroceder
        if (checkpointIndex <= data.CheckpointIndex) return;

        data.CheckpointIndex = checkpointIndex;

        if (isFinishLine)
        {
            OnRaceFinished?.Invoke(data);
            Debug.Log($"[RaceManager] {data.Transform.name} cruzó la meta en puesto {data.Place}.");
        }

        UpdatePlaces();
    }

    // ─────────────────────────────────────────────
    //  ACTUALIZAR PUESTOS
    //  Criterio: más checkpoints = mejor puesto.
    //  En empate de checkpoints, gana el que tiene
    //  mayor posición X en el mundo.
    // ─────────────────────────────────────────────
    private void UpdatePlaces()
    {
        if (_raceData == null) return;

        var sorted = _raceData
            .Where(d => !d.IsEliminated)
            .OrderByDescending(d => d.CheckpointIndex)
            .ThenByDescending(d => d.Transform.position.x)
            .ToArray();

        for (int i = 0; i < sorted.Length; i++)
            sorted[i].Place = i + 1;

        // Puestos para eliminados al final
        int lastPlace = sorted.Length + 1;
        foreach (var d in _raceData.Where(d => d.IsEliminated))
            d.Place = lastPlace++;

        // Notificar cambio de líder
        var newLeader = sorted.FirstOrDefault();
        if (newLeader != null && newLeader != _lastLeader)
        {
            _lastLeader = newLeader;
            OnLeaderChanged?.Invoke(newLeader);
        }
    }

    // ─────────────────────────────────────────────
    //  MUERTE DE JUGADOR
    // ─────────────────────────────────────────────
    private void HandlePlayerDied(int playerIndex)
    {
        var data = _raceData[playerIndex];

        // En SpeedRunners, morir una vez no elimina al jugador
        // (pierde una vida y reaparece). Solo se elimina si se
        // queda sin vidas — PlayerHealth ya gestiona eso.
        // Aquí solo eliminamos si CurrentLives llegó a 0.
        if (data.Health.CurrentLives <= 0)
        {
            data.IsEliminated = true;
            OnPlayerEliminated?.Invoke(data);
            UpdatePlaces();
            Debug.Log($"[RaceManager] {data.Transform.name} eliminado de la carrera.");
        }
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────
    private PlayerRaceData GetDataFor(PlayerHealth player)
    {
        foreach (var d in _raceData)
            if (d.Health == player) return d;
        return null;
    }

    /// <summary>
    /// Devuelve los datos de carrera de un jugador por su Transform.
    /// Útil para la cámara y la UI.
    /// </summary>
    public PlayerRaceData GetDataFor(Transform t)
    {
        foreach (var d in _raceData)
            if (d.Transform == t) return d;
        return null;
    }
}
