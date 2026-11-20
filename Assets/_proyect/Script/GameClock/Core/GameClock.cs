using System;
using UnityEngine;

namespace Tape0317.Core
{
    /// <summary>
    /// Reloj del juego. Su ÚNICA responsabilidad es saber qué hora es dentro de la noche
    /// y avisar cuando cambia el minuto. No sabe nada del Mimic, de las cintas ni del terror:
    /// eso lo decide el NightDirector escuchando a este reloj.
    ///
    /// Representación interna: minutos desde la medianoche en un float
    /// (02:45 = 165, 03:17 = 197, 03:35 = 215). Es simple de comparar e interpolar.
    /// Limitación conocida: no soporta noches que crucen la medianoche (23:50 → 00:10).
    /// Para este juego no hace falta.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        [Header("Horario de la noche")]
        [SerializeField, Range(0, 23)] int startHour = 2;
        [SerializeField, Range(0, 59)] int startMinute = 45;
        [SerializeField, Range(0, 23)] int endHour = 3;
        [SerializeField, Range(0, 59)] int endMinute = 35;

        [Header("Velocidad")]
        [Tooltip("Cuántos minutos de juego pasan por cada minuto real. " +
                 "Con 4: de 02:45 a 03:17 pasan 8 minutos reales.")]
        [SerializeField, Min(0.01f)] float gameMinutesPerRealMinute = 4f;

        [Tooltip("Si está activo, el reloj empieza a correr solo al iniciar la escena.")]
        [SerializeField] bool autoStart = true;

        /// <summary>Se lanza cada vez que cambia el minuto entero (hora, minuto).</summary>
        public event Action<int, int> OnMinuteChanged;

        /// <summary>Se lanza una sola vez cuando el reloj llega a la hora de fin.</summary>
        public event Action OnClockEnded;

        // Estado interno
        float currentMinutes;   // minutos desde medianoche, con decimales
        int lastWholeMinute;    // último minuto entero que ya notificamos
        bool running;
        bool ended;

        // ---------- API pública (solo lectura) ----------

        public int CurrentHour => lastWholeMinute / 60;
        public int CurrentMinute => lastWholeMinute % 60;
        public bool IsRunning => running;
        public bool HasEnded => ended;

        /// <summary>0 al inicio de la noche, 1 al final. Útil para escalar la tensión.</summary>
        public float Progress => Mathf.InverseLerp(StartMinutes, EndMinutes, currentMinutes);

        /// <summary>Hora actual en minutos desde medianoche (con decimales).</summary>
        public float GetMinutesOfDay() => currentMinutes;

        /// <summary>Convierte una hora a "minutos desde medianoche" para comparar con GetMinutesOfDay().</summary>
        public static int ToMinutes(int hour, int minute) => hour * 60 + minute;

        int StartMinutes => ToMinutes(startHour, startMinute);
        int EndMinutes => ToMinutes(endHour, endMinute);

        // ---------- Ciclo de vida de Unity ----------

        void Awake()
        {
            // Inicializamos en Awake para que cualquier otro script que consulte
            // CurrentHour/CurrentMinute en su Start ya reciba la hora correcta.
            currentMinutes = StartMinutes;
            lastWholeMinute = StartMinutes;
        }

        void Start()
        {
            // Avisamos la hora inicial en Start (no en Awake) porque en Start todos los
            // demás objetos ya ejecutaron su OnEnable y por lo tanto ya están suscritos.
            Notify();

            if (autoStart)
                Resume();
        }

        void Update()
        {
            if (!running)
                return;

            // Usamos Time.deltaTime (no Time.timeScale = 0 para pausar) porque congelar
            // timeScale también congelaría la física y la interacción del XR Toolkit.
            currentMinutes += Time.deltaTime / 60f * gameMinutesPerRealMinute;

            if (currentMinutes >= EndMinutes)
                currentMinutes = EndMinutes;

            // Si un frame fue muy largo (tirón al cargar algo en el Quest) podríamos saltar
            // varios minutos de golpe. Notificamos CADA minuto intermedio para que ningún
            // evento programado en un minuto exacto se pierda.
            int whole = Mathf.FloorToInt(currentMinutes);
            while (lastWholeMinute < whole)
            {
                lastWholeMinute++;
                Notify();
            }

            if (currentMinutes >= EndMinutes && !ended)
            {
                ended = true;
                running = false;
                OnClockEnded?.Invoke();
            }
        }

        // ---------- Control ----------

        public void Pause() => running = false;

        public void Resume()
        {
            // Una vez terminada la noche no se puede reanudar (evita relanzar eventos).
            if (!ended)
                running = true;
        }

        /// <summary>
        /// Salto narrativo de la hora (por ejemplo, al terminar la cinta → 03:17).
        /// A diferencia del avance normal, aquí NO se notifican los minutos intermedios:
        /// solo la hora de destino. Un salto es un corte de escena, no tiempo que "pasó".
        /// </summary>
        public void SetTime(int hour, int minute)
        {
            int target = Mathf.Clamp(ToMinutes(hour, minute), StartMinutes, EndMinutes);
            currentMinutes = target;
            lastWholeMinute = target;
            Notify();

            if (target >= EndMinutes && !ended)
            {
                ended = true;
                running = false;
                OnClockEnded?.Invoke();
            }
        }

        void Notify() => OnMinuteChanged?.Invoke(CurrentHour, CurrentMinute);

        // ---------- Depuración (clic derecho sobre el componente en el Inspector) ----------

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
