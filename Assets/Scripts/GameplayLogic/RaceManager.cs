using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Jugadores")]
    public PlayerHealth[] players = new PlayerHealth[2];

    [Header("Configuración")]
    public int  totalCheckpoints = 10;
    public bool raceIsActive     = true;

    [Header("Victorias")]
    [Tooltip("Victorias necesarias para ganar la partida completa")]
    public int winsToWin = 3;

    [Header("Countdown entre rondas")]
    [Tooltip("Segundos de cuenta regresiva antes de reiniciar la ronda")]
    public int countdownSeconds = 3;

    // ─────────────────────────────────────────────
    //  ANIMACIÓN DE ELIMINACIÓN  ← NUEVO
    //  Arrastra aquí el GameObject que tiene
    //  EliminationUIAnimator. Si se deja vacío,
    //  el juego sigue funcionando sin animación.
    // ─────────────────────────────────────────────
    [Header("Animación de eliminación")]
    [Tooltip("GameObject con el componente EliminationUIAnimator. " +
             "Opcional: si está vacío, la secuencia de animación se omite.")]
    public EliminationUIAnimator eliminationAnimator;

    // ─────────────────────────────────────────────
    //  PROGRESO POR JUGADOR
    // ─────────────────────────────────────────────
    [Serializable]
    public class PlayerProgress
    {
        public PlayerHealth Health;
        public Transform    Transform;
        public int          CurrentCP    = 0;
        public int          Laps         = 0;
        public bool         IsEliminated = false;
        public int          Wins         = 0;

        // Referencia al ultimo checkpoint fisico que cruzo
        public Checkpoint LastCheckpoint = null;

        public float TotalScore => (Laps * 1000) + CurrentCP;
    }

    public List<PlayerProgress> RaceData = new List<PlayerProgress>();

    private Dictionary<PlayerHealth, PlayerProgress> _progressMap
        = new Dictionary<PlayerHealth, PlayerProgress>();

    private List<Checkpoint> _checkpoints = new List<Checkpoint>();

    // ─────────────────────────────────────────────
    //  EVENTOS
    // ─────────────────────────────────────────────
    public event Action<int> OnRoundEnd;
    public event Action<int> OnMatchEnd;
    public event Action<int> OnCountdownTick;
    public event Action      OnCountdownFinished;

    // ─────────────────────────────────────────────
    //  CAMARA
    // ─────────────────────────────────────────────
    public Transform LeaderTransform
    {
        get {
            var leader = RaceData.Where(d => !d.IsEliminated)
                                 .OrderByDescending(d => d.TotalScore)
                                 .FirstOrDefault();
            return leader != null ? leader.Transform : null;
        }
    }

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake() => Instance = this;

    private void Start()
    {
        foreach (var p in players)
        {
            if (p == null) continue;

            var data = new PlayerProgress { Health = p, Transform = p.transform };
            RaceData.Add(data);
            _progressMap[p] = data;

            PlayerHealth captured = p;
            p.OnDied += () => HandleElimination(captured);
        }
    }

    // ─────────────────────────────────────────────
    //  CHECKPOINTS
    //  Sin cambios respecto al original
    // ─────────────────────────────────────────────
    public void RegisterCheckpoint(Checkpoint cp)
    {
        if (!_checkpoints.Contains(cp))
            _checkpoints.Add(cp);

        if (cp.checkpointIndex > totalCheckpoints)
            totalCheckpoints = cp.checkpointIndex;
    }

    public void NotifyCheckpointReached(PlayerHealth p, int idx, bool isMeta)
    {
        if (!raceIsActive) return;

        if (!_progressMap.TryGetValue(p, out var data)) return;

        var checkpoint = _checkpoints.FirstOrDefault(c => c.checkpointIndex == idx);
        if (checkpoint != null)
            data.LastCheckpoint = checkpoint;

        if (isMeta && data.CurrentCP >= totalCheckpoints - 1)
        {
            data.Laps++;
            data.CurrentCP = 0;
            Debug.Log($"[RaceManager] {p.gameObject.name} completo la vuelta {data.Laps}.");
        }
        else if (idx > data.CurrentCP)
        {
            data.CurrentCP = idx;
        }
    }

    // ─────────────────────────────────────────────
    //  ELIMINACION
    //  Solo se agrega la llamada al animador antes
    //  de iniciar RoundEndSequence. El resto es igual.
    // ─────────────────────────────────────────────
    private void HandleElimination(PlayerHealth eliminated)
    {
        if (!raceIsActive) return;

        if (_progressMap.TryGetValue(eliminated, out var eliminatedData))
            eliminatedData.IsEliminated = true;

        var winner = RaceData.FirstOrDefault(d => !d.IsEliminated);

        if (winner == null)
        {
            raceIsActive = false;
            Debug.Log("[RaceManager] Empate — ambos eliminados simultaneamente.");
            StartCoroutine(EliminationThenRoundEnd(winnerData: null));
            return;
        }

        winner.Wins++;
        int winnerIndex = RaceData.IndexOf(winner);

        Debug.Log($"[RaceManager] Ronda terminada — Ganador: {winner.Health.gameObject.name} " +
                  $"| Victorias: {winner.Wins}/{winsToWin}");

        OnRoundEnd?.Invoke(winnerIndex);
        raceIsActive = false;

        if (winner.Wins >= winsToWin)
        {
            Debug.Log($"[RaceManager] {winner.Health.gameObject.name} gana la partida! " +
                      $"({winner.Wins} victorias)");
            OnMatchEnd?.Invoke(winnerIndex);

            // ← NUEVO: reproducir animación incluso en victoria final,
            //   luego pausar el juego
            StartCoroutine(EliminationThenMatchEnd());
        }
        else
        {
            StartCoroutine(EliminationThenRoundEnd(winner));
        }
    }

    // ─────────────────────────────────────────────
    //  NUEVO: espera la animación, luego continúa
    // ─────────────────────────────────────────────

    /// <summary>
    /// Reproduce la secuencia de animación de eliminación y,
    /// al terminar, inicia RoundEndSequence normalmente.
    /// </summary>
    private IEnumerator EliminationThenRoundEnd(PlayerProgress winnerData)
    {
        yield return StartCoroutine(WaitForEliminationAnimation());
        yield return StartCoroutine(RoundEndSequence(winnerData));
    }

    /// <summary>
    /// Reproduce la secuencia de animación y luego pausa el juego
    /// (victoria final de partida).
    /// </summary>
    private IEnumerator EliminationThenMatchEnd()
    {
        yield return StartCoroutine(WaitForEliminationAnimation());
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Si hay un EliminationUIAnimator asignado, lo reproduce y
    /// espera a que termine. Si no hay, continúa inmediatamente.
    /// </summary>
    private IEnumerator WaitForEliminationAnimation()
    {
        if (eliminationAnimator == null)
            yield break;

        bool animationDone = false;

        // Suscribirse una sola vez al evento de fin de secuencia
        void OnDone() => animationDone = true;
        eliminationAnimator.OnSequenceComplete += OnDone;

        eliminationAnimator.PlayEliminationSequence();

        // Esperar hasta que la animación termine
        yield return new WaitUntil(() => animationDone);

        eliminationAnimator.OnSequenceComplete -= OnDone;
    }

    // ─────────────────────────────────────────────
    //  SECUENCIA FIN DE RONDA
    //  Sin cambios respecto al original
    // ─────────────────────────────────────────────
    private IEnumerator RoundEndSequence(PlayerProgress winnerData)
    {
        // 1 — Congelar jugadores
        FreezeAllPlayers(true);

        yield return new WaitForSeconds(0.5f);

        // 2 — Posicion de spawn
        Vector3 spawnPos = GetSpawnPosition(winnerData);

        // 3 — Teleportar y revivir a todos
        foreach (var data in RaceData)
        {
            data.Transform.position = spawnPos;
            data.IsEliminated       = false;
            data.Health.gameObject.SetActive(true);
            data.Health.Revive();
            data.CurrentCP      = 0;
            data.Laps           = 0;
            data.LastCheckpoint = null;
        }

        // 4 — Countdown
        for (int i = countdownSeconds; i > 0; i--)
        {
            Debug.Log($"[RaceManager] Nueva ronda en... {i}");
            OnCountdownTick?.Invoke(i);
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("[RaceManager] YA! — Ronda iniciada.");
        OnCountdownFinished?.Invoke();

        // 5 — Descongelar y reactivar
        FreezeAllPlayers(false);
        raceIsActive = true;
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    //  Sin cambios respecto al original
    // ─────────────────────────────────────────────
    private void FreezeAllPlayers(bool frozen)
    {
        foreach (var data in RaceData)
        {
            var movement = data.Health.GetComponent<PlayerMovement>();
            if (movement != null)
                movement.SetFrozen(frozen);
        }
    }

    private Vector3 GetSpawnPosition(PlayerProgress winnerData)
    {
        if (winnerData != null && winnerData.LastCheckpoint != null)
            return winnerData.LastCheckpoint.SpawnPosition;

        var first = _checkpoints.OrderBy(c => c.checkpointIndex).FirstOrDefault();
        if (first != null)
        {
            Debug.LogWarning("[RaceManager] El ganador no cruzo ningun checkpoint — " +
                             "spawneando en el checkpoint inicial.");
            return first.SpawnPosition;
        }

        Debug.LogWarning("[RaceManager] No hay checkpoints registrados — spawneando en (0,0,0).");
        return Vector3.zero;
    }
}
