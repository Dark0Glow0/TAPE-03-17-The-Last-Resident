using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tape0317.UI
{
    /// <summary>
    /// Opción de menú estilo tele/videocasetera antigua:
    /// al apuntarla con el rayo (o el mouse) aparece una FRANJA que la marca, los colores se invierten
    /// (fondo claro, letra oscura) y un cursor ">" parpadea al lado. Al dejar de apuntarla, vuelve a la normalidad.
    ///
    /// Montaje en cada botón (Boton_Play, Boton_Reiniciar, Boton_Salir):
    ///   Boton_X                 ← Button + este script
    ///    ├── Franja             ← Image estirada (color claro o sprite del pack PSX). Raycast Target ❌
    ///    └── Texto              ← TMP con el texto de la opción (ej. "REINTENTAR")
    /// El "Cursor" es opcional: un Image con el sprite ">>" (o un TMP con ">") a la izquierda.
    /// La franja también es opcional: sin ella, la marca es el cursor + el texto que se ilumina.
    /// </summary>
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

        // Si el menú se apaga mientras el rayo apunta a la opción, no llega el "Exit":
        // dejamos todo apagado para que no aparezca marcada la próxima vez.
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

            // unscaledDeltaTime: el menú funciona aunque el juego esté "pausado" con timeScale.
            blinkTimer += Time.unscaledDeltaTime;
            if (blinkTimer >= cursorBlinkSeconds)
            {
                blinkTimer = 0f;
                cursor.enabled = !cursor.enabled;
            }
        }
    }
}
