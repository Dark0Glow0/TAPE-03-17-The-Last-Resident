using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Tape0317.UI
{
    /// <summary>
    /// Pantalla de muerte estilo VHS: corte seco a negro con "STOP" parpadeando, como una cinta detenida.
    /// Después de unos segundos se oculta y lanza On Finished (ej. mostrar el menú final).
    ///
    /// Montaje: va en el Canvas "Pantalla_Muerte" (hijo de la Main Camera), que empieza
    /// DESACTIVADO. Se activa desde NightDirector → On Player Died → SetActive(true).
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        [Tooltip("Texto principal (STOP). Opcional: si está, parpadea como en un VHS.")]
        [SerializeField] TMP_Text stopLabel;
        [SerializeField, Min(0.1f)] float blinkSeconds = 0.6f;

        [Header("Sonido (opcional)")]
        [Tooltip("AudioSource 2D (Spatial Blend = 0) en este mismo Canvas.")]
        [SerializeField] AudioSource sfx;
        [Tooltip("El 'clac' mecánico de un VHS al detenerse.")]
        [SerializeField] AudioClip stopClip;

        [Header("Final")]
        [Tooltip("Segundos en la pantalla STOP antes de lanzar On Finished.")]
        [SerializeField, Min(0.5f)] float showSeconds = 3f;
        [Tooltip("Conectar a GameMenus.ShowDeathMenu.")]
        [SerializeField] UnityEvent onFinished;

        void OnEnable()
        {
            // Silencio total menos este sonido: corta la respiración del Mimic, ruidos, etc.
            // AudioListener.pause detiene todos los AudioSource; el nuestro usa ignoreListenerPause.
            AudioListener.pause = true;
            if (sfx != null)
            {
                sfx.ignoreListenerPause = true;
                if (stopClip != null)
                    sfx.PlayOneShot(stopClip);
            }

            StartCoroutine(Run());
        }

        void OnDisable()
        {
            // Nunca dejar el audio pausado si este objeto se apaga o se recarga la escena.
            AudioListener.pause = false;
        }

        IEnumerator Run()
        {
            float t = 0f;
            float blink = 0f;

            while (t < showSeconds)
            {
                // unscaledDeltaTime: funciona aunque alguien haya cambiado Time.timeScale.
                t += Time.unscaledDeltaTime;
                blink += Time.unscaledDeltaTime;

                if (stopLabel != null && blink >= blinkSeconds)
                {
                    blink = 0f;
                    stopLabel.enabled = !stopLabel.enabled;
                }

                yield return null;
            }

            onFinished?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
