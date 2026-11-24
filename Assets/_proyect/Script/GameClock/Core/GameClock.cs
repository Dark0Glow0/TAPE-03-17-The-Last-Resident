using System;
using UnityEngine;

namespace Tape0317.Core
{
    /// <summary>
    /// Reloj del juego (24 horas). Su ÚNICA responsabilidad es saber qué hora es y avisar
    /// cuando cambia el minuto. No sabe nada del Mimic ni de las cintas: eso lo decide el
    /// NightDirector escuchando a este reloj.
    ///
    /// CAMBIO v2: ahora la jornada puede cruzar la medianoche (08:00 → 03:35 del día siguiente).
    ///
    /// Representación interna: "minutos de línea de tiempo" = minutos desde la medianoche del
    /// PRIMER día. Si una hora es anterior a la hora de inicio, se considera del día siguiente
    /// y se le suman 1440 (24 h). Ejemplo con inicio a las 08:00:
    ///   08:00 → 480      20:00 → 1200      00:00 → 1440      03:17 → 1440 + 197 = 1637
    /// Así las comparaciones (¿ya pasaron las 03:17?) siguen siendo un simple "mayor o igual".
    /// </summary>
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

        /// <summary>Se lanza cada vez que cambia el minuto entero (hora 0-23, minuto 0-59).</summary>
        public event Action<int, int> OnMinuteChanged;

        /// <summary>Se lanza una sola vez cuando el reloj llega a la hora de fin.</summary>
        public event Action OnClockEnded;

        // Estado interno (en minutos de línea de tiempo)
        float currentMinutes;
        int lastWholeMinute;
        bool running;
        bool ended;

        // ---------- API pública (solo lectura) ----------

        /// <summary>Hora del día, 0-23 (lo que muestra el reloj de pared).</summary>
        public int CurrentHour => (lastWholeMinute / 60) % 24;
        public int CurrentMinute => lastWholeMinute % 60;
        public bool IsRunning => running;
        public bool HasEnded => ended;

        /// <summary>0 al inicio de la jornada, 1 al final. Útil para escalar la tensión.</summary>
        public float Progress => Mathf.InverseLerp(StartMinutes, EndMinutes, currentMinutes);

        /// <summary>
        /// Momento del día de 0 a 1 (0 = 00:00, 0.5 = 12:00). Pensado para el ciclo día/noche:
        /// rotar la luna/sol, cambiar el color ambiente, etc.
        /// </summary>
        public float TimeOfDay01 => (currentMinutes % MinutesPerDay) / MinutesPerDay;

        /// <summary>Hora actual en minutos de línea de tiempo (con decimales).</summary>
        public float GetTimelineMinutes() => currentMinutes;

        /// <summary>
        /// Convierte una hora del reloj (ej. 3:17) a minutos de línea de tiempo de ESTA jornada.
        /// Úsalo para comparar con GetTimelineMinutes(). Si la hora es anterior al inicio,
        /// se entiende que es del día siguiente.
        /// </summary>
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
            // Inicializamos en Awake para que cualquier script que consulte la hora en su Start
            // ya reciba la hora correcta.
            currentMinutes = StartMinutes;
            lastWholeMinute = StartMinutes;
        }

        void Start()
        {
            // Avisamos la hora inicial en Start: para entonces todos los demás ya hicieron
            // su OnEnable y están suscritos.
            Notify();

            if (autoStart)
                Resume();
        }

        void Update()
        {
            if (!running)
                return;

            // Pausamos con un bool propio y NO con Time.timeScale = 0, porque eso congelaría
            // también la física y la interacción del XR Toolkit.
            currentMinutes += Time.deltaTime / 60f * gameMinutesPerRealMinute;

            if (currentMinutes >= EndMinutes)
                currentMinutes = EndMinutes;

            // Con 80 min de juego por minuto real, avanzan ~1,3 minutos de juego por segundo.
            // Si un frame se alarga, notificamos CADA minuto intermedio para que no se pierda
            // ningún evento programado en un minuto exacto (ej. 03:17).
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

        /// <summary>Detiene el avance (ej. mientras el jugador está dentro de una cinta).</summary>
        public void Pause() => running = false;

        public void Resume()
        {
            // Una vez terminada la jornada no se puede reanudar (evita relanzar eventos).
            if (!ended)
                running = true;
        }

        /// <summary>
        /// Salto narrativo de la hora (ej. al volver de la cinta → 03:17).
        /// A diferencia del avance normal, NO se notifican los minutos intermedios, solo el destino:
        /// un salto es un corte de escena, no tiempo que "pasó".
        /// </summary>
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

        // ---------- Depuración (clic derecho sobre el componente en el Inspector) ----------

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
