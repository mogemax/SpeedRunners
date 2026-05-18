using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
/// <summary>
/// Maneja la secuencia de AnimationClips de UI que se reproduce
/// cuando un jugador es eliminado, antes del countdown entre rondas.
///
/// Setup en el GameObject:
///   - Animator (en este mismo GameObject o en un hijo)
///   - Este script
///
/// En el inspector asigna los AnimationClips en el orden en que
/// quieres que se reproduzcan. Se reproducirán todos en secuencia
/// y al terminar notificarán al RaceManager para continuar.
///
/// El Animator debe tener:
///   - Un estado por cada clip (o usar Override Controller si prefieres)
///   - Parámetro trigger: "PlayNext" para avanzar entre estados
///   - Alternativamente, este script usa PlayInFixedTime para mayor control
/// </summary>
public class EliminationUIAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  CONFIGURACIÓN — editable desde el Inspector
    // ─────────────────────────────────────────────
    [Header("Animaciones de eliminación")]
    [Tooltip("Lista de AnimationClips en el orden en que se reproducirán. " +
             "Puedes agregar o reordenar desde el Inspector.")]
    public List<AnimationClip> eliminationClips = new List<AnimationClip>();
 
    [Header("Referencias")]
    [Tooltip("El Animator que reproducirá los clips. " +
             "Si se deja vacío, se busca en este GameObject.")]
    public Animator targetAnimator;
 
    // ─────────────────────────────────────────────
    //  EVENTOS
    // ─────────────────────────────────────────────
 
    /// <summary>
    /// Se invoca cuando TODAS las animaciones de la secuencia terminaron.
    /// RaceManager escucha esto para continuar con el countdown.
    /// </summary>
    public event Action OnSequenceComplete;
 
    // ─────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────
    private AnimatorOverrideController _overrideController;
    private bool                        _isPlaying = false;
 
    // Nombre del estado/clip base en el Animator.
    // El Animator necesita UN solo estado con este nombre
    // que sirva como "slot" donde se inyectan los clips.
    private const string BASE_STATE_NAME = "EliminationSlot";
    private const string BASE_CLIP_NAME  = "EliminationSlot";
 
    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();
 
        if (targetAnimator == null)
        {
            Debug.LogError("[EliminationUIAnimator] No se encontró un Animator. " +
                           "Asigna uno en el Inspector o agrega el componente.");
            return;
        }
 
        // Crear un Override Controller sobre el RuntimeAnimatorController existente.
        // Esto permite reemplazar el clip base con cada clip de la lista
        // sin modificar el Animator Asset original.
        if (targetAnimator.runtimeAnimatorController != null)
        {
            _overrideController = new AnimatorOverrideController(
                targetAnimator.runtimeAnimatorController
            );
            targetAnimator.runtimeAnimatorController = _overrideController;
        }
        else
        {
            Debug.LogError("[EliminationUIAnimator] El Animator no tiene un " +
                           "RuntimeAnimatorController asignado.");
        }
 
        // El panel empieza oculto
        gameObject.SetActive(false);
    }
 
    // ─────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────
 
    /// <summary>
    /// Inicia la reproducción de todos los clips en secuencia.
    /// Llamado por RaceManager al detectar una eliminación.
    /// </summary>
    public void PlayEliminationSequence()
    {
        if (_isPlaying)
        {
            Debug.LogWarning("[EliminationUIAnimator] Ya se está reproduciendo una secuencia.");
            return;
        }
 
        if (eliminationClips == null || eliminationClips.Count == 0)
        {
            Debug.LogWarning("[EliminationUIAnimator] No hay clips asignados. " +
                             "Saltando secuencia de animación.");
            OnSequenceComplete?.Invoke();
            return;
        }
 
        gameObject.SetActive(true);
        StartCoroutine(PlaySequenceRoutine());
    }
 
    // ─────────────────────────────────────────────
    //  SECUENCIA
    // ─────────────────────────────────────────────
    private IEnumerator PlaySequenceRoutine()
    {
        _isPlaying = true;
 
        foreach (AnimationClip clip in eliminationClips)
        {
            if (clip == null)
            {
                Debug.LogWarning("[EliminationUIAnimator] Clip nulo en la lista, se omite.");
                continue;
            }
 
            yield return StartCoroutine(PlaySingleClip(clip));
        }
 
        _isPlaying = false;
        gameObject.SetActive(false);
 
        // Notificar al RaceManager que ya terminó toda la secuencia
        OnSequenceComplete?.Invoke();
    }
 
    private IEnumerator PlaySingleClip(AnimationClip clip)
    {
        if (_overrideController == null)
        {
            // Fallback: esperar la duración del clip sin animación
            Debug.LogWarning($"[EliminationUIAnimator] Sin Override Controller. " +
                             $"Esperando {clip.length}s por el clip '{clip.name}'.");
            yield return new WaitForSeconds(clip.length);
            yield break;
        }
 
        // Reemplazar el clip base con el clip actual
        _overrideController[BASE_CLIP_NAME] = clip;
 
        // Forzar al Animator a usar el nuevo clip desde el inicio
        targetAnimator.Play(BASE_STATE_NAME, 0, 0f);
 
        // Esperar un frame para que el Animator procese el cambio
        yield return null;
 
        // Esperar exactamente la duración del clip
        yield return new WaitForSeconds(clip.length);
    }
}