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
    [Tooltip("Segundos de espera antes de comenzar a reproducir cualquier audio.")]
    [SerializeField, Min(0f)] private float retrasoInicio = 0f;

    private AudioSource fuente;

    void Awake() {
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.volume = volumen;
    }

    void Start() {
        StartCoroutine(SecuenciaReproduccion());

        if (RaceManager.Instance != null)
            RaceManager.Instance.OnRoundEnd += AlTerminarRonda;
    }

    void OnDestroy() {
        if (RaceManager.Instance != null)
            RaceManager.Instance.OnRoundEnd -= AlTerminarRonda;
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
        ReproducirFinRonda();
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
}
