using Tape0317.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Tape0317.Interaction
{
    /// <summary>
    /// Reproductor VHS de la casa: una ranura (socket) donde se mete la cinta y un botón de play.
    /// Al apretar play con una cinta dentro → entramos a la cinta (DualExperienceManager).
    ///
    /// Montaje:
    ///  - Ranura: XRSocketInteractor (o el XRLockSocketInteractor de los ejemplos, que hereda de él
    ///    y permite aceptar SOLO cintas con el sistema de Key/Keychain).
    ///  - Botón: el PushButton de los ejemplos → en su evento On Press, llamar a VHSPlayer.PressPlay.
    /// </summary>
    public class VHSPlayer : MonoBehaviour
    {
        [SerializeField] XRSocketInteractor tapeSlot;
        [SerializeField] DualExperienceManager experience;

        [Header("Sonido (opcional)")]
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip insertClip;
        [SerializeField] AudioClip playClip;
        [Tooltip("Clic seco cuando apretas play sin cinta.")]
        [SerializeField] AudioClip noTapeClip;

        void Reset() => experience = FindFirstObjectByType<DualExperienceManager>();

        void OnEnable()
        {
            if (tapeSlot != null)
                tapeSlot.selectEntered.AddListener(OnTapeInserted);
        }

        void OnDisable()
        {
            if (tapeSlot != null)
                tapeSlot.selectEntered.RemoveListener(OnTapeInserted);
        }

        void OnTapeInserted(SelectEnterEventArgs args) => PlaySfx(insertClip);

        /// <summary>Conectar al On Press del botón.</summary>
        public void PressPlay()
        {
            bool hasTape = tapeSlot != null && tapeSlot.hasSelection;
            if (!hasTape)
            {
                PlaySfx(noTapeClip);
                return;
            }

            PlaySfx(playClip);
            if (experience != null)
                experience.EnterTape();
        }

        void PlaySfx(AudioClip clip)
        {
            if (sfx != null && clip != null)
                sfx.PlayOneShot(clip);
        }
    }
}
