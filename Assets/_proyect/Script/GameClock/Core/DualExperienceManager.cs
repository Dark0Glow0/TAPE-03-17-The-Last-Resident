using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Tape0317.Core
{
    public class DualExperienceManager : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] XROrigin xrOrigin;
        [SerializeField] GameClock clock;
        [SerializeField] NightDirector director;
        [SerializeField] ScreenFader fader;

        [Header("Mundos (grupos de objetos)")]
        [Tooltip("Todo lo que solo existe en el presente: muebles de Héctor, luces, la TV...")]
        [SerializeField] GameObject presentWorld;
        [Tooltip("Todo lo que solo existe en la cinta: el círculo ritual, la cámara VHS, el Volume VHS...")]
        [SerializeField] GameObject pastWorld;

        [Header("Puntos de aparición")]
        [Tooltip("Dónde aparece el jugador al entrar a la cinta. La flecha azul (Z) indica hacia dónde mira.")]
        [SerializeField] Transform pastSpawn;
        [Tooltip("Dónde aparece al volver al presente (ej. frente a la TV).")]
        [SerializeField] Transform presentReturnPoint;

        [Tooltip("Segundos en negro entre los dos mundos (deja respirar el cambio).")]
        [SerializeField, Min(0f)] float blackHoldSeconds = 1f;

        [Header("Eventos")]
        [SerializeField] UnityEvent onEnterTape;
        [SerializeField] UnityEvent onReturnToPresent;

        public bool InTape { get; private set; }
        bool busy;
        XRInteractionManager interactionManager;

        void Reset()
        {
            xrOrigin = FindFirstObjectByType<XROrigin>();
            clock = FindFirstObjectByType<GameClock>();
            director = FindFirstObjectByType<NightDirector>();
            fader = FindFirstObjectByType<ScreenFader>();
        }

        void Awake()
        {
            interactionManager = FindFirstObjectByType<XRInteractionManager>();

            if (presentWorld != null) presentWorld.SetActive(true);
            if (pastWorld != null) pastWorld.SetActive(false);
        }

        // ---------- API pública (conectar desde VHSPlayer / PlayerTrigger) ----------

        public void EnterTape()
        {
            if (busy || InTape)
                return;

            if (director != null && director.CurrentPhase != NightPhase.Calm)
                return;

            StartCoroutine(EnterTapeRoutine());
        }

        public void ReturnToPresent()
        {
            if (busy || !InTape)
                return;

            StartCoroutine(ReturnRoutine());
        }

        // ---------- Secuencias ----------

        IEnumerator EnterTapeRoutine()
        {
            busy = true;

            if (fader != null)
                yield return fader.FadeOut();

            if (clock != null)
                clock.Pause();

            ReleaseHeldObjects(presentWorld);
            SwapWorlds(toPast: true);
            Teleport(pastSpawn);
            InTape = true;
            onEnterTape?.Invoke();

            yield return new WaitForSeconds(blackHoldSeconds);

            if (fader != null)
                yield return fader.FadeIn();

            busy = false;
        }

        IEnumerator ReturnRoutine()
        {
            busy = true;

            if (fader != null)
                yield return fader.FadeOut();

            ReleaseHeldObjects(pastWorld);
            SwapWorlds(toPast: false);
            Teleport(presentReturnPoint);
            InTape = false;
            onReturnToPresent?.Invoke();

            yield return new WaitForSeconds(blackHoldSeconds);

            if (fader != null)
                yield return fader.FadeIn();

            if (clock != null)
                clock.Resume();
            if (director != null)
                director.NotifyTapeWatched();

            busy = false;
        }

        
        void ReleaseHeldObjects(GameObject outgoingWorld)
        {
            if (xrOrigin == null)
            {
                Debug.LogWarning("[Dual] No hay XR Origin asignado: no puedo soltar lo que tiene el jugador.");
                return;
            }

           
            if (interactionManager == null)
                interactionManager = FindFirstObjectByType<XRInteractionManager>();

           
            var held = new List<Transform>();
            var interactors = xrOrigin.GetComponentsInChildren<IXRSelectInteractor>();
            foreach (IXRSelectInteractor interactor in interactors)
                foreach (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable selected in interactor.interactablesSelected)
                    held.Add(selected.transform);

           
            foreach (IXRSelectInteractor interactor in interactors)
            {
                if (!interactor.hasSelection)
                    continue;

                XRInteractionManager manager = interactionManager;
                if (manager == null && interactor is XRBaseInteractor baseInteractor)
                    manager = baseInteractor.interactionManager;

                if (manager != null)
                    manager.CancelInteractorSelection(interactor);
                else
                    Debug.LogWarning($"[Dual] No encontré XR Interaction Manager para soltar desde {interactor.transform.name}.");
            }

           
            if (outgoingWorld == null)
                return;

            foreach (Transform t in held)
            {
                if (t != null && t.parent == null)
                    t.SetParent(outgoingWorld.transform, worldPositionStays: true);
            }
        }

        void SwapWorlds(bool toPast)
        {
            if (presentWorld != null) presentWorld.SetActive(!toPast);
            if (pastWorld != null) pastWorld.SetActive(toPast);
        }

       
        void Teleport(Transform target)
        {
            if (target == null || xrOrigin == null)
                return;

           
            var controller = xrOrigin.GetComponent<CharacterController>();
            bool hadController = controller != null && controller.enabled;
            if (hadController)
                controller.enabled = false;

            xrOrigin.MatchOriginUpCameraForward(target.up, target.forward);
            Vector3 headPosition = target.position + target.up * xrOrigin.CameraInOriginSpaceHeight;
            xrOrigin.MoveCameraToWorldLocation(headPosition);

            Physics.SyncTransforms();

            if (hadController)
                controller.enabled = true;
        }

        // ---------- Depuración ----------

        [ContextMenu("Debug/Entrar a la cinta")]
        void DebugEnter() => EnterTape();

        [ContextMenu("Debug/Volver al presente")]
        void DebugReturn() => ReturnToPresent();
    }
}
