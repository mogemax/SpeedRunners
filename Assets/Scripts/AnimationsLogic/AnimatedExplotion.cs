using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedExplosion : MonoBehaviour
{
    public Sprite[] frames;
    public float    frameRate = 18f;

    private void Start()
    {
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float interval = 1f / frameRate;

        foreach (Sprite frame in frames)
        {
            sr.sprite = frame;
            yield return new WaitForSecondsRealtime(interval);
        }

        Destroy(gameObject);
    }
}