using UnityEngine;
using System.Collections;

public partial class EfectoSpawner : MonoBehaviour {
    [Header("Configuración del Efecto")]
    [SerializeField] private GameObject prefabEfecto;
    [SerializeField] private float tiempoMinimo = 0.5f;
    [SerializeField] private float tiempoMaximo = 2.0f;

    [Header("Cantidad por Ráfaga")]
    [SerializeField] private int minimoEstrellas = 2;
    [SerializeField] private int maximoEstrellas = 5;

    [Header("Efecto de Titilado (Aparición escalonada)")]
    [SerializeField] private float delayMinimoEntreEstrellas = 0.05f;
    [SerializeField] private float delayMaximoEntreEstrellas = 0.2f;

    private RectTransform canvasRect;

    void Start() {
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine() {
        while (true) {
            yield return new WaitForSeconds(Random.Range(tiempoMinimo, tiempoMaximo));

            int cantidadDeEstrellas = Random.Range(minimoEstrellas, maximoEstrellas + 1);

            for (int i = 0; i < cantidadDeEstrellas; i++) {
                SpawnearEfecto();
                // Esto hace que no aparezcan todas exactamente al mismo milisegundo
                yield return new WaitForSeconds(Random.Range(delayMinimoEntreEstrellas, delayMaximoEntreEstrellas));
            }
        }
    }

    void SpawnearEfecto() {
        GameObject nuevaInstancia = Instantiate(prefabEfecto, canvasRect.transform);
        RectTransform instanciaRect = nuevaInstancia.GetComponent<RectTransform>();

        float widthLim = canvasRect.rect.width / 2f;
        float heightLim = canvasRect.rect.height / 2f;

        float randomX = Random.Range(-widthLim, widthLim);
        float randomY = Random.Range(-heightLim, heightLim);
        instanciaRect.anchoredPosition = new Vector2(randomX, randomY);

        float randomZRotation = Random.Range(0f, 360f);
        instanciaRect.localEulerAngles = new Vector3(0, 0, randomZRotation);
    }
}