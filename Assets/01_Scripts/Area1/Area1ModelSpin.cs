using UnityEngine;

namespace SlimeRancher.Area1
{
    // Showcase models on the boards: always facing the player (the board's -Z side),
    // with a gentle side-to-side sway and float instead of a full turn.
    public sealed class Area1ModelSpin : MonoBehaviour
    {
        public const float FacingYaw = 180;
        public float swayDegrees = 20;
        public float swaySpeed = .9f;
        public float bobHeight = .012f;
        public float bobSpeed = 1.6f;
        Vector3 start;
        float phase;

        void Awake()
        {
            start = transform.localPosition;
            phase = Random.value * Mathf.PI * 2;
        }

        void Update()
        {
            float time = Time.time + phase;
            transform.localRotation = Quaternion.Euler(0, FacingYaw + Mathf.Sin(time * swaySpeed) * swayDegrees, 0);
            transform.localPosition = start + Vector3.up * (Mathf.Sin(time * bobSpeed) * bobHeight);
        }
    }
}
