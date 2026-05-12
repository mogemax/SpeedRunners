using UnityEngine;

/// <summary>
/// Mueve esta capa de fondo en funcion del desplazamiento de la camara,
/// creando efecto de parallax. Un mismo script cubre dos casos:
///
///   parallaxFactor = 0  →  capa completamente fija a la camara (skybox, fondo solido)
///   parallaxFactor = 0..1  →  parallax con profundidad variable
///   parallaxFactor = 1  →  capa fija en el mundo (sin parallax)
///
/// Setup en escena:
///   Agregar este componente al GameObject del sprite de fondo.
///   Asignar la camara principal en el Inspector o dejar vacia
///   para que use Camera.main automaticamente.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  CONFIGURACION
    // ─────────────────────────────────────────────
    [Header("Parallax")]
    [Tooltip("0 = fijo a la camara | 1 = fijo en el mundo")]
    [Range(0f, 1f)]
    public float parallaxFactorX = 0f;

    [Tooltip("Parallax vertical. Generalmente 0 para fondos horizontales.")]
    [Range(0f, 1f)]
    public float parallaxFactorY = 0f;

    [Header("Tiling")]
    [Tooltip("Repetir la textura horizontalmente cuando se aleja de la camara")]
    public bool enableTiling = false;

    [Tooltip("Ancho del sprite en unidades de mundo. " +
             "Coincide con el ancho del SpriteRenderer en escala 1:1.")]
    public float tileWidth = 20f;

    [Header("Referencia")]
    [Tooltip("Dejar vacio para usar Camera.main")]
    public Camera targetCamera;

    // ─────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────
    private Vector3 _lastCameraPos;
    private float   _startX;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        _lastCameraPos = targetCamera.transform.position;
        _startX        = transform.position.x;
    }

    // ─────────────────────────────────────────────
    //  LATE UPDATE — despues de que la camara se mueva
    // ─────────────────────────────────────────────
    private void LateUpdate()
    {
        Vector3 delta = targetCamera.transform.position - _lastCameraPos;

        // Aplicar fraccion del movimiento de la camara segun el factor
        // factor 0: se mueve igual que la camara → queda fija en pantalla
        // factor 1: no se mueve → fija en el mundo
        float moveX = delta.x * (1f - parallaxFactorX);
        float moveY = delta.y * (1f - parallaxFactorY);

        transform.position += new Vector3(moveX, moveY, 0f);

        _lastCameraPos = targetCamera.transform.position;

        if (enableTiling)
            HandleTiling();
    }

    // ─────────────────────────────────────────────
    //  TILING HORIZONTAL
    //  Cuando la capa se aleja mas de medio tileWidth
    //  de la posicion de la camara, se reposiciona
    //  para simular repeticion infinita.
    // ─────────────────────────────────────────────
    private void HandleTiling()
    {
        float distFromCamera = transform.position.x - targetCamera.transform.position.x;

        if (Mathf.Abs(distFromCamera) >= tileWidth)
        {
            float offset = Mathf.Sign(distFromCamera) * tileWidth;
            transform.position = new Vector3(
                transform.position.x - offset,
                transform.position.y,
                transform.position.z
            );
        }
    }

    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (!enableTiling) return;

        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.4f);
        Vector3 center = new Vector3(transform.position.x, transform.position.y, 0f);
        Gizmos.DrawWireCube(center, new Vector3(tileWidth, 10f, 0f));
    }
}
