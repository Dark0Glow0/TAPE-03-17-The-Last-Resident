using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

namespace Tape0317.Tape
{
    [RequireComponent(typeof(Collider))]
    public class PlayerTrigger : MonoBehaviour
    {
        [Tooltip("Si está activo, solo se dispara la primera vez.")]
        [SerializeField] bool once = true;
        [Tooltip("Segundos de espera entre entrar y lanzar el evento (para que el susto 'respire').")]
        [SerializeField, Min(0f)] float delay;
        [SerializeField] UnityEvent onPlayerEnter;

        bool fired;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (once && fired)
                return;

            // Ignoramos todo lo que no sea parte del jugador (objetos que caen, la cámara VHS, etc.).
            if (other.GetComponentInParent<XROrigin>() == null)
                return;

            fired = true;

            if (delay > 0f)
                StartCoroutine(FireAfterDelay());
            else
                onPlayerEnter?.Invoke();
        }

        IEnumerator FireAfterDelay()
        {
            yield return new WaitForSeconds(delay);
            onPlayerEnter?.Invoke();
        }

        public void ResetTrigger() => fired = false;
    }
}
