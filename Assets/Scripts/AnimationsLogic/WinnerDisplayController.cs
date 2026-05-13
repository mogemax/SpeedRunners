using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Setup en la escena:
//   Canvas (Screen Space - Overlay)
//     └─ WinnerDisplay  ← este GameObject
//          ├─ Image
//          └─ WinnerDisplayController
//
// En el Inspector asigna los sprites de cada jugador en "Players > Sprites".
// No necesita Animator ni Animator Controller.
[RequireComponent(typeof(Image))]
public class WinnerDisplayController : MonoBehaviour
{
    [Header("Sprites de victoria por jugador (en orden de índice)")]
    public WinnerPlayerData[] players;

    [Header("Velocidad de animación")]
    public float frameRate = 12f;

    public event Action OnAnimationComplete;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        gameObject.SetActive(false);
    }

    public void Show(int winnerIndex)
    {
        if (players == null || winnerIndex >= players.Length || players[winnerIndex] == null)
        {
            Debug.LogWarning("[WinnerDisplayController] Sin datos para jugador " + winnerIndex);
            OnAnimationComplete?.Invoke();
            return;
        }

        gameObject.SetActive(true);
        StartCoroutine(PlayAnimation(players[winnerIndex].sprites));
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator PlayAnimation(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("[WinnerDisplayController] Sin sprites asignados.");
            OnAnimationComplete?.Invoke();
            yield break;
        }

        float interval = 1f / frameRate;

        foreach (Sprite frame in sprites)
        {
            _image.sprite = frame;
            yield return new WaitForSeconds(interval);
        }

        OnAnimationComplete?.Invoke();
    }
}

[Serializable]
public class WinnerPlayerData
{
    public Sprite[] sprites;
}
