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

    [Header("Límites")]
    [Tooltip("Opcional. Define el área con arte. Si no se asigna, no hay clamping.")]
    public CameraBounds bounds;

    private Camera _cam;
    private Vector3 _targetPos;
    private float _targetZoom;

    private void Awake() => _cam = GetComponent<Camera>();

    private void LateUpdate()
    {
        if (RaceManager.Instance == null || RaceManager.Instance.LeaderTransform == null) return;

        // Zoom primero: HandlePosition usa el orthographicSize ya actualizado de este frame
        HandleZoom();
        HandlePosition();
    }

    private void HandlePosition()
    {
        Transform leader = RaceManager.Instance.LeaderTransform;
        _targetPos = new Vector3(leader.position.x + offset.x, leader.position.y + offset.y, -10f);

        if (bounds != null)
        {
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            _targetPos.x = Mathf.Clamp(_targetPos.x, bounds.minX + halfW, bounds.maxX - halfW);
            _targetPos.y = Mathf.Clamp(_targetPos.y, bounds.minY + halfH, bounds.maxY - halfH);
        }

        transform.position = Vector3.Lerp(transform.position, _targetPos, smoothSpeed * Time.deltaTime);
    }

    private void HandleZoom()
    {
        var players = RaceManager.Instance.RaceData.Where(d => !d.IsEliminated).ToList();
        float dist = 0;

        if (players.Count > 1)
            dist = Vector3.Distance(players[0].Transform.position, players[1].Transform.position);

        float effectiveMax = maxSize;
        if (bounds != null)
        {
            // Máximo zoom sin que el viewport salga de los bounds
            float maxFromHeight = bounds.Height * 0.5f;
            float maxFromWidth  = bounds.Width  * 0.5f / _cam.aspect;
            effectiveMax = Mathf.Min(maxSize, Mathf.Min(maxFromHeight, maxFromWidth));
        }

        _targetZoom = Mathf.Clamp(minSize + (dist * 0.5f), minSize, effectiveMax);
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, zoomSpeed * Time.deltaTime);
    }
}
