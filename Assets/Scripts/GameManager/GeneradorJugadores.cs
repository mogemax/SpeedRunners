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
        SpawnearJugador1();
        SpawnearJugador2();
    }

    private void SpawnearJugador1() {
        GameObject prefabFinal = DatosSeleccion.prefabJugador1;

        if (prefabFinal == null) {
            Debug.LogWarning("Menú saltado: Usando personaje por defecto para Jugador 1.");
            prefabFinal = prefabDefectoP1;
        }

        if (prefabFinal != null && puntoSpawnJugador1 != null) {
            GameObject jugador1 = Instantiate(prefabFinal, puntoSpawnJugador1.position, puntoSpawnJugador1.rotation);
            RegistrarEnHUD(0, jugador1);
        } else {
            Debug.LogError("Falta asignar el Spawn Point o el Prefab por defecto del Jugador 1 en el Generador.");
        }
    }

    private void SpawnearJugador2() {
        GameObject prefabFinal = DatosSeleccion.prefabJugador2;

        if (prefabFinal == null) {
            Debug.LogWarning("Menú saltado: Usando personaje por defecto para Jugador 2.");
            prefabFinal = prefabDefectoP2;
        }

        if (prefabFinal != null && puntoSpawnJugador2 != null) {
            GameObject jugador2 = Instantiate(prefabFinal, puntoSpawnJugador2.position, puntoSpawnJugador2.rotation);
            RegistrarEnHUD(1, jugador2);
        } else {
            Debug.LogError("Falta asignar el Spawn Point o el Prefab por defecto del Jugador 2 en el Generador.");
        }
    }

    private void RegistrarEnHUD(int index, GameObject player) {
        var hud = FindFirstObjectByType<PlayerHUDController>();
        if (hud != null)
            hud.BindPlayer(index, player);
    }
}