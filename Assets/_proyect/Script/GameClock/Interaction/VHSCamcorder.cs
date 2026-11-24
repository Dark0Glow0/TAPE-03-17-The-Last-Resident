using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Tape0317.Interaction
{
    /// <summary>
    /// Cámara VHS de mano (estilo MADiSON):
    ///  - Al AGARRARLA (grip) se enciende y la pantallita muestra lo que ve el lente.
    ///  - Con el GATILLO (trigger) empieza/detiene la grabación ("REC" rojo parpadeando).
    ///  - La pantalla tiene look VHS: grano animado, fecha/hora de los 90 y "tirones" de tracking.
    ///  - ExtraGlitch (0..1) lo pueden subir otros scripts cerca de algo paranormal.
    ///
    /// Cómo funciona por dentro: una segunda Camera (el "lente") renderiza a una RenderTexture
    /// de baja resolución, y un RawImage en la pantallita muestra esa textura.
    ///
    /// RENDIMIENTO EN QUEST: una cámara extra es cara. Por eso: resolución baja (320x240, que
    /// además se ve más VHS), solo renderiza cuando está encendida, y cada N frames.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class VHSCamcorder : MonoBehaviour
    {
        [Header("Lente")]
        [Tooltip("Camera hija en la parte frontal del modelo. SIN AudioListener y SIN tag MainCamera.")]
        [SerializeField] Camera lensCamera;
        [SerializeField] Vector2Int resolution = new Vector2Int(320, 240);
        [Tooltip("1 = cada frame. 2 = la mitad de frames (36 fps en Quest a 72 Hz): más barato y más 'video viejo'.")]
        [SerializeField, Range(1, 4)] int renderEveryNFrames = 2;

        [Header("Pantallita (Canvas World Space sobre la pantalla del modelo)")]
        [Tooltip("Objeto con todo lo de la pantalla; se oculta cuando la cámara está apagada.")]
        [SerializeField] GameObject screenRoot;
        [SerializeField] RawImage feed;
        [Tooltip("RawImage encima del feed para el grano. Puede quedar vacío.")]
        [SerializeField] RawImage noiseOverlay;
        [SerializeField] TMP_Text recLabel;
        [SerializeField] TMP_Text timestampLabel;

        [Header("Fecha y hora que muestra la cinta")]
        [SerializeField] string recordingDate = "MAR. 13 1998";
        [SerializeField, Range(0, 23)] int startHour = 19;
        [SerializeField, Range(0, 59)] int startMinute = 2;

        [Header("Efecto VHS")]
        [SerializeField, Range(0f, 1f)] float noiseOpacity = 0.12f;
        [Tooltip("Probabilidad por segundo de un 'tirón' de tracking.")]
        [SerializeField, Range(0f, 2f)] float glitchChancePerSecond = 0.15f;
        [Tooltip("Cuánto se desplaza la imagen en un tirón (fracción del ancho).")]
        [SerializeField, Range(0f, 0.2f)] float glitchMaxOffset = 0.03f;
        [SerializeField, Min(0.1f)] float recBlinkSeconds = 0.5f;

        [Header("Sonido (opcional)")]
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip powerOnClip;
        [SerializeField] AudioClip recStartClip;
        [SerializeField] AudioClip recStopClip;

        public bool IsOn { get; private set; }
        public bool IsRecording { get; private set; }

        /// <summary>0..1: más interferencia. Lo puede subir el Mimic o un trigger cerca del ritual.</summary>
        public float ExtraGlitch { get; set; }

        XRGrabInteractable grab;
        RenderTexture renderTexture;
        Texture2D noiseTexture;

        float onSeconds;        // tiempo encendida (para el reloj de la pantalla)
        int lastShownSecond = -1;
        float blinkTimer;
        bool blinkVisible = true;
        float glitchTimer;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();

            // RenderTexture creada por código: así la resolución se cambia desde el Inspector
            // y no hay que crear el asset a mano.
            renderTexture = new RenderTexture(resolution.x, resolution.y, 16) { name = "VHSCamcorder_RT" };
            renderTexture.Create();

            if (lensCamera != null)
            {
                lensCamera.targetTexture = renderTexture;
                lensCamera.enabled = false; // LateUpdate decide cuándo renderiza
            }

            if (feed != null)
                feed.texture = renderTexture;

            if (noiseOverlay != null)
            {
                noiseTexture = BuildNoiseTexture(64);
                noiseOverlay.texture = noiseTexture;
                noiseOverlay.color = new Color(1f, 1f, 1f, noiseOpacity);
                noiseOverlay.raycastTarget = false;
            }

            SetPower(false, playSound: false);
        }

        void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
            grab.activated.AddListener(OnTriggerPressed);
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
            grab.activated.RemoveListener(OnTriggerPressed);

            // Si el mundo del pasado se desactiva con la cámara en la mano, que quede apagada.
            if (IsOn)
                SetPower(false, playSound: false);
        }

        void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (noiseTexture != null)
                Destroy(noiseTexture);
        }

        // ---------- Interacción XR ----------

        void OnGrabbed(SelectEnterEventArgs args) => SetPower(true, playSound: true);

        void OnReleased(SelectExitEventArgs args)
        {
            // Si la sujetaba con las dos manos y suelta una, sigue encendida.
            if (!grab.isSelected)
                SetPower(false, playSound: false);
        }

        void OnTriggerPressed(ActivateEventArgs args) => ToggleRecording();

        public void ToggleRecording()
        {
            if (!IsOn)
                return;

            IsRecording = !IsRecording;
            PlaySfx(IsRecording ? recStartClip : recStopClip);
            RefreshRecLabel();
        }

        void SetPower(bool on, bool playSound)
        {
            IsOn = on;
            if (!on)
                IsRecording = false;

            if (screenRoot != null)
                screenRoot.SetActive(on);
            if (lensCamera != null && !on)
                lensCamera.enabled = false;

            if (on && playSound)
                PlaySfx(powerOnClip);

            lastShownSecond = -1; // fuerza a redibujar la hora
            RefreshRecLabel();
        }

        // ---------- Actualización de la pantalla ----------

        void Update()
        {
            if (!IsOn)
                return;

            onSeconds += Time.deltaTime;
            UpdateTimestamp();
            UpdateRecBlink();
            UpdateNoise();
            UpdateGlitch();
        }

        void LateUpdate()
        {
            // Se ejecuta antes del render del frame: aquí decidimos si el lente dibuja este frame.
            if (lensCamera != null)
                lensCamera.enabled = IsOn && Time.frameCount % renderEveryNFrames == 0;
        }

        void UpdateTimestamp()
        {
            int second = Mathf.FloorToInt(onSeconds);
            if (timestampLabel == null || second == lastShownSecond)
                return; // solo creamos el string una vez por segundo (evita basura para el GC)

            lastShownSecond = second;
            int total = startHour * 3600 + startMinute * 60 + second;
            int h = (total / 3600) % 24;
            int m = (total / 60) % 60;
            int s = total % 60;

            // Formato de cámara de los 90: AM/PM y fecha abajo.
            string ampm = h < 12 ? "AM" : "PM";
            int h12 = h % 12 == 0 ? 12 : h % 12;
            timestampLabel.text = $"{ampm} {h12}:{m:00}:{s:00}\n{recordingDate}";
        }

        void UpdateRecBlink()
        {
            if (!IsRecording || recLabel == null)
                return;

            blinkTimer += Time.deltaTime;
            if (blinkTimer >= recBlinkSeconds)
            {
                blinkTimer = 0f;
                blinkVisible = !blinkVisible;
                recLabel.enabled = blinkVisible;
            }
        }

        void RefreshRecLabel()
        {
            if (recLabel == null)
                return;

            // Sin símbolos raros: la fuente por defecto de TMP no trae "●" y saldría un cuadrado.
            // Para el punto rojo, pon una Image circular roja al lado del texto.
            recLabel.text = IsRecording ? "REC" : "STBY";
            recLabel.color = IsRecording ? new Color(1f, 0.15f, 0.15f) : Color.white;
            recLabel.enabled = true;
            blinkVisible = true;
            blinkTimer = 0f;
        }

        void UpdateNoise()
        {
            if (noiseOverlay == null)
                return;

            // Movemos la textura de ruido a un lugar aleatorio cada frame: grano "vivo" gratis.
            // El 5 y el 4 repiten la textura para que el grano sea fino.
            noiseOverlay.uvRect = new Rect(Random.value, Random.value, 5f, 4f);
        }

        void UpdateGlitch()
        {
            if (feed == null)
                return;

            if (glitchTimer > 0f)
            {
                glitchTimer -= Time.deltaTime;
                float offset = Random.Range(-glitchMaxOffset, glitchMaxOffset) * (1f + ExtraGlitch * 3f);
                feed.uvRect = new Rect(offset, 0f, 1f, 1f);
                return;
            }

            feed.uvRect = new Rect(0f, 0f, 1f, 1f);

            float chance = (glitchChancePerSecond + ExtraGlitch * 3f) * Time.deltaTime;
            if (Random.value < chance)
                glitchTimer = Random.Range(0.05f, 0.25f);
        }

        // ---------- Utilidades ----------

        static Texture2D BuildNoiseTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "VHS_Noise",
                filterMode = FilterMode.Point,  // píxeles duros, como el grano de una cinta
                wrapMode = TextureWrapMode.Repeat
            };

            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)Random.Range(0, 256);
                pixels[i] = new Color32(v, v, v, 255);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        void PlaySfx(AudioClip clip)
        {
            if (sfx != null && clip != null)
                sfx.PlayOneShot(clip);
        }
    }
}
