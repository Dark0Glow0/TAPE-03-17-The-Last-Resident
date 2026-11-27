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

        public void NotifyTapeWatched()
        {
            if (TapeWatched || CurrentPhase != NightPhase.Calm)
                return;

            TapeWatched = true;
            Debug.Log("[NightDirector] Cinta vista a tiempo");

            if (jumpToCursedHourOnTapeWatched)
                clock.SetTime(cursedHour, cursedMinute);
        }

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

        // ---------- Depuración ----------

        [ContextMenu("Debug/Simular cinta vista")]
        void DebugTapeWatched() => NotifyTapeWatched();

        [ContextMenu("Debug/Simular atrapado por el Mimic")]
        void DebugKillPlayer() => KillPlayer();
    }
}
