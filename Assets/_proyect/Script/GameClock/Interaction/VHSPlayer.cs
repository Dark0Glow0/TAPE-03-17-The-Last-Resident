using Tape0317.Core;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Tape0317.Interaction
{
    public class VHSPlayer : MonoBehaviour
    {
        [SerializeField] XRSocketInteractor tapeSlot;
        [SerializeField] DualExperienceManager experience;

        [Header("Menú en la tele")]
        [Tooltip("Canvas del menú del reproductor (sobre la pantalla de la tele). Empieza desactivado.")]
        [SerializeField] GameObject playerMenu;
        [Tooltip("Lo que se muestra cuando HAY cinta (nombre + botón REPRODUCIR).")]
        [SerializeField] GameObject tapePanel;
        [Tooltip("Lo que se muestra cuando NO hay cinta (ej. 'INSERTE UNA CINTA').")]
        [SerializeField] GameObject noTapePanel;
        [Tooltip("Opcional: texto donde se escribe el nombre de la cinta.")]
        [SerializeField] TMP_Text tapeLabel;
        [SerializeField] string tapeName = "CINTA 01";

        [Header("Sonido (opcional)")]
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip insertClip;
        [Tooltip("El 'clac' + zumbido de la videocasetera al encenderse.")]
        [SerializeField] AudioClip powerOnClip;
        [SerializeField] AudioClip powerOffClip;
        [Tooltip("El mecanismo al empezar a reproducir.")]
        [SerializeField] AudioClip playClip;

        public bool IsOn { get; private set; }
        bool HasTape => tapeSlot != null && tapeSlot.hasSelection;

        void Reset() => experience = FindFirstObjectByType<DualExperienceManager>();

        void Awake()
        {
            if (playerMenu != null)
                playerMenu.SetActive(false);
        }

        void OnEnable()
        {
            if (tapeSlot != null)
            {
                tapeSlot.selectEntered.AddListener(OnTapeInserted);
                tapeSlot.selectExited.AddListener(OnTapeRemoved);
            }
        }

        void OnDisable()
        {
            if (tapeSlot != null)
            {
                tapeSlot.selectEntered.RemoveListener(OnTapeInserted);
                tapeSlot.selectExited.RemoveListener(OnTapeRemoved);
            }
        }

        // ---------- Ranura ----------

        void OnTapeInserted(SelectEnterEventArgs args)
        {
            PlaySfx(insertClip);
            RefreshMenu(); // si estaba encendido, cambia de "INSERTE UNA CINTA" a "CINTA 01"
        }

        void OnTapeRemoved(SelectExitEventArgs args) => RefreshMenu();

        // ---------- Botón físico ----------

        public void PressPower()
        {
            IsOn = !IsOn;
            PlaySfx(IsOn ? powerOnClip : powerOffClip);
            RefreshMenu();
        }

        public void PressPlay() => PressPower();

        // ---------- Menú de la tele ----------

        
        public void PlayTape()
        {
            if (!IsOn || !HasTape)
                return;

            PlaySfx(playClip);

            IsOn = false;
            RefreshMenu();

            if (experience != null)
                experience.EnterTape();
        }

        void RefreshMenu()
        {
            if (playerMenu != null)
                playerMenu.SetActive(IsOn);

            if (!IsOn)
                return;

            if (tapePanel != null) tapePanel.SetActive(HasTape);
            if (noTapePanel != null) noTapePanel.SetActive(!HasTape);
            if (tapeLabel != null) tapeLabel.text = tapeName;
        }

        void PlaySfx(AudioClip clip)
        {
            if (sfx != null && clip != null)
                sfx.PlayOneShot(clip);
        }
    }
}
