using System;
using UnityEngine;
using UnityEngine.Events;

namespace Tape0317.Core
{
    public enum NightPhase
    {
        Calm,           // 02:45 → 03:17: explorar, encontrar y ver la cinta
        Manifestation,  // 03:17 → 03:30: empiezan las cosas raras
        Hunt            // 03:30 →     : el Mimic caza (solo si NO se vio la cinta)
    }

    /// <summary>
    /// Traduce la hora del GameClock a fases de la noche y avisa cuando cambia la fase.
    /// Los demás sistemas (Mimic, radio, luces, sonidos) escuchan OnPhaseChanged y reaccionan.
    ///
    /// Regla de la cinta:
    ///  - Si el jugador ve la cinta ANTES de las 03:17 → el reloj salta a 03:17 y la manifestación
    ///    es "guionizada" (TapeWatched = true). Nunca llega la caza.
    ///  - Si llegan las 03:17 sin verla → manifestación hostil y a las 03:30 empieza la caza.
    /// </summary>
    public class NightDirector : MonoBehaviour
    {
        [SerializeField] GameClock clock;

        [Header("Horario de fases")]
        [SerializeField, Range(0, 23)] int manifestationHour = 3;
        [SerializeField, Range(0, 59)] int manifestationMinute = 17;
        [SerializeField, Range(0, 23)] int huntHour = 3;
        [SerializeField, Range(0, 59)] int huntMinute = 30;

        [Header("Eventos para conectar desde el Inspector (útil para probar rápido)")]
        [SerializeField] UnityEvent onManifestationStarted;
        [SerializeField] UnityEvent onHuntStarted;

        /// <summary>Para código: (nueva fase). Los listeners pueden consultar TapeWatched.</summary>
        public event Action<NightPhase> OnPhaseChanged;

        public NightPhase CurrentPhase { get; private set; } = NightPhase.Calm;

        /// <summary>True si el jugador vio la cinta. Cambia el tono de la manifestación y anula la caza.</summary>
        public bool TapeWatched { get; private set; }

        /// <summary>Atajo para el Mimic: ¿la noche es peligrosa ahora mismo?</summary>
        public bool IsHostile => CurrentPhase != NightPhase.Calm && !TapeWatched;

        void Reset() => clock = FindFirstObjectByType<GameClock>();

        void OnEnable()
        {
            if (clock != null)
                clock.OnMinuteChanged += HandleMinuteChanged;
        }

        void OnDisable()
        {
            if (clock != null)
                clock.OnMinuteChanged -= HandleMinuteChanged;
        }

        /// <summary>
        /// Lo llamará el VHSPlayer / GameManager cuando la cinta termine de reproducirse.
        /// </summary>
        public void NotifyTapeWatched()
        {
            if (TapeWatched)
                return;

            TapeWatched = true;

            // Si aún no eran las 03:17, la cinta "se corta" y el reloj salta a esa hora.
            // SetTime lanza OnMinuteChanged → HandleMinuteChanged → pasamos a Manifestation.
            if (CurrentPhase == NightPhase.Calm)
                clock.SetTime(manifestationHour, manifestationMinute);
        }

        void HandleMinuteChanged(int hour, int minute)
        {
            int now = GameClock.ToMinutes(hour, minute);
            NightPhase target = EvaluatePhase(now);

            // Solo avanzamos de fase, nunca retrocedemos (la noche no "se calma" sola).
            if (target > CurrentPhase)
                SetPhase(target);
        }

        NightPhase EvaluatePhase(int now)
        {
            int manifestation = GameClock.ToMinutes(manifestationHour, manifestationMinute);
            int hunt = GameClock.ToMinutes(huntHour, huntMinute);

            // La caza solo existe si el jugador NO vio la cinta.
            if (now >= hunt && !TapeWatched)
                return NightPhase.Hunt;

            if (now >= manifestation)
                return NightPhase.Manifestation;

            return NightPhase.Calm;
        }

        void SetPhase(NightPhase phase)
        {
            CurrentPhase = phase;
            Debug.Log($"[NightDirector] Fase: {phase} (cinta vista: {TapeWatched})");

            OnPhaseChanged?.Invoke(phase);

            switch (phase)
            {
                case NightPhase.Manifestation: onManifestationStarted?.Invoke(); break;
                case NightPhase.Hunt: onHuntStarted?.Invoke(); break;
            }
        }

        // ---------- Depuración ----------

        [ContextMenu("Debug/Simular cinta vista")]
        void DebugTapeWatched() => NotifyTapeWatched();
    }
}
