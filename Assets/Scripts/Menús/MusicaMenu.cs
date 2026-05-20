using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicaMenu : MonoBehaviour {
    [Header("Música del menú")]
    [SerializeField] private AudioClip musica;
    [SerializeField, Range(0f, 1f)] private float volumen = 0.5f;
    [SerializeField] private bool enBucle = true;

    private AudioSource fuente;

    void Awake() {
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = enBucle;
        fuente.volume = volumen;
        if (musica != null) fuente.clip = musica;
    }

    void Start() {
        if (fuente.clip != null) fuente.Play();
    }
}
