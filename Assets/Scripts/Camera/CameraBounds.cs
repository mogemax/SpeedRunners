using UnityEngine;

/// <summary>
/// Define el área rectangular que tiene cobertura de arte.
/// SpeedRunnersCamera la consulta para limitar zoom y posición,
/// evitando que el viewport se salga a zonas sin arte.
///
/// Colocar este componente en la escena, ajustar los cuatro valores
/// en el Inspector hasta que el Gizmo cubra exactamente el área con arte.
/// </summary>
public class CameraBounds : MonoBehaviour
{
    [Header("Área de arte")]
    public float minX = -50f;
    public float maxX =  50f;
    public float minY = -20f;
    public float maxY =  20f;

    public float Width  => maxX - minX;
    public float Height => maxY - minY;

    private void OnDrawGizmos()
    {
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size   = new Vector3(Width, Height, 0.1f);

        Gizmos.color = new Color(0.1f, 1f, 0.5f, 0.08f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(0.1f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(center, size);
    }
}
