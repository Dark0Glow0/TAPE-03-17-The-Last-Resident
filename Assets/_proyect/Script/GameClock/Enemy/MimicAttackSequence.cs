using System.Collections;
using System.Collections.Generic;
using Tape0317.Core;
using UnityEngine;

namespace Tape0317.Enemy
{
    public class MimicAttackSequence : MonoBehaviour
    {
        [SerializeField] NightDirector director;

        [Header("Mimic")]
        [Tooltip("El modelo del Mimic (con el pivote en los pies). Empieza desactivado.")]
        [SerializeField] GameObject mimic;
        [Tooltip("Dónde aparece: la puerta de entrada, una ventana...")]
        [SerializeField] Transform entryPoint;
        [SerializeField, Min(0.1f)] float moveSpeed = 1.2f;
        [Tooltip("Distancia (m) a la que te atrapa.")]
        [SerializeField, Min(0.1f)] float catchDistance = 0.9f;
        [Tooltip("Altura de los ojos del Mimic: se usa para que su cara quede frente a la tuya.")]
        [SerializeField] float mimicEyeHeight = 1.6f;
        [Tooltip("Si el jugador huye, igual lo atrapa tras este tiempo (teletransporte final).")]
        [SerializeField, Min(1f)] float maxChaseSeconds = 20f;

        [Header("Luces de la casa")]
        [SerializeField] List<Light> houseLights = new List<Light>();
        [SerializeField, Min(0f)] float flickerSeconds = 6f;
        [SerializeField, Min(0f)] float darknessSeconds = 2.5f;

        [Header("Sonido (AudioSources 3D repartidos por la casa)")]
        [Tooltip("Fuentes de sonido en distintos puntos de la casa. Spatial Blend = 1 (3D).")]
        [SerializeField] List<AudioSource> noiseSources = new List<AudioSource>();
        [Tooltip("Golpes, pasos, susurros, voces...")]
        [SerializeField] List<AudioClip> noiseClips = new List<AudioClip>();
        [Tooltip("AudioSource en el propio Mimic (3D, en loop): respiración, murmullo...")]
        [SerializeField] AudioSource mimicLoop;
        [SerializeField] AudioClip entryClip;
        [SerializeField] AudioClip screamClip;
        [SerializeField, Min(0.1f)] float jumpscareSeconds = 0.8f;

        Transform playerHead;
        bool running;

        void Reset() => director = FindFirstObjectByType<NightDirector>();

        void Awake()
        {
            if (mimic != null)
                mimic.SetActive(false);
        }

        void OnEnable()
        {
            if (director != null)
                director.OnPhaseChanged += HandlePhaseChanged;
        }

        void OnDisable()
        {
            if (director != null)
                director.OnPhaseChanged -= HandlePhaseChanged;
        }

        void HandlePhaseChanged(NightPhase phase)
        {
            if (phase == NightPhase.Attack && !running)
                StartCoroutine(RunAttack());
        }

        IEnumerator RunAttack()
        {
            running = true;

            playerHead = Camera.main != null ? Camera.main.transform : null;

            // ---- 1. Parpadeo de luces + ruidos ----
            float t = 0f;
            float nextNoise = 0f;
            while (t < flickerSeconds)
            {
                foreach (Light l in houseLights)
                    if (l != null) l.enabled = Random.value > 0.35f;

                if (t >= nextNoise)
                {
                    PlayRandomNoise();
                    nextNoise = t + Random.Range(0.6f, 1.5f);
                }

                float step = Random.Range(0.05f, 0.2f);
                yield return new WaitForSeconds(step);
                t += step;
            }

            // ---- 2. Apagón y silencio ----
            SetLights(false);
            yield return new WaitForSeconds(darknessSeconds);

            // Si falta algo en el Inspector, al menos el juego termina en vez de quedarse colgado.
            if (mimic == null || playerHead == null)
            {
                Debug.LogWarning("[MimicAttackSequence] Falta el Mimic o la cámara: se salta al final.");
                director.KillPlayer();
                running = false;
                yield break;
            }

            // ---- 3. El Mimic aparece en la entrada ----
            if (entryPoint != null)
                mimic.transform.SetPositionAndRotation(entryPoint.position, entryPoint.rotation);
            mimic.SetActive(true);

            if (entryClip != null)
                AudioSource.PlayClipAtPoint(entryClip, mimic.transform.position);
            if (mimicLoop != null)
            {
                mimicLoop.loop = true;
                mimicLoop.Play();
            }

            // ---- 4. Avanza hacia el jugador ----
            float chase = 0f;
            while (chase < maxChaseSeconds)
            {
                // Apuntamos a "los pies" que tendría alguien con los ojos a la altura del jugador.
                Vector3 target = playerHead.position + Vector3.down * mimicEyeHeight;
                Vector3 toTarget = target - mimic.transform.position;

                if (toTarget.magnitude <= catchDistance)
                    break;

                mimic.transform.position += toTarget.normalized * (moveSpeed * Time.deltaTime);

                // Que mire al jugador, pero sin inclinarse hacia arriba o abajo.
                Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
                if (flat.sqrMagnitude > 0.001f)
                    mimic.transform.rotation = Quaternion.LookRotation(flat, Vector3.up);

                chase += Time.deltaTime;
                yield return null;
            }

            // ---- 5. Susto: frente a la cara del jugador ----
            Vector3 forward = playerHead.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            mimic.transform.position = playerHead.position + forward * 0.6f + Vector3.down * mimicEyeHeight;
            mimic.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);

            if (screamClip != null)
                AudioSource.PlayClipAtPoint(screamClip, playerHead.position);

            yield return new WaitForSeconds(jumpscareSeconds);

            if (mimicLoop != null)
                mimicLoop.Stop();

            director.KillPlayer();
            running = false;
        }

        void PlayRandomNoise()
        {
            if (noiseSources.Count == 0 || noiseClips.Count == 0)
                return;

            AudioSource source = noiseSources[Random.Range(0, noiseSources.Count)];
            AudioClip clip = noiseClips[Random.Range(0, noiseClips.Count)];
            if (source != null && clip != null)
                source.PlayOneShot(clip);
        }

        void SetLights(bool on)
        {
            foreach (Light l in houseLights)
                if (l != null) l.enabled = on;
        }

        // ---------- Depuración ----------

        [ContextMenu("Debug/Lanzar ataque ahora")]
        void DebugRunAttack()
        {
            if (!running)
                StartCoroutine(RunAttack());
        }
    }
}
