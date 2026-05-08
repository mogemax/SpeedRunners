using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class SpeedRunnersCamera : MonoBehaviour
{
    [Header("Configuración")]
    public Vector2 offset = new Vector2(3f, 1f);
    public float smoothSpeed = 5f;
    public float zoomSpeed = 2f;
    
    [Header("Zoom")]
    public float minSize = 5f;
    public float maxSize = 10f;

    private Camera _cam;
    private Vector3 _targetPos;
    private float _targetZoom;

    private void Awake() => _cam = GetComponent<Camera>();

    private void LateUpdate()
    {
        if (RaceManager.Instance == null || RaceManager.Instance.LeaderTransform == null) return;

        HandlePosition();
        HandleZoom();
    }

    private void HandlePosition()
    {
        Transform leader = RaceManager.Instance.LeaderTransform;
        _targetPos = new Vector3(leader.position.x + offset.x, leader.position.y + offset.y, -10f);
        
        transform.position = Vector3.Lerp(transform.position, _targetPos, smoothSpeed * Time.deltaTime);
    }

    private void HandleZoom()
    {
        // Calculamos la distancia entre los dos jugadores para el zoom
        var players = RaceManager.Instance.RaceData.Where(d => !d.IsEliminated).ToList();
        float dist = 0;

        if (players.Count > 1)
        {
            dist = Vector3.Distance(players[0].Transform.position, players[1].Transform.position);
        }

        _targetZoom = Mathf.Clamp(minSize + (dist * 0.5f), minSize, maxSize);
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, zoomSpeed * Time.deltaTime);
    }
}