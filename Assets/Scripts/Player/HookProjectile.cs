using System.Collections;
using UnityEngine;

public class HookProjectile : MonoBehaviour {
    [Header("Trayectoria")]
    [Tooltip("Altura del arco bezier respecto al punto medio origen-destino")]
    public float arcHeight = 1.5f;

    [Header("Animación de ruptura")]
    [Tooltip("Sprites del HookBreak en orden de reproducción")]
    public Sprite[] breakSprites;
    [Tooltip("Frames por segundo de la animación de ruptura")]
    public float breakFrameRate = 20f;

    private PlayerGrapple _owner;
    private Vector2 _origin;
    private Vector2 _target;
    private float _speed;
    private bool _willConnect;
    private float _t;
    private bool _done;

    private SpriteRenderer _sr;

    private void Awake() {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void Launch(PlayerGrapple owner, Vector2 origin, Vector2 target, float speed, bool willConnect) {
        _owner = owner;
        _origin = origin;
        _target = target;
        _speed = speed;
        _willConnect = willConnect;
        _t = 0f;
        _done = false;
    }

    private void Update() {
        if (_done) return;

        float totalDist = Vector2.Distance(_origin, _target);
        if (totalDist < 0.01f) {
            OnArrived();
            return;
        }

        _t += (_speed / totalDist) * Time.deltaTime;
        _t = Mathf.Clamp01(_t);

        // Punto de control elevado para crear el arco parabólico
        Vector2 control = (_origin + _target) * 0.5f + Vector2.up * arcHeight;
        Vector2 pos = Bezier(_origin, control, _target, _t);
        transform.position = pos;

        // Rotar el sprite en la dirección de avance del arco
        if (_t < 0.98f) {
            float tNext = Mathf.Clamp01(_t + 0.04f);
            Vector2 posNext = Bezier(_origin, control, _target, tNext);
            Vector2 dir = posNext - pos;
            if (dir.sqrMagnitude > 0.0001f) {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        if (_t >= 1f) OnArrived();
    }

    private void OnArrived() {
        _done = true;
        if (_willConnect) {
            _owner.ConnectGrapple(_target);
            _owner.OnProjectileFinished();
            Destroy(gameObject);
        } else {
            StartCoroutine(PlayBreak());
        }
    }

    public void Cancel() {
        _done = true;
        Destroy(gameObject);
    }

    private IEnumerator PlayBreak() {
        if (_sr != null && breakSprites != null && breakSprites.Length > 0) {
            float interval = 1f / breakFrameRate;
            foreach (Sprite s in breakSprites) {
                if (_sr == null) break;
                _sr.sprite = s;
                yield return new WaitForSeconds(interval);
            }
        } else {
            yield return new WaitForSeconds(0.25f);
        }
        _owner.OnProjectileFinished();
        Destroy(gameObject);
    }

    private static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, float t) {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }
}
