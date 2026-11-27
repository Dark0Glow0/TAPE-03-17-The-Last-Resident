using System.Collections;
using Tape0317.Core;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tape0317.UI
{
    public class GameMenus : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] GameClock clock;
        [SerializeField] XROrigin xrOrigin;
        [Tooltip("El objeto 'Locomotion' del XR Origin. Se apaga frente a la tele para que el jugador no camine.")]
        [SerializeField] GameObject locomotion;
        [Tooltip("Opcional: para el fundido al volver a la tele.")]
        [SerializeField] ScreenFader fader;

        [Header("La tele")]
        [Tooltip("Dónde está el jugador frente a la tele (en el sillón). La flecha azul debe mirar a la tele.")]
        [SerializeField] Transform tvSpawn;
        [Tooltip("Canvas con PLAY, puesto sobre la pantalla de la tele. Activo al empezar.")]
        [SerializeField] GameObject startMenu;
        [Tooltip("Canvas del final, puesto sobre la pantalla de la tele. Empieza desactivado.")]
        [SerializeField] GameObject endMenu;
        [SerializeField] TMP_Text endTitle;
        [SerializeField] TMP_Text endSubtitle;

        [Header("Textos")]
        [SerializeField] string deathTitle = "STOP";
        [SerializeField] string deathSubtitle = "No viste la cinta antes de las 03:17.";
        [SerializeField] string victoryTitle = "FIN DE LA DEMO";
        [SerializeField] string victorySubtitle = "La cinta terminó. Pero él sigue mirando.";

        [Header("Final bueno")]
        [Tooltip("Segundos tras la Revelation antes de volver a la tele.")]
        [SerializeField, Min(0f)] float victoryMenuDelay = 3f;

        [Header("Al terminar")]
        [Tooltip("Se ocultan al volver a la tele (ej. el Mimic del ataque).")]
        [SerializeField] GameObject[] hideOnEnd;
        [Tooltip("Aparecen al volver a la tele (ej. la cámara VHS sobre la mesita).")]
        [SerializeField] GameObject[] showOnEnd;

        [Header("Cierre tras el susto")]
        [Tooltip("Segundos entre QuitAfterScare y el cierre del juego.")]
        [SerializeField, Min(0f)] float quitDelay = 0.5f;

        bool ended;

        void Reset()
        {
            clock = FindFirstObjectByType<GameClock>();
            xrOrigin = FindFirstObjectByType<XROrigin>();
            fader = FindFirstObjectByType<ScreenFader>();
        }

        void Start()
        {
            if (endMenu != null) endMenu.SetActive(false);
            if (startMenu != null) startMenu.SetActive(true);
            foreach (GameObject go in showOnEnd)
                if (go != null) go.SetActive(false);

            // El reloj no corre hasta PLAY. Pausamos igual por si quedó marcado Auto Start.
            if (clock != null)
                clock.Pause();

            SetLocomotion(false);
            StartCoroutine(PlaceAtTvAfterTracking());
        }

        IEnumerator PlaceAtTvAfterTracking()
        {
            // El visor (o el simulador) tarda unos frames en dar la posición real de la cabeza.
            yield return new WaitForSeconds(0.2f);
            TeleportTo(tvSpawn);
        }

        // ---------- Botones (conectar en el On Click de cada Button) ----------

        public void PressPlay()
        {
            if (startMenu != null)
                startMenu.SetActive(false);
            if (clock != null)
                clock.Resume();
            SetLocomotion(true);
        }

        public void PressRestart()
        {
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void PressQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void QuitAfterScare() => Invoke(nameof(PressQuit), quitDelay);

        // ---------- Finales ----------

        public void ShowDeathMenu() => StartCoroutine(EndRoutine(deathTitle, deathSubtitle, 0f));

        public void ShowVictoryMenu() => StartCoroutine(EndRoutine(victoryTitle, victorySubtitle, victoryMenuDelay));

        IEnumerator EndRoutine(string title, string subtitle, float delay)
        {
            if (ended)
                yield break;
            ended = true;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            SetLocomotion(false);

            if (fader != null)
                yield return fader.FadeOut();

            foreach (GameObject go in hideOnEnd)
                if (go != null) go.SetActive(false);

            if (startMenu != null)
                startMenu.SetActive(false);

            TeleportTo(tvSpawn);

            if (endTitle != null) endTitle.text = title;
            if (endSubtitle != null) endSubtitle.text = subtitle;
            if (endMenu != null) endMenu.SetActive(true);

            foreach (GameObject go in showOnEnd)
                if (go != null) go.SetActive(true);

            if (fader != null)
                yield return fader.FadeIn();
        }

        // ---------- Utilidades ----------

        void SetLocomotion(bool on)
        {
            if (locomotion != null)
                locomotion.SetActive(on);
        }

        void TeleportTo(Transform target)
        {
            if (target == null || xrOrigin == null)
                return;

            var controller = xrOrigin.GetComponent<CharacterController>();
            bool hadController = controller != null && controller.enabled;
            if (hadController)
                controller.enabled = false;

            xrOrigin.MatchOriginUpCameraForward(target.up, target.forward);
            xrOrigin.MoveCameraToWorldLocation(target.position + target.up * xrOrigin.CameraInOriginSpaceHeight);
            Physics.SyncTransforms();

            if (hadController)
                controller.enabled = true;
        }
    }
}
