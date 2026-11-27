using System;
using UnityEngine;

namespace Tape0317.Core
{
    public class GameClock : MonoBehaviour
    {
        const int MinutesPerDay = 24 * 60;

        [Header("Horario de la jornada")]
        [SerializeField, Range(0, 23)] int startHour = 8;
        [SerializeField, Range(0, 59)] int startMinute = 0;
        [Tooltip("Si es anterior a la hora de inicio, se entiende que es del día siguiente.")]
        [SerializeField, Range(0, 23)] int endHour = 3;
        [SerializeField, Range(0, 59)] int endMinute = 35;

        [Header("Velocidad")]
        [Tooltip("Minutos de juego por cada minuto real. Con 80: 1 hora de juego = 45 s reales, " +
                 "y de 08:00 a 03:17 pasan unos 14,5 minutos reales.")]
        [SerializeField, Min(0.01f)] float gameMinutesPerRealMinute = 80f;

        [Tooltip("Si está activo, el reloj empieza a correr solo al iniciar la escena.")]
        [SerializeField] bool autoStart = true;

        public event Action<int, int> OnMinuteChanged;

        public event Action OnClockEnded;

        float currentMinutes;
        int lastWholeMinute;
        bool running;
        bool ended;

        // ---------- API pública (solo lectura) ----------

        public int CurrentHour => (lastWholeMinute / 60) % 24;
        public int CurrentMinute => lastWholeMinute % 60;
        public bool IsRunning => running;
        public bool HasEnded => ended;

        public float Progress => Mathf.InverseLerp(StartMinutes, EndMinutes, currentMinutes);

        public float TimeOfDay01 => (currentMinutes % MinutesPerDay) / MinutesPerDay;

        public float GetTimelineMinutes() => currentMinutes;

        public int ToTimelineMinutes(int hour, int minute)
        {
            int m = hour * 60 + minute;
            if (m < StartOfDayMinutes)
                m += MinutesPerDay;
            return m;
        }

        int StartOfDayMinutes => startHour * 60 + startMinute;
        int StartMinutes => StartOfDayMinutes;
        int EndMinutes => ToTimelineMinutes(endHour, endMinute);

        // ---------- Ciclo de vida de Unity ----------

        void Awake()
        {
            currentMinutes = StartMinutes;
            lastWholeMinute = StartMinutes;
        }

        void Start()
        {
            Notify();

            if (autoStart)
                Resume();
        }

        void Update()
        {
            if (!running)
                return;

            currentMinutes += Time.deltaTime / 60f * gameMinutesPerRealMinute;

            if (currentMinutes >= EndMinutes)
                currentMinutes = EndMinutes;

            int whole = Mathf.FloorToInt(currentMinutes);
            while (lastWholeMinute < whole)
            {
                lastWholeMinute++;
                Notify();
            }

            if (currentMinutes >= EndMinutes)
                EndClock();
        }

        // ---------- Control ----------

        public void Pause() => running = false;

        public void Resume()
        {
            if (!ended)
                running = true;
        }

        public void SetTime(int hour, int minute)
        {
            int target = Mathf.Clamp(ToTimelineMinutes(hour, minute), StartMinutes, EndMinutes);
            currentMinutes = target;
            lastWholeMinute = target;
            Notify();

            if (target >= EndMinutes)
                EndClock();
        }

        void EndClock()
        {
            if (ended)
                return;
            ended = true;
            running = false;
            OnClockEnded?.Invoke();
        }

        void Notify() => OnMinuteChanged?.Invoke(CurrentHour, CurrentMinute);

        // ---------- Depuración ----------

        [ContextMenu("Debug/Saltar a 20:00")]
        void DebugJumpTo2000() => SetTime(20, 0);

        [ContextMenu("Debug/Saltar a 03:15")]
        void DebugJumpTo0315() => SetTime(3, 15);

        [ContextMenu("Debug/Saltar a 03:29")]
        void DebugJumpTo0329() => SetTime(3, 29);

        [ContextMenu("Debug/Pausar o reanudar")]
        void DebugTogglePause()
        {
            if (running) Pause();
            else Resume();
        }
    }
}
