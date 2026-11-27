using UnityEngine;

namespace Tape0317.Core
{
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] GameClock clock;
        [Tooltip("La luz direccional de la escena (sol de día, luna de noche).")]
        [SerializeField] Light sun;

        [Header("Orientación")]
        [Tooltip("Desde qué lado sale el sol (giro en Y).")]
        [SerializeField, Range(-180f, 180f)] float sunYaw = -30f;

        [Header("Sol (de día)")]
        [Tooltip("Color del sol a lo largo del día (izquierda = amanecer, derecha = atardecer).")]
        [SerializeField] Gradient sunColor;
        [SerializeField, Min(0f)] float sunMaxIntensity = 1.1f;

        [Header("Luna (de noche)")]
        [SerializeField] Color moonColor = new Color(0.45f, 0.55f, 0.8f);
        [SerializeField, Min(0f)] float moonIntensity = 0.08f;

        [Header("Ambiente y niebla (día ↔ noche)")]
        [SerializeField] Color ambientDay = new Color(0.55f, 0.52f, 0.48f);
        [SerializeField] Color ambientNight = new Color(0.04f, 0.05f, 0.08f);
        [SerializeField] Color fogDay = new Color(0.6f, 0.62f, 0.6f);
        [SerializeField] Color fogNight = new Color(0.12f, 0.15f, 0.26f);

        [Header("Lámparas que se encienden de noche (opcional)")]
        [SerializeField] Light[] nightLamps;
        [Tooltip("Hora a la que se encienden (y se apagan al amanecer, a las 06:30).")]
        [SerializeField, Range(0f, 24f)] float lampsOnHour = 19f;

        bool useFixedHour;
        float fixedHour;

        void Reset()
        {
            clock = FindFirstObjectByType<GameClock>();
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }

            sunColor = new Gradient();
            sunColor.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.55f, 0.3f), 0f),    // amanecer: naranjo
                    new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.3f), // mañana: blanco cálido
                    new GradientColorKey(new Color(1f, 0.97f, 0.9f), 0.7f),  // tarde
                    new GradientColorKey(new Color(1f, 0.45f, 0.2f), 1f),    // atardecer: naranjo rojizo
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        }

        void Update()
        {
            float hour = useFixedHour ? fixedHour
                       : clock != null ? clock.TimeOfDay01 * 24f
                       : 12f;
            Apply(hour);
        }

        // ---------- API (conectar desde eventos) ----------

        public void SetFixedHour(float hour)
        {
            useFixedHour = true;
            fixedHour = Mathf.Repeat(hour, 24f);
        }

        public void FollowClock() => useFixedHour = false;

        // ---------- Lógica ----------

        void Apply(float hour)
        {
            if (sun == null)
                return;

            // 06:00 → 0° (horizonte), 12:00 → 90° (arriba), 18:00 → 180°, 00:00 → 270° (bajo el suelo).
            float sunAngle = (hour - 6f) / 24f * 360f;
            float elevation = Mathf.Sin(sunAngle * Mathf.Deg2Rad); // 1 = mediodía, -1 = medianoche

            // Cuánto "día" hay: 0 de noche, 1 a pleno día. Transición suave en el amanecer/atardecer.
            float day = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.1f, 0.25f, elevation));

            if (elevation > -0.05f)
            {
                // SOL: posición según la hora. Color según el avance del día (06:00 → 18:00).
                sun.transform.rotation = Quaternion.Euler(sunAngle, sunYaw, 0f);
                float dayProgress = Mathf.InverseLerp(6f, 18f, hour);
                Color c = sunColor != null ? sunColor.Evaluate(dayProgress) : Color.white;
                sun.color = Color.Lerp(moonColor, c, day);
                sun.intensity = Mathf.Lerp(moonIntensity, sunMaxIntensity, day);
            }
            else
            {
                // LUNA: la misma luz desde el lado opuesto, para que de noche ilumine desde arriba.
                sun.transform.rotation = Quaternion.Euler(sunAngle + 180f, sunYaw, 0f);
                sun.color = moonColor;
                sun.intensity = moonIntensity;
            }

            RenderSettings.ambientLight = Color.Lerp(ambientNight, ambientDay, day);
            RenderSettings.fogColor = Color.Lerp(fogNight, fogDay, day);

            bool manageLamps = useFixedHour || (clock != null && clock.IsRunning);
            if (nightLamps != null && manageLamps)
            {
                bool lampsOn = hour >= lampsOnHour || hour < 6.5f;
                foreach (Light l in nightLamps)
                    if (l != null && l.enabled != lampsOn) l.enabled = lampsOn;
            }
        }

        // ---------- Depuración ----------

        [ContextMenu("Debug/Ver 08:00")] void D8() => SetFixedHour(8);
        [ContextMenu("Debug/Ver 13:00")] void D13() => SetFixedHour(13);
        [ContextMenu("Debug/Ver 19:00")] void D19() => SetFixedHour(19);
        [ContextMenu("Debug/Ver 03:17")] void D3() => SetFixedHour(3.28f);
        [ContextMenu("Debug/Seguir el reloj")] void DF() => FollowClock();
    }
}
