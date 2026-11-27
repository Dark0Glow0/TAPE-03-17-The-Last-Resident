using TMPro;
using Tape0317.Core;
using UnityEngine;

namespace Tape0317.UI
{
    public class WallClockDisplay : MonoBehaviour
    {
        public enum ClockFormat { Digital24, VHS }

        [SerializeField] GameClock clock;

        [Tooltip("TMP_Text sirve tanto para TextMeshPro (3D en el mundo) como para TextMeshProUGUI (Canvas).")]
        [SerializeField] TMP_Text label;

        [SerializeField] ClockFormat format = ClockFormat.Digital24;

        void Reset()
        {
            label = GetComponentInChildren<TMP_Text>();
            clock = FindFirstObjectByType<GameClock>();
        }

        void OnEnable()
        {

            if (clock == null)
                return;

            clock.OnMinuteChanged += Show;

            Show(clock.CurrentHour, clock.CurrentMinute);
        }

        void OnDisable()
        {
            if (clock != null)
                clock.OnMinuteChanged -= Show;
        }

        void Start()
        {
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
