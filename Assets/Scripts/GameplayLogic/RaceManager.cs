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
    //  EXPLOSIÓN  ← NUEVO
    //  Arrastra aquí el prefab que tiene
    //  ExplosionEffect + Animator + SpriteRenderer.
    //  Se instanciará en la posición del jugador eliminado.
    // ─────────────────────────────────────────────
    [Header("Explosión")]
    [Tooltip("GameObject en la escena (dentro de un Canvas) con ExplosionEffect e Image.")]
    public ExplosionEffect explosionEffect;

    // ─────────────────────────────────────────────
    //  ANIMACIÓN DE ELIMINACIÓN (UI)
    //  Se mantiene por si lo usas para las otras
    //  animaciones de UI que mencionaste.
    // ─────────────────────────────────────────────
    [Header("Animación de eliminación (UI)")]
    [Tooltip("Opcional: GameObject con EliminationUIAnimator para animaciones de UI " +
             "que ocurren DESPUÉS de la explosión.")]
    public EliminationUIAnimator eliminationAnimator;

    [Header("Animación de victoria")]
    [Tooltip("GameObject en la escena con WinnerDisplayController. Se activa después de la explosión.")]
    public WinnerDisplayController winnerDisplay;

    [Header("Cuenta regresiva")]
    [Tooltip("GameObject en la escena con CountdownDisplayController. Se muestra antes de iniciar la ronda.")]
    public CountdownDisplayController countdownDisplay;

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

        public Checkpoint LastCheckpoint = null;

        public float TotalScore => (Laps * 1000) + CurrentCP;
    }

    public List<PlayerProgress> RaceData = new List<PlayerProgress>();

    private Dictionary<PlayerHealth, PlayerProgress> _progressMap
        = new Dictionary<PlayerHealth, PlayerProgress>();

    private List<Checkpoint> _checkpoints = new List<Checkpoint>();

    private Vector3 _lastEliminationPos = Vector3.zero;
    private int     _lastWinnerIndex    = -1;

    // ─────────────────────────────────────────────
    //  EVENTOS
    // ─────────────────────────────────────────────
    public event Action<int> OnRoundEnd;
    public event Action<int> OnMatchEnd;
    public event Action<int> OnCountdownTick;
    public event Action      OnCountdownFinished;

    // ─────────────────────────────────────────────
    //  CÁMARA
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
            Debug.Log($"[RaceManager] {p.gameObject.name} completó la vuelta {data.Laps}.");
        }
        else if (idx > data.CurrentCP)
        {
            data.CurrentCP = idx;
        }
    }

    // ─────────────────────────────────────────────
    //  ELIMINACIÓN
    // ─────────────────────────────────────────────
    private void HandleElimination(PlayerHealth eliminated)
    {
        if (!raceIsActive) return;

        // Guardar posición ANTES de que el GameObject se desactive
        if (_progressMap.TryGetValue(eliminated, out var eliminatedData))
        {
            _lastEliminationPos      = eliminatedData.Transform.position;
            eliminatedData.IsEliminated = true;
        }

        var winner = RaceData.FirstOrDefault(d => !d.IsEliminated);

        if (winner == null)
        {
            raceIsActive = false;
            Debug.Log("[RaceManager] Empate — ambos eliminados simultáneamente.");
            StartCoroutine(EliminationThenRoundEnd(winnerData: null));
            return;
        }

        winner.Wins++;
        int winnerIndex  = RaceData.IndexOf(winner);
        _lastWinnerIndex = winnerIndex;

        Debug.Log($"[RaceManager] Ronda terminada — Ganador: {winner.Health.gameObject.name} " +
                  $"| Victorias: {winner.Wins}/{winsToWin}");

        OnRoundEnd?.Invoke(winnerIndex);
        raceIsActive = false;

        if (winner.Wins >= winsToWin)
        {
            Debug.Log($"[RaceManager] {winner.Health.gameObject.name} gana la partida! " +
                      $"({winner.Wins} victorias)");
            OnMatchEnd?.Invoke(winnerIndex);
            StartCoroutine(EliminationThenMatchEnd());
        }
        else
        {
            StartCoroutine(EliminationThenRoundEnd(winner));
        }
    }

    // ─────────────────────────────────────────────
    //  CORRUTINAS DE SECUENCIA
    // ─────────────────────────────────────────────

    private IEnumerator EliminationThenRoundEnd(PlayerProgress winnerData)
    {
        yield return StartCoroutine(WaitForEliminationAnimation());

        if (eliminationAnimator != null)
            yield return StartCoroutine(WaitForUIAnimation());

        yield return StartCoroutine(WaitForWinnerAnimation(_lastWinnerIndex));

        yield return StartCoroutine(RoundEndSequence(winnerData));
    }

    private IEnumerator EliminationThenMatchEnd()
    {
        yield return StartCoroutine(WaitForEliminationAnimation());

        if (eliminationAnimator != null)
            yield return StartCoroutine(WaitForUIAnimation());

        yield return StartCoroutine(WaitForWinnerAnimation(_lastWinnerIndex));

        // Victoria final: pausar el juego (el winnerDisplay queda visible)
        Time.timeScale = 0f;
    }

    // ─────────────────────────────────────────────
    //  ANIMACIÓN DE VICTORIA
    // ─────────────────────────────────────────────
    private IEnumerator WaitForWinnerAnimation(int winnerIndex)
    {
        if (winnerDisplay == null || winnerIndex < 0)
            yield break;

        bool done = false;
        void OnDone() => done = true;
        winnerDisplay.OnAnimationComplete += OnDone;

        winnerDisplay.Show(winnerIndex);

        yield return new WaitUntil(() => done);

        winnerDisplay.OnAnimationComplete -= OnDone;
        // El display NO se oculta aquí — se mantiene visible durante el countdown
    }

    // ─────────────────────────────────────────────
    //  EXPLOSIÓN EN WORLD SPACE
    //  Instancia el prefab en la posición del jugador
    //  eliminado y espera a que ExplosionEffect termine.
    //  El slow motion lo maneja el propio ExplosionEffect.
    // ─────────────────────────────────────────────
    private IEnumerator WaitForEliminationAnimation()
    {
        if (explosionEffect == null)
        {
            Debug.LogWarning("[RaceManager] No hay explosionEffect asignado. Saltando animación.");
            yield break;
        }

        bool explosionDone = false;

        void OnDone() => explosionDone = true;
        explosionEffect.OnExplosionComplete += OnDone;

        explosionEffect.Play(_lastEliminationPos);

        yield return new WaitUntil(() => explosionDone);

        explosionEffect.OnExplosionComplete -= OnDone;
    }

    // ─────────────────────────────────────────────
    //  ANIMACIÓN UI (EliminationUIAnimator)
    //  Sin cambios — se mantiene para las otras
    //  dos animaciones que mencionaste.
    // ─────────────────────────────────────────────
    private IEnumerator WaitForUIAnimation()
    {
        bool animationDone = false;

        void OnDone() => animationDone = true;
        eliminationAnimator.OnSequenceComplete += OnDone;

        eliminationAnimator.PlayEliminationSequence();

        yield return new WaitUntil(() => animationDone);

        eliminationAnimator.OnSequenceComplete -= OnDone;
    }

    // ─────────────────────────────────────────────
    //  SECUENCIA FIN DE RONDA
    // ─────────────────────────────────────────────
    private IEnumerator RoundEndSequence(PlayerProgress winnerData)
    {
        FreezeAllPlayers(true);

        yield return new WaitForSeconds(0.5f);

        Vector3 spawnPos = GetSpawnPosition(winnerData);

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

        if (winnerDisplay != null)
            winnerDisplay.Hide();

        if (countdownDisplay != null && countdownDisplay.frames != null && countdownDisplay.frames.Length > 0)
        {
            void TickRelay(int idx) => OnCountdownTick?.Invoke(idx);
            countdownDisplay.OnTick += TickRelay;
            yield return countdownDisplay.PlayRoutine();
            countdownDisplay.OnTick -= TickRelay;
        }
        else
        {
            for (int i = countdownSeconds; i > 0; i--)
            {
                Debug.Log($"[RaceManager] Nueva ronda en... {i}");
                OnCountdownTick?.Invoke(i);
                yield return new WaitForSeconds(1f);
            }
        }

        Debug.Log("[RaceManager] ¡YA! — Ronda iniciada.");
        OnCountdownFinished?.Invoke();

        FreezeAllPlayers(false);
        raceIsActive = true;
    }

    // ─────────────────────────────────────────────
    //  HELPERS
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
            Debug.LogWarning("[RaceManager] El ganador no cruzó ningún checkpoint — " +
                             "spawneando en el checkpoint inicial.");
            return first.SpawnPosition;
        }

        Debug.LogWarning("[RaceManager] No hay checkpoints registrados — spawneando en (0,0,0).");
        return Vector3.zero;
    }
}
