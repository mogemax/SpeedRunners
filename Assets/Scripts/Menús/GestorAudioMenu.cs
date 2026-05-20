using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GestorAudioMenu : MonoBehaviour {
    public static GestorAudioMenu Instancia { get; private set; }

    [Header("SFX de botones")]
    [SerializeField] private AudioClip sonidoHover;
    [SerializeField] private AudioClip sonidoClickGenerico;
    [SerializeField] private AudioClip sonidoClickStart;
    [SerializeField] private AudioClip sonidoClickCancel;
    [SerializeField] private AudioClip sonidoClickDone;
    [SerializeField] private AudioClip sonidoTabSlideIn;

    [Header("Volumen")]
    [SerializeField, Range(0f, 1f)] private float volumenSfx = 1f;

    private AudioSource fuente;

    void Awake() {
        if (Instancia != null && Instancia != this) {
            Destroy(gameObject);
            return;
        }
        Instancia = this;

        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.volume = volumenSfx;
    }

    void OnDestroy() {
        if (Instancia == this) Instancia = null;
    }

    private void Reproducir(AudioClip clip) {
        if (clip == null || fuente == null) return;
        fuente.PlayOneShot(clip, volumenSfx);
    }

    public void ReproducirHover() => Reproducir(sonidoHover);
    public void ReproducirClick() => Reproducir(sonidoClickGenerico);
    public void ReproducirClickStart() => Reproducir(sonidoClickStart);
    public void ReproducirClickCancel() => Reproducir(sonidoClickCancel);
    public void ReproducirClickDone() => Reproducir(sonidoClickDone);
    public void ReproducirTabSlideIn() => Reproducir(sonidoTabSlideIn);
}
