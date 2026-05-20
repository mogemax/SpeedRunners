using UnityEngine;
using UnityEngine.UI; // Necesario para modificar componentes de UI como 'Image'

public class AlternadorImagenes : MonoBehaviour {
    [Header("Referencias de UI")]
    [Tooltip("Arrastra aquí el objeto Image de tu Canvas donde aparecerán las imágenes")]
    [SerializeField] private Image imagenDestino;

    [Header("Las Dos Imágenes")]
    [SerializeField] private Sprite imagenOpcion1;
    [SerializeField] private Sprite imagenOpcion2;

    // Usamos un booleano (verdadero/falso) para saber cuál está mostrándose
    private bool mostrandoPrimera = true;

    void Start() {
        // Al iniciar el juego, nos aseguramos de que se muestre la opción 1 por defecto
        if (imagenDestino != null && imagenOpcion1 != null) {
            imagenDestino.sprite = imagenOpcion1;
        }
    }

    // Esta es la función que asignaremos a TUS DOS BOTONES de flecha
    public void CambiarImagen() {
        if (imagenDestino == null) return;

        // Invertimos el valor (si era true pasa a false, y viceversa)
        mostrandoPrimera = !mostrandoPrimera;

        // Cambiamos el sprite dependiendo del estado actual
        if (mostrandoPrimera) {
            imagenDestino.sprite = imagenOpcion1;
        } else {
            imagenDestino.sprite = imagenOpcion2;
        }
    }
}