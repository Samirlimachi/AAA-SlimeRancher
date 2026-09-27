using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // Heart bought at the food shop: it can be grabbed, vacuumed and shot like any item.
    // Walking over it while it lies on the ground heals `healAmount` (not a full refill).
    // With full health it stays on the ground for later.
    [RequireComponent(typeof(RanchItem))]
    public sealed class Area1HealthPickup : MonoBehaviour
    {
        [Tooltip("Vida que recupera cada corazón.")]
        public float healAmount = 15;
        [Tooltip("Distancia horizontal a la que se recoge al pasar.")]
        public float pickupRadius = .6f;

        RanchItem item;
        XRGrabInteractable grab;
        float nextFullMessage;
        static AudioClip chime;

        void Awake()
        {
            item = GetComponent<RanchItem>();
            grab = GetComponent<XRGrabInteractable>();
        }

        void Update()
        {
            var game = RanchGame.Instance;
            var camera = Camera.main;
            // Not while held, flying out of the vacuum or being sucked in (gravity off).
            if (!game || !camera || item.Consumed || !item.enabled || (grab && grab.isSelected) || Time.time < item.graceUntil || !item.Body.useGravity) return;
            // Player standing on it: head above it (horizontally close) and the heart near the floor under them.
            var head = camera.transform.position;
            var flat = transform.position - head;
            float height = head.y - transform.position.y;
            flat.y = 0;
            if (flat.magnitude > pickupRadius || height < .8f || height > 2.2f) return;
            if (game.health >= game.maxHealth - .01f)
            {
                if (Time.time >= nextFullMessage) { game.Notify("Tu vida ya está llena: guarda el corazón para después."); nextFullMessage = Time.time + 3; }
                return;
            }
            float healed = Mathf.Min(healAmount, game.maxHealth - game.health);
            var position = transform.position;
            if (!item.Consume()) return;
            game.health += healed;
            game.Notify("+" + Mathf.CeilToInt(healed) + " de vida");
            Area1Effects.Heal(position);
            if (!chime) chime = Chime();
            AudioSource.PlayClipAtPoint(chime, head, .6f);
        }

        // Two soft rising notes.
        static AudioClip Chime()
        {
            const int rate = 44100;
            float[] notes = { 659.25f, 987.77f };
            int length = (int)(rate * .5f);
            var data = new float[length];
            for (int n = 0; n < notes.Length; n++)
            {
                int start = (int)(rate * .09f * n);
                for (int i = 0; start + i < length; i++)
                {
                    float time = (float)i / rate;
                    data[start + i] += Mathf.Sin(2 * Mathf.PI * notes[n] * time) * Mathf.Exp(-time * 7) * .3f;
                }
            }
            var clip = AudioClip.Create("Curar", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
