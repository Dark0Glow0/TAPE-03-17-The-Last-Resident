using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Tape0317.Core
{
    /// <summary>
    /// Cambia entre el PRESENTE (Héctor en la casa) y el PASADO (la cinta que se está "jugando").
    ///
    /// Versión MVP en UNA sola escena: el presente y el pasado son dos grupos de objetos
    /// (presentWorld / pastWorld) que se activan y desactivan. El jugador se teletransporta
    /// con un fundido a negro. Ventajas para la demo: no hay carga de escenas ni referencias
    /// entre escenas, y el XR Origin y los sistemas nunca se destruyen.
    /// (Para el juego completo con 7 cintas conviene pasar a escenas aditivas.)
    ///
    /// IMPORTANTE: el XR Origin, [Systems] y el ScreenFader NO pueden estar dentro de
    /// presentWorld ni de pastWorld, porque se desactivarían con ellos.
    /// </summary>
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

            // Estado inicial garantizado aunque en el editor quedaran los dos activos.
            if (presentWorld != null) presentWorld.SetActive(true);
            if (pastWorld != null) pastWorld.SetActive(false);
        }

        // ---------- API pública (conectar desde VHSPlayer / PlayerTrigger) ----------

        public void EnterTape()
        {
            if (busy || InTape)
                return;

            // Después de las 03:17 ya no se puede entrar: la noche terminó.
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

            // El tiempo del presente no corre mientras estás dentro de la cinta.
            if (clock != null)
                clock.Pause();

            // Suelta lo que el jugador tenga en las manos ANTES de apagar el mundo (ver ReleaseHeldObjects).
            ReleaseHeldObjects();
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

            ReleaseHeldObjects();
            SwapWorlds(toPast: false);
            Teleport(presentReturnPoint);
            InTape = false;
            onReturnToPresent?.Invoke();

            yield return new WaitForSeconds(blackHoldSeconds);

            if (fader != null)
                yield return fader.FadeIn();

            // ORDEN IMPORTANTE: primero reanudamos el reloj y DESPUÉS avisamos que se vio la cinta.
            // NotifyTapeWatched salta a las 03:17 y ahí el director vuelve a pausar el reloj;
            // si lo hiciéramos al revés, este Resume lo volvería a poner en marcha.
            if (clock != null)
                clock.Resume();
            if (director != null)
                director.NotifyTapeWatched();

            busy = false;
        }

        /// <summary>
        /// Obliga al jugador a soltar todo lo que tenga agarrado.
        ///
        /// Por qué: mientras un objeto está agarrado, XRI lo saca de su carpeta (ej. Pasado_cinta) y lo deja
        /// suelto en la raíz de la escena. Si apagamos el mundo así, el objeto NO se apaga con él y el
        /// jugador "se lo lleva" al otro mundo. Al soltarlo aquí, con el mundo todavía activo, XRI lo
        /// devuelve a su carpeta (Retain Transform Parent) y después se apaga junto con ella.
        /// Funciona con cualquier objeto agarrable, sin conectar eventos a mano.
        /// </summary>
        void ReleaseHeldObjects()
        {
            if (interactionManager == null || xrOrigin == null)
                return;

            // Todos los "agarradores" del jugador: mano izquierda, derecha, rayos, poke...
            foreach (IXRSelectInteractor interactor in xrOrigin.GetComponentsInChildren<IXRSelectInteractor>())
            {
                if (interactor.hasSelection)
                    interactionManager.CancelInteractorSelection(interactor);
            }
        }

        void SwapWorlds(bool toPast)
        {
            if (presentWorld != null) presentWorld.SetActive(!toPast);
            if (pastWorld != null) pastWorld.SetActive(toPast);
        }

        /// <summary>
        /// Mueve al jugador para que su CABEZA quede sobre el punto y mirando hacia su forward.
        /// No basta con mover el XR Origin: en VR la cámara está desplazada dentro del origin
        /// (el jugador se movió físicamente por su habitación), por eso usamos las funciones de XROrigin.
        /// </summary>
        void Teleport(Transform target)
        {
            if (target == null || xrOrigin == null)
                return;

            // El CharacterController "pelea" con los cambios directos de posición: lo apagamos un momento.
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
