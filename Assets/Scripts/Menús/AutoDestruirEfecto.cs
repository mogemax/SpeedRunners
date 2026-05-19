using UnityEngine;

public class AutoDestruirEfecto : MonoBehaviour {
    private Animator animator;

    void Awake() {
        animator = GetComponent<Animator>();
    }

    void Start() {
        // Obtenemos la duración de la animación actual
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Destruye el objeto automáticamente cuando termine el tiempo de la animación
        Destroy(gameObject, stateInfo.length);
    }
}