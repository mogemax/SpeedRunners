using UnityEngine;

public class GeneradorJugadores : MonoBehaviour {
    [Header("Puntos de Aparición (Spawn Points)")]
    [Tooltip("Arrastra aquí un objeto vacío que indique dónde empieza el Jugador 1")]
    [SerializeField] private Transform puntoSpawnJugador1;
    [Tooltip("Arrastra aquí un objeto vacío que indique dónde empieza el Jugador 2")]
    [SerializeField] private Transform puntoSpawnJugador2;

    [Header("Prefabs por Defecto (Para Pruebas)")]
    [Tooltip("Si le das Play a esta escena directamente sin pasar por el menú, se usarán estos prefabs para evitar errores.")]
    [SerializeField] private GameObject prefabDefectoP1;
    [SerializeField] private GameObject prefabDefectoP2;

    void Start() {
        GameObject jugador1 = SpawnearJugador(0, DatosSeleccion.prefabJugador1, prefabDefectoP1, puntoSpawnJugador1);
        GameObject jugador2 = SpawnearJugador(1, DatosSeleccion.prefabJugador2, prefabDefectoP2, puntoSpawnJugador2);

        RegistrarEnRaceManager(jugador1, jugador2);
    }

    private GameObject SpawnearJugador(int index, GameObject prefabSeleccion, GameObject prefabDefecto, Transform puntoSpawn) {
        GameObject prefabFinal = prefabSeleccion;

        if (prefabFinal == null) {
            Debug.LogWarning($"Menú saltado: Usando personaje por defecto para Jugador {index + 1}.");
            prefabFinal = prefabDefecto;
        }

        if (prefabFinal == null || puntoSpawn == null) {
            Debug.LogError($"Falta asignar el Spawn Point o el Prefab por defecto del Jugador {index + 1} en el Generador.");
            return null;
        }

        GameObject jugador = Instantiate(prefabFinal, puntoSpawn.position, puntoSpawn.rotation);
        RegistrarEnHUD(index, jugador);
        return jugador;
    }

    private void RegistrarEnRaceManager(GameObject jugador1, GameObject jugador2) {
        if (RaceManager.Instance == null) {
            Debug.LogError("No hay RaceManager en la escena.");
            return;
        }

        var p1 = jugador1 != null ? jugador1.GetComponent<PlayerHealth>() : null;
        var p2 = jugador2 != null ? jugador2.GetComponent<PlayerHealth>() : null;

        var jugadoresValidos = new System.Collections.Generic.List<PlayerHealth>();
        if (p1 != null) jugadoresValidos.Add(p1);
        if (p2 != null) jugadoresValidos.Add(p2);

        RaceManager.Instance.InitializeRace(jugadoresValidos.ToArray());
    }

    private void RegistrarEnHUD(int index, GameObject player) {
        var hud = FindFirstObjectByType<PlayerHUDController>();
        if (hud != null)
            hud.BindPlayer(index, player);
    }
}