using System;
using UnityEngine;
using UnityEngine.Events;

namespace Tape0317.Core
{
    public enum NightPhase
    {
        Calm,        // 08:00 → 03:17: explorar la casa, encontrar la cinta y jugarla
        Revelation,  // 03:17 CON la cinta vista: susto "guionizado" y fin de la demo
        Attack,      // 03:17 SIN la cinta vista: el Mimic entra y te atrapa
        Death        // el Mimic te atrapó → pantalla de derrota
    }

    /// <summary>
    /// Decide qué pasa a las 03:17 según si el jugador vio la cinta o no.
    /// No hace sustos: solo cambia de fase y avisa. Quien reacciona es otro script
    /// (MimicAttackSequence, la pantalla final, etc.).
    ///
    /// Reglas (MVP, versión simple):
    ///  - Ver la cinta ANTES de las 03:17 → al volver, el reloj salta a 03:17 → Revelation.
    ///  - Llegar a las 03:17 SIN verla → Attack → el Mimic entra y llama a KillPlayer() → Death.
    ///  - A las 03:17 el reloj se detiene en ambos casos: la noche "terminó".
    /// </summary>
    public class NightDirector : MonoBehaviour
    {
        [SerializeField] GameClock clock;

        [Header("La hora maldita")]
        [SerializeField, Range(0, 23)] int cursedHour = 3;
        [SerializeField, Range(0, 59)] int cursedMinute = 17;

        [Header("Cinta")]
        [Tooltip("Si está activo y la cinta se ve antes de las 03:17, al volver el reloj salta " +
                 "directo a las 03:17 (corte narrativo). Si no, la jornada sigue hasta esa hora.")]
        [SerializeField] bool jumpToCursedHourOnTapeWatched = true;

        [Header("Eventos para conectar desde el Inspector")]
        [Tooltip("03:17 habiendo visto la cinta: Mimic en la ventana, pantalla '1/7', etc.")]
        [SerializeField] UnityEvent onRevelation;
        [Tooltip("03:17 sin haber visto la cinta. El MimicAttackSequence ya escucha esto por código.")]
        [SerializeField] UnityEvent onAttackStarted;
        [Tooltip("El Mimic atrapó al jugador: pantalla de muerte / 'STOP' de VHS.")]
        [SerializeField] UnityEvent onPlayerDied;

        /// <summary>Para código: (nueva fase).</summary>
        public event Action<NightPhase> OnPhaseChanged;

        public NightPhase CurrentPhase { get; private set; } = NightPhase.Calm;
        public bool TapeWatched { get; private set; }

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

        // ---------- API pública ----------

        /// <summary>
        /// Lo llamará el DualExperienceManager cuando el jugador termine la experiencia de la cinta
        /// y vuelva a la casa. Solo cuenta si todavía no son las 03:17.
        /// </summary>
        public void NotifyTapeWatched()
        {
            if (TapeWatched || CurrentPhase != NightPhase.Calm)
                return;

            TapeWatched = true;
            Debug.Log("[NightDirector] Cinta vista a tiempo");

            // SetTime lanza OnMinuteChanged(3,17) → HandleMinuteChanged → Revelation.
            if (jumpToCursedHourOnTapeWatched)
                clock.SetTime(cursedHour, cursedMinute);
        }

        /// <summary>Lo llama el Mimic cuando atrapa al jugador. Solo tiene efecto durante el ataque.</summary>
        public void KillPlayer()
        {
            if (CurrentPhase == NightPhase.Attack)
                SetPhase(NightPhase.Death);
        }

        // ---------- Lógica interna ----------

        void HandleMinuteChanged(int hour, int minute)
        {
            if (CurrentPhase != NightPhase.Calm)
                return;

            // Minutos de línea de tiempo: así 03:17 (del día siguiente) es MAYOR que 20:00.
            int now = clock.ToTimelineMinutes(hour, minute);
            int cursed = clock.ToTimelineMinutes(cursedHour, cursedMinute);

            if (now >= cursed)
            {
                // La noche se detiene a las 03:17 pase lo que pase.
                clock.Pause();
                SetPhase(TapeWatched ? NightPhase.Revelation : NightPhase.Attack);
            }
        }

        void SetPhase(NightPhase phase)
        {
            CurrentPhase = phase;
            Debug.Log($"[NightDirector] Fase: {phase}");

            OnPhaseChanged?.Invoke(phase);

            switch (phase)
            {
                case NightPhase.Revelation: onRevelation?.Invoke(); break;
                case NightPhase.Attack: onAttackStarted?.Invoke(); break;
                case NightPhase.Death: onPlayerDied?.Invoke(); break;
            }
        }

        // ---------- Depuración (clic derecho sobre el componente en el Inspector) ----------

        [ContextMenu("Debug/Simular cinta vista")]
        void DebugTapeWatched() => NotifyTapeWatched();

        [ContextMenu("Debug/Simular atrapado por el Mimic")]
        void DebugKillPlayer() => KillPlayer();
    }
}
