using System.Collections;
using UnityEngine;

/// <summary>
/// Reproduce un array de sprites en loop sobre un SpriteRenderer.
/// Usado para animaciones idle (ej. el cubo del BreakableCrate).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LoopedSpriteAnimation : MonoBehaviour
{
    public Sprite[] frames;
    public float frameRate = 12f;

    private SpriteRenderer _sr;

    private void Awake() => _sr = GetComponent<SpriteRenderer>();

    private void OnEnable()
    {
        if (frames != null && frames.Length > 0)
            StartCoroutine(Play());
    }

    private void OnDisable() => StopAllCoroutines();

    private IEnumerator Play()
    {
        float interval = 1f / Mathf.Max(frameRate, 0.01f);
        int i = 0;

        while (true)
        {
            _sr.sprite = frames[i];
            i = (i + 1) % frames.Length;
            yield return new WaitForSeconds(interval);
        }
    }
}
