using UnityEngine;
using System.Collections;

public class FallingCrate : MonoBehaviour
{
    [Header("Configuración de Penalización")]
    [Tooltip("Factor de velocidad (0.2 = 20% de la velocidad normal)")]
    public float speedFactor = 0.3f;
    public float penaltyDuration = 2.0f;

    [Header("Configuración de Respawn")]
    public float respawnTime = 5.0f;

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private SpriteRenderer _renderer;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private bool _isTriggered = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<SpriteRenderer>();

        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isTriggered) return;

        if (collision.gameObject.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            _isTriggered = true;
            player.ApplyCratePenalty(speedFactor, penaltyDuration);
            StartCoroutine(CrateLifeCycle());
        }
    }

    private IEnumerator CrateLifeCycle()
    {
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _collider.isTrigger = true;

        _rb.AddForce(new Vector2(2f, 3f), ForceMode2D.Impulse);
        _rb.AddTorque(-15f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(respawnTime * 0.4f);

        _renderer.enabled = false;

        yield return new WaitForSeconds(respawnTime * 0.6f);

        ResetCrate();
    }

    private void ResetCrate()
    {
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        _renderer.enabled = true;
        _collider.enabled = true;
        _collider.isTrigger = false;
        _isTriggered = false;
    }
}
