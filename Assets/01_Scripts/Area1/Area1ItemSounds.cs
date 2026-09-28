using UnityEngine;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // Impact sound for plorts hitting the ground (thrown, dropped or knocked around).
    // Added to every RanchItem by RanchItem.Awake; only plorts make a sound.
    [DisallowMultipleComponent]
    public sealed class Area1ItemSounds : MonoBehaviour
    {
        RanchItem item;
        float nextSound;

        void Awake() => item = GetComponent<RanchItem>();

        void OnCollisionEnter(Collision collision)
        {
            if (!item || !item.data || item.data.kind != RanchItemKind.PinkPlort || Time.time < nextSound) return;
            if (collision.relativeVelocity.magnitude < 1.2f || collision.contactCount == 0) return;
            if (collision.GetContact(0).normal.y < .5f) return; // hits the ground, not a wall
            nextSound = Time.time + .25f;
            float strength = Mathf.InverseLerp(1.2f, 6, collision.relativeVelocity.magnitude);
            Area1Audio.Play(b => b.plortCaeSuelo, transform.position, Mathf.Lerp(.5f, 1, strength));
        }
    }
}
