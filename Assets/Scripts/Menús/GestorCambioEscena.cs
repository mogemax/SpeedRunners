using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GestorCambioEscena : MonoBehaviour {
    [Header("Referencias")]
    [Tooltip("Arrastra aquí todos los objetos que tengan un Animator y deban reproducir la animación de Salida")]
    [SerializeField] private Animator[] animadoresMenu;

    [Header("Configuración")]
    [SerializeField] private float tiempoDeEspera = 0.5f;
    [SerializeField] private string nombreDeLaSiguienteEscena;

    public void IniciarTransicion() {
        StartCoroutine(RutinaCambioEscenaAsync());
    }

    private IEnumerator RutinaCambioEscenaAsync() {
        // 1. Disparamos el trigger para la animación de Salida en todos los objetos
        foreach (Animator anim in animadoresMenu) {
            if (anim != null) {
                anim.SetTrigger("Salida");
            }
        }

        // 2. Empezamos a cargar la siguiente escena en SEGUNDO PLANO
        AsyncOperation operacionCarga = SceneManager.LoadSceneAsync(nombreDeLaSiguienteEscena);

        // 3. IMPORTANTE: Le decimos a Unity que NO cambie de escena todavía, aunque ya la haya cargado
        operacionCarga.allowSceneActivation = false;

        // 4. Esperamos a que termine la animación visualmente
        yield return new WaitForSeconds(tiempoDeEspera);

        // 5. ¡Pum! La animación terminó. Le damos permiso a Unity para mostrar la escena que ya cargó en secreto.
        operacionCarga.allowSceneActivation = true;
    }
}