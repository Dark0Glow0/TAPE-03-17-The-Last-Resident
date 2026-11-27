using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tape0317.UI
{
    public class TVStatic : MonoBehaviour
    {
        [SerializeField] RawImage staticImage;
        [Tooltip("Qué tan fino es el grano (repeticiones de la textura en pantalla).")]
        [SerializeField] Vector2 grainTiling = new Vector2(6f, 4f);

        [Header("Texto que parpadea (opcional)")]
        [SerializeField] TMP_Text blinkLabel;
        [SerializeField, Min(0.1f)] float blinkSeconds = 0.5f;

        [Header("Sonido (opcional)")]
        [Tooltip("AudioSource 3D en la tele con un clip de estática en loop.")]
        [SerializeField] AudioSource staticSound;

        Texture2D noise;
        float blinkTimer;

        void Awake()
        {
            noise = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = "TV_Static",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            var pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)Random.Range(0, 256);
                pixels[i] = new Color32(v, v, v, 255);
            }
            noise.SetPixels32(pixels);
            noise.Apply();

            if (staticImage != null)
            {
                staticImage.texture = noise;
                staticImage.raycastTarget = false;
            }
        }

        void OnEnable()
        {
            if (staticSound != null) staticSound.Play();
        }

        void OnDisable()
        {
            if (staticSound != null) staticSound.Stop();
        }

        void OnDestroy()
        {
            if (noise != null) Destroy(noise);
        }

        void Update()
        {
            if (staticImage != null)
                staticImage.uvRect = new Rect(Random.value, Random.value, grainTiling.x, grainTiling.y);

            if (blinkLabel != null)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= blinkSeconds)
                {
                    blinkTimer = 0f;
                    blinkLabel.enabled = !blinkLabel.enabled;
                }
            }
        }
    }
}
