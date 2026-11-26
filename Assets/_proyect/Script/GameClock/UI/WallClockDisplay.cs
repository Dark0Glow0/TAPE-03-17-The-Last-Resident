using TMPro;
using Tape0317.Core;
using UnityEngine;

namespace Tape0317.UI
{
    /// <summary>
    /// Muestra la hora del GameClock en un texto: el reloj de pared, la esquina de los menús VHS, etc.
    /// No consulta la hora en cada Update: solo se actualiza cuando el reloj avisa que cambió el minuto.
    ///
    /// Formatos:
    ///  - Digital24: "14:41"  (reloj de pared)
    ///  - VHS:       "PM 2:41" (esquina de la videocasetera)
    /// </summary>
    public class WallClockDisplay : MonoBehaviour
    {
        public enum ClockFormat { Digital24, VHS }

        [SerializeField] GameClock clock;

        [Tooltip("TMP_Text sirve tanto para TextMeshPro (3D en el mundo) como para TextMeshProUGUI (Canvas).")]
        [SerializeField] TMP_Text label;

        [SerializeField] ClockFormat format = ClockFormat.Digital24;

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
            if (clock == null)
                return;

            clock.OnMinuteChanged += Show;

            // Mostramos la hora actual apenas se enciende (ej. al prender el menú del reproductor
            // a mitad del juego), sin esperar al próximo cambio de minuto.
            Show(clock.CurrentHour, clock.CurrentMinute);
        }

        void OnDisable()
        {
            if (clock != null)
                clock.OnMinuteChanged -= Show;
        }

        void Start()
        {
            // Por si nos suscribimos antes de que el reloj inicializara su hora
            // (depende del orden en que Unity inicializa los objetos).
            if (clock != null)
                Show(clock.CurrentHour, clock.CurrentMinute);
        }

        void Show(int hour, int minute)
        {
            if (label == null)
                return;

            if (format == ClockFormat.VHS)
            {
                string ampm = hour < 12 ? "AM" : "PM";
                int h12 = hour % 12 == 0 ? 12 : hour % 12;
                label.text = $"{ampm} {h12}:{minute:00}";
            }
            else
            {
                label.text = $"{hour:00}:{minute:00}";
            }
        }
    }
}
