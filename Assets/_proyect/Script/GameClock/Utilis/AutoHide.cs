using UnityEngine;
using UnityEngine.Events;

namespace Tape0317.Tape
{
    public class AutoHide : MonoBehaviour
    {
        [Tooltip("Segundos visible antes de desaparecer.")]
        [SerializeField, Min(0.1f)] float seconds = 4f;

        [Tooltip("Se lanza cuando desaparece SOLO (al cumplirse el tiempo), no si otro lo apaga antes. " + "Ej.: mostrar el menú final cuando el Mimic se va.")]
        [SerializeField] UnityEvent onHidden;

        void OnEnable()
        {
            Invoke(nameof(Hide), seconds);
            Debug.Log("aparece");
        }

        void OnDisable()
        {
            CancelInvoke(nameof(Hide));
            Debug.Log("se va");
        }

        void Hide()
        {
            gameObject.SetActive(false);
            onHidden?.Invoke();
        }
    }
}
