using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tape0317.UI
{
    public class VHSMenuItem : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Partes")]
        [Tooltip("La franja que marca la opción elegida.")]
        [SerializeField] Graphic highlightBar;
        [SerializeField] TMP_Text label;
        [Tooltip("Opcional: lo que parpadea al lado de la opción marcada. Puede ser un Image " +
                 "(ej. el sprite '>>' del pack PSX) o un texto TMP con '>'.")]
        [SerializeField] Graphic cursor;

        [Header("Colores del texto")]
        [SerializeField] Color normalTextColor = Color.white;
        [Tooltip("Color del texto cuando está marcada (normalmente el color del fondo del menú).")]
        [SerializeField] Color highlightedTextColor = new Color32(0x0D, 0x1F, 0xB0, 0xFF);

        [Header("Cursor")]
        [SerializeField, Min(0.05f)] float cursorBlinkSeconds = 0.4f;

        [Header("Sonido (opcional)")]
        [Tooltip("AudioSource 2D con el 'tic' de menú.")]
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip hoverClip;

        bool highlighted;
        float blinkTimer;

        void Reset()
        {
            label = GetComponentInChildren<TMP_Text>();
        }

        void OnEnable() => SetHighlighted(false, playSound: false);

        void OnDisable() => SetHighlighted(false, playSound: false);

        public void OnPointerEnter(PointerEventData eventData) => SetHighlighted(true, playSound: true);
        public void OnPointerExit(PointerEventData eventData) => SetHighlighted(false, playSound: false);
        public void OnSelect(BaseEventData eventData) => SetHighlighted(true, playSound: true);
        public void OnDeselect(BaseEventData eventData) => SetHighlighted(false, playSound: false);

        void SetHighlighted(bool on, bool playSound)
        {
            if (on && highlighted)
                return;

            highlighted = on;
            blinkTimer = 0f;

            if (highlightBar != null) highlightBar.enabled = on;
            if (label != null) label.color = on ? highlightedTextColor : normalTextColor;
            if (cursor != null) cursor.enabled = on;

            if (on && playSound && sfx != null && hoverClip != null)
                sfx.PlayOneShot(hoverClip);
        }

        void Update()
        {
            if (!highlighted || cursor == null)
                return;
 
            blinkTimer += Time.unscaledDeltaTime;
            if (blinkTimer >= cursorBlinkSeconds)
            {
                blinkTimer = 0f;
                cursor.enabled = !cursor.enabled;
            }
        }
    }
}
