using TMPro;
using Tape0317.Core;
using UnityEngine;

namespace Tape0317.UI
{
    /// <summary>
    /// Muestra la hora del GameClock en un texto (por ejemplo, un reloj despertador LED en la sala).
    /// No consulta la hora en cada Update: solo se actualiza cuando el reloj avisa que cambió el minuto.
    /// </summary>
    public class WallClockDisplay : MonoBehaviour
    {
        [SerializeField] GameClock clock;

        [Tooltip("TMP_Text sirve tanto para TextMeshPro (3D en el mundo) como para TextMeshProUGUI (Canvas).")]
        [SerializeField] TMP_Text label;

        void Reset()
        {
            // Reset se llama al agregar el componente en el editor: intenta autoasignar referencias.
            label = GetComponentInChildren<TMP_Text>();
            clock = FindFirstObjectByType<GameClock>();
        }

        void OnEnable()
        {
            // Suscribirse en OnEnable y desuscribirse en OnDisable es el patrón estándar:
            // si este objeto se desactiva o destruye, el reloj no intentará llamarlo.
            if (clock != null)
                clock.OnMinuteChanged += Show;
        }

        void OnDisable()
        {
            if (clock != null)
                clock.OnMinuteChanged -= Show;
        }

        void Start()
        {
            // Por si nos suscribimos después de que el reloj lanzara su primer aviso
            // (depende del orden en que Unity inicializa los objetos).
            if (clock != null)
                Show(clock.CurrentHour, clock.CurrentMinute);
        }

        void Show(int hour, int minute)
        {
            if (label != null)
                label.text = $"{hour:00}:{minute:00}";
        }
    }
}
