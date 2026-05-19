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
        // Tomamos el prefab almacenado de forma estática
        GameObject prefabFinal = DatosSeleccion.prefabJugador1;

        // Protección por si pruebas la escena suelta en Unity
        if (prefabFinal == null) {
            Debug.LogWarning("Menú saltado: Usando personaje por defecto para Jugador 1.");
            prefabFinal = prefabDefectoP1;
        }

        // Si tenemos el prefab y el lugar de aparición, lo creamos
        if (prefabFinal != null && puntoSpawnJugador1 != null) {
            GameObject jugador1 = Instantiate(prefabFinal, puntoSpawnJugador1.position, puntoSpawnJugador1.rotation);

            // TIP EXTRA: Si tus prefabs necesitan saber qué número de jugador son para los controles, puedes decírselo aquí:
            // jugador1.GetComponent<ControladorMovimiento>().AsignarControles(1);
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

            // jugador2.GetComponent<ControladorMovimiento>().AsignarControles(2);
        } else {
            Debug.LogError("Falta asignar el Spawn Point o el Prefab por defecto del Jugador 2 en el Generador.");
        }
    }
}