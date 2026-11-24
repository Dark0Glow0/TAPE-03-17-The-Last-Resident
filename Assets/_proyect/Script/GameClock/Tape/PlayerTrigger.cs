using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

namespace Tape0317.Tape
{
    /// <summary>
    /// Zona invisible que lanza un evento cuando el jugador entra. Es la pieza básica para
    /// guionizar los niveles de las cintas sin escribir un script por cada momento:
    ///   - Círculo ritual → sonido detrás del jugador + activar la zona de la casa.
    ///   - Puerta de la casa → volver al presente.
    ///
    /// Detecta al jugador buscando el XROrigin en los padres del collider que entró
    /// (el CharacterController del XR Origin dispara los triggers al moverse).
    /// Si no se dispara, agrégale a esta zona un Rigidbody con "Is Kinematic" activado.
    /// </summary>
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
            // Al agregar el componente, dejamos el collider como trigger automáticamente.
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

        /// <summary>Permite volver a usar la zona (por ejemplo, al reiniciar el nivel).</summary>
        public void ResetTrigger() => fired = false;
    }
}
