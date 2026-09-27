using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace SlimeRancher.Area1
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public sealed class Area1HeldToolPhysics : MonoBehaviour
    {
        XRGrabInteractable grab;
        Rigidbody body;
        GameObject[] colliderObjects;
        int[] originalLayers;
        RigidbodyInterpolation originalInterpolation;
        bool applied;
        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>(); body = GetComponent<Rigidbody>();
            var colliders = GetComponentsInChildren<Collider>(true);
            var objects = new System.Collections.Generic.List<GameObject>();
            foreach (var collider in colliders) if (!objects.Contains(collider.gameObject)) objects.Add(collider.gameObject);
            colliderObjects = objects.ToArray(); originalLayers = new int[colliderObjects.Length];
        }
        void OnEnable() { grab.selectEntered.AddListener(Grabbed); grab.selectExited.AddListener(Released); }
        void Grabbed(SelectEnterEventArgs args)
        {
            if (applied) return;
            applied = true; originalInterpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            // Unity's point-and-click simulator uses DefaultRaycastLayers. A held tool
            // must not become its own aim target. XRI interaction layers are unchanged.
            for (int i = 0; i < colliderObjects.Length; i++)
            {
                originalLayers[i] = colliderObjects[i].layer;
                colliderObjects[i].layer = LayerMask.NameToLayer("Ignore Raycast");
            }
        }
        void Released(SelectExitEventArgs args) { if (!grab.isSelected) Restore(); }
        void Restore()
        {
            if (!applied) return;
            applied = false;
            for (int i = 0; i < colliderObjects.Length; i++) if (colliderObjects[i]) colliderObjects[i].layer = originalLayers[i];
            if (body) body.interpolation = originalInterpolation;
        }
        void OnDisable()
        {
            if (grab) { grab.selectEntered.RemoveListener(Grabbed); grab.selectExited.RemoveListener(Released); }
            Restore();
        }
    }
}

