using System.Collections;
using UnityEngine;

namespace Tape0317.Core
{
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
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void Start()
        {
            if (startBlack)
                StartCoroutine(FadeIn());
        }

        public IEnumerator FadeOut(float duration = -1f) => Fade(1f, duration);

        public IEnumerator FadeIn(float duration = -1f) => Fade(0f, duration);

        IEnumerator Fade(float target, float duration)
        {
            if (duration < 0f)
                duration = defaultDuration;

            float start = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
