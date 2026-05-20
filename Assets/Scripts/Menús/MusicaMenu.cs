using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicaMenu : MonoBehaviour {
    [Header("Música")]
    [Tooltip("Clip de intro que se reproduce una vez al inicio (opcional). Si está vacío, va directo a 'musica'.")]
    [SerializeField] private AudioClip musicaIntro;
    [Tooltip("Clip principal que se reproduce después de la intro. Se loopea si 'enBucle' está activo.")]
    [SerializeField] private AudioClip musica;
    [Tooltip("Clip que se reproduce cuando termina una ronda (reemplaza a la música actual).")]
    [SerializeField] private AudioClip musicaFinRonda;
    [SerializeField, Range(0f, 1f)] private float volumen = 0.5f;
    [SerializeField] private bool enBucle = true;
    [Tooltip("Segundos de espera antes de comenzar a reproducir cualquier audio, contados desde el inicio del countdown.")]
    [SerializeField, Min(0f)] private float retrasoInicio = 0f;

    private AudioSource fuente;
    private bool musicaPendiente = true;

    void Awake() {
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.volume = volumen;
    }

    void Start() {
        if (RaceManager.Instance != null) {
            RaceManager.Instance.OnRoundEnd += AlTerminarRonda;
            RaceManager.Instance.OnCountdownTick += AlTickCountdown;
        } else {
            // Escena sin RaceManager (menús): arranca de una.
            StartCoroutine(SecuenciaReproduccion());
        }
    }

    void OnDestroy() {
        if (RaceManager.Instance != null) {
            RaceManager.Instance.OnRoundEnd -= AlTerminarRonda;
            RaceManager.Instance.OnCountdownTick -= AlTickCountdown;
        }
    }

    private IEnumerator SecuenciaReproduccion() {
        if (retrasoInicio > 0f)
            yield return new WaitForSeconds(retrasoInicio);

        if (musicaIntro != null) {
            fuente.clip = musicaIntro;
            fuente.loop = false;
            fuente.Play();
            yield return new WaitForSeconds(musicaIntro.length);
        }

        if (musica != null) {
            fuente.clip = musica;
            fuente.loop = enBucle;
            fuente.volume = volumen;
            fuente.Play();
        }
    }

    private void AlTerminarRonda(int ganadorIndex) {
        musicaPendiente = true;
        ReproducirFinRonda();
    }

    private void AlTickCountdown(int tick) {
        if (!musicaPendiente) return;
        musicaPendiente = false;
        ReiniciarMusica();
    }

    public void ReproducirFinRonda() {
        if (musicaFinRonda == null) return;
        StopAllCoroutines();
        fuente.Stop();
        fuente.clip = musicaFinRonda;
        fuente.loop = false;
        fuente.volume = volumen;
        fuente.Play();
    }

    public void ReiniciarMusica() {
        StopAllCoroutines();
        fuente.Stop();
        StartCoroutine(SecuenciaReproduccion());
    }
}
