using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using SlimeRancherVR;
namespace SlimeRancher.Area1
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class Area1GrabbableItem : MonoBehaviour
    {
        XRGrabInteractable grab;
        PinkSlime slime;
        RanchItem item;
        bool slimeEnabled, itemEnabled;
        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            slime = GetComponent<PinkSlime>(); item = GetComponent<RanchItem>();
            grab.selectEntered.AddListener(Hold); grab.selectExited.AddListener(Release);
        }
        void Hold(SelectEnterEventArgs args)
        {
            if (slime) { slimeEnabled = slime.enabled; slime.enabled = false; }
            if (item) { itemEnabled = item.enabled; item.enabled = false; }
        }
        void Release(SelectExitEventArgs args)
        {
            if (slime) slime.enabled = slimeEnabled;
            if (item) { item.enabled = itemEnabled; item.graceUntil = Time.time + .5f; }
        }
        void OnDestroy()
        {
            if (grab) { grab.selectEntered.RemoveListener(Hold); grab.selectExited.RemoveListener(Release); }
        }
    }
}
