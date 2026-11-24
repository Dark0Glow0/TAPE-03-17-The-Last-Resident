using UnityEngine;

namespace Tape0317.Tape
{
    /// <summary>
    /// Desactiva este objeto unos segundos después de que se active.
    /// Uso: apariciones que se dejan ver un momento y se van (ej. el Mimic en la ventana).
    /// Se reinicia cada vez que el objeto se vuelve a activar.
    /// </summary>
    public class AutoHide : MonoBehaviour
    {
        [Tooltip("Segundos visible antes de desaparecer.")]
        [SerializeField, Min(0.1f)] float seconds = 4f;

        void OnEnable()
        {
            // Invoke es más simple que una corrutina para "llamar a X dentro de N segundos".
            Invoke(nameof(Hide), seconds);
            Debug.Log("aparece");
        }

        void OnDisable()
        {
            // Si alguien lo desactiva antes de tiempo, cancelamos la llamada pendiente
            // para que no se ejecute la próxima vez que se active.
            CancelInvoke(nameof(Hide));
            Debug.Log("se va");
        }

        void Hide() => gameObject.SetActive(false);
    }
}
