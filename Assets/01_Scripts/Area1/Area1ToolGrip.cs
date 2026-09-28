using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using SlimeRancherVR;
namespace SlimeRancher.Area1
{
    // Resolve the equipped tool from the tracked hand, never from a movable ray endpoint.
    public sealed class Area1ToolGrip : XRBaseGrabTransformer
    {
        VacuumPickup pickup;
        Vector3 gripPosition;
        Quaternion gripRotation;
        public override void OnGrab(XRGrabInteractable grab)
        {
            pickup = GetComponent<VacuumPickup>();
            var attach = grab.attachTransform;
            gripPosition = attach ? grab.transform.InverseTransformPoint(attach.position) : Vector3.zero;
            gripRotation = attach ? Quaternion.Inverse(grab.transform.rotation) * attach.rotation : Quaternion.identity;
        }
        public override void Process(XRGrabInteractable grab, XRInteractionUpdateOrder.UpdatePhase phase, ref Pose pose, ref Vector3 scale)
        {
            if (!pickup || grab.interactorsSelecting.Count == 0) return;
            var selector = grab.interactorsSelecting[0].transform;
            Transform hand = pickup.leftHand && selector.IsChildOf(pickup.leftHand) ? pickup.leftHand : pickup.rightHand;
            if (!hand) return;
            pose.rotation = hand.rotation * Quaternion.Inverse(gripRotation);
            pose.position = hand.position - pose.rotation * Vector3.Scale(gripPosition, grab.transform.lossyScale);
        }
    }
}
