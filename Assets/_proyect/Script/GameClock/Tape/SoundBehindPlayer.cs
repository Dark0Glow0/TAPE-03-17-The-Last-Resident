using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Tape0317.Tape
{
    public class SoundBehindPlayer : MonoBehaviour
    {
        [SerializeField] AudioClip clip;
        [SerializeField, Range(0f, 1f)] float volume = 1f;
        [Tooltip("Metros detrás de la cabeza del jugador.")]
        [SerializeField, Min(0.5f)] float distance = 3f;

        [Header("Aparición (opcional)")]
        [Tooltip("Objeto con el pivote en los pies. Debe empezar desactivado.")]
        [SerializeField] GameObject apparition;
        [SerializeField, Min(0.1f)] float apparitionSeconds = 2.5f;
        [SerializeField] float playerEyeHeight = 1.6f;

        [SerializeField] UnityEvent onPlayed;

        public void Play()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            Transform head = cam.transform;

            // "Detrás" en horizontal: ignoramos si el jugador mira hacia arriba o abajo.
            Vector3 back = -head.forward;
            back.y = 0f;
            if (back.sqrMagnitude < 0.001f)
                back = Vector3.back;
            back.Normalize();

            Vector3 soundPosition = head.position + back * distance;

            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, soundPosition, volume);

            if (apparition != null)
            {
                Vector3 feet = soundPosition + Vector3.down * playerEyeHeight;
                apparition.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(-back, Vector3.up));
                StopAllCoroutines();
                StartCoroutine(ShowApparition());
            }

            onPlayed?.Invoke();
        }

        IEnumerator ShowApparition()
        {
            apparition.SetActive(true);
            yield return new WaitForSeconds(apparitionSeconds);
            apparition.SetActive(false);
        }
    }
}
