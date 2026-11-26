using System.Collections;
using UnityEngine;

namespace Tape0317.Utils
{
    /// <summary>
    /// Hace parpadear una o varias luces durante unos segundos y las deja como estaban.
    /// Parpadea cambiando la INTENSIDAD (no apagando la luz): así no pelea con el DayNightCycle,
    /// que enciende y apaga las lámparas según la hora.
    /// Reutilizable: Revelation, sustos, cuando el Mimic está cerca...
    /// Conectar desde cualquier evento a LightFlicker → Flicker().
    /// </summary>
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField] Light[] lights;
        [SerializeField, Min(0.1f)] float seconds = 2.5f;
        [Tooltip("Probabilidad (0-1) de que la luz esté encendida en cada parpadeo. Más bajo = más oscuro.")]
        [SerializeField, Range(0f, 1f)] float onChance = 0.55f;
        [Tooltip("Zumbido eléctrico opcional (AudioSource 3D cerca de la lámpara).")]
        [SerializeField] AudioSource buzz;

        public void Flicker()
        {
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            // Guardamos la intensidad original para dejarlas igual al terminar.
            var original = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                original[i] = lights[i] != null ? lights[i].intensity : 0f;

            if (buzz != null) buzz.Play();

            float t = 0f;
            while (t < seconds)
            {
                for (int i = 0; i < lights.Length; i++)
                    if (lights[i] != null)
                        lights[i].intensity = Random.value < onChance ? original[i] : 0f;

                float step = Random.Range(0.04f, 0.18f);
                yield return new WaitForSeconds(step);
                t += step;
            }

            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].intensity = original[i];

            if (buzz != null) buzz.Stop();
        }
    }
}
