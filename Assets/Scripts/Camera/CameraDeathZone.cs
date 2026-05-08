using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CameraDeathZone : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image[] redBorders; // Arrastra aquí las imágenes de los bordes rojos

    [Header("Configuración")]
    public float warningThreshold = 0.8f; // A partir de qué % del borde se activa
    public float flashSpeed = 5f;

    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;
        // Ocultamos los bordes al inicio
        foreach (var border in redBorders) {
            if (border != null) border.canvasRenderer.SetAlpha(0);
        }
    }

    private void Update()
    {
        if (RaceManager.Instance == null || !RaceManager.Instance.raceIsActive) 
        {
            SetBordersAlpha(0);
            return;
        }

        // Buscamos al jugador que va de último (el que tiene menos score)
        var lastPlayer = RaceManager.Instance.RaceData
            .Where(d => !d.IsEliminated)
            .OrderBy(d => d.TotalScore)
            .FirstOrDefault();

        if (lastPlayer == null) return;

        // Convertimos su posición de mundo a posición de pantalla (0 a 1)
        Vector3 screenPos = _mainCam.WorldToViewportPoint(lastPlayer.Transform.position);

        // Si el jugador está cerca de los bordes (0.1 o 0.9 en viewport)
        if (screenPos.x < 0.15f || screenPos.x > 0.85f || screenPos.y < 0.15f || screenPos.y > 0.85f)
        {
            // Efecto de parpadeo suave
            float alpha = (Mathf.Sin(Time.time * flashSpeed) + 1f) / 2f;
            SetBordersAlpha(alpha * 0.6f); // 0.6 para que no sea rojo sólido
        }
        else
        {
            SetBordersAlpha(0);
        }
    }

    private void SetBordersAlpha(float alpha)
    {
        foreach (var border in redBorders) {
            if (border != null) border.canvasRenderer.SetAlpha(alpha);
        }
    }
}