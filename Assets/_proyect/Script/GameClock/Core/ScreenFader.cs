using System.Collections;
using UnityEngine;

namespace Tape0317.Core
{
    /// <summary>
    /// Fundido a negro para VR. En VR NUNCA se debe cortar de golpe ni mover al jugador con la
    /// vista abierta: marea. Siempre: fundido a negro → mover/cambiar → fundido de vuelta.
    ///
    /// Montaje: Canvas (World Space) HIJO de la Main Camera del XR Origin, a 0.1 m delante,
    /// con un Image negro que lo cubra entero y un CanvasGroup. Este script va en ese Canvas.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] float defaultDuration = 0.6f;
        [Tooltip("Empieza en negro y se aclara al iniciar la escena (entrada suave al juego).")]
        [SerializeField] bool startBlack = true;

        CanvasGroup group;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = startBlack ? 1f : 0f;
            // El fundido nunca debe bloquear los rayos de los controles.
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void Start()
        {
            if (startBlack)
                StartCoroutine(FadeIn());
        }

        /// <summary>Oscurece. Úsalo con "yield return fader.FadeOut();" dentro de una corrutina.</summary>
        public IEnumerator FadeOut(float duration = -1f) => Fade(1f, duration);

        /// <summary>Aclara.</summary>
        public IEnumerator FadeIn(float duration = -1f) => Fade(0f, duration);

        IEnumerator Fade(float target, float duration)
        {
            if (duration < 0f)
                duration = defaultDuration;

            float start = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                // unscaledDeltaTime: el fundido funciona aunque alguien cambie Time.timeScale.
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
