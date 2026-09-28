using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // Slimes, chickens and plorts always end up standing: once they leave the hand (thrown, dropped,
    // shot out of the vacuum or knocked over) they stop tumbling and smoothly turn back upright,
    // keeping the direction they face. Carrots are left free to lie on their side.
    // Added to every RanchItem by RanchItem.Awake.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Area1KeepUpright : MonoBehaviour
    {
        [Tooltip("Qué tan rápido se vuelve a poner de pie.")]
        public float uprightSpeed = 8;

        Rigidbody body;
        RanchItem item;
        XRGrabInteractable grab;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            item = GetComponent<RanchItem>();
            grab = GetComponent<XRGrabInteractable>();
        }

        void FixedUpdate()
        {
            if (body.isKinematic || (grab && grab.isSelected)) return;
            if (item && item.data && item.data.kind == RanchItemKind.Carrot) return;

            // No tumbling: only spinning around the vertical axis is kept.
            var spin = body.angularVelocity;
            body.angularVelocity = new Vector3(0, spin.y, 0);

            var up = body.rotation * Vector3.up;
            if (Vector3.Angle(up, Vector3.up) < .5f) return;
            var forward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < .001f) forward = Vector3.ProjectOnPlane(-up, Vector3.up); // lying on its face
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            var upright = Quaternion.LookRotation(forward, Vector3.up);
            body.MoveRotation(Quaternion.Slerp(body.rotation, upright, 1 - Mathf.Exp(-uprightSpeed * Time.fixedDeltaTime)));
        }
    }
}
