using UnityEngine;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    [DefaultExecutionOrder(12500)]
    [RequireComponent(typeof(SlimeVacuum))]
    public sealed class Area1WaterVacuum : MonoBehaviour
    {
        public int capacity = 30;
        int amount { get => RanchGame.Instance ? RanchGame.Instance.water : 0; set { if (RanchGame.Instance) RanchGame.Instance.water = value; } }
        public Material waterMaterial;
        public float range = 6, unitsPerSecond = 6;
        public int Amount => amount;
        public bool WaterSelected { get; private set; }
        public bool IsFilling { get; private set; }
        public bool HasSource { get; private set; }
        SlimeVacuum vacuum;
        LineRenderer stream;
        Vector3 sourcePoint;
        float fraction, nextShot;
        bool requested;
        void Awake()
        {
            vacuum = GetComponent<SlimeVacuum>();
            var go = new GameObject("Chorro de agua"); go.transform.SetParent(transform, false);
            stream = go.AddComponent<LineRenderer>(); stream.sharedMaterial = waterMaterial;
            stream.positionCount = 2; stream.startWidth = .045f; stream.endWidth = .12f;
            stream.numCapVertices = 4; stream.enabled = false;
        }
        public void SelectWater(bool selected) { WaterSelected = selected; }
        bool Owned(Transform t) => t.IsChildOf(transform) || (vacuum.playerRoot && t.IsChildOf(vacuum.playerRoot));
        bool ClearPath(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            foreach (var hit in Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!Owned(hit.transform)) return false;
            return true;
        }
        public bool TryFindSource(out Vector3 point)
        {
            point = default;
            if (!vacuum || !vacuum.muzzle) return false;
            if (Camera.main && !ClearPath(Camera.main.transform.position, vacuum.muzzle.position)) return false;
            float nearest = range + 1;
            Area1WaterSource source = null;
            foreach (var hit in Physics.RaycastAll(vacuum.muzzle.position, vacuum.muzzle.forward, range, ~0, QueryTriggerInteraction.Collide))
            {
                if (Owned(hit.transform)) continue;
                var candidate = hit.collider.GetComponent<Area1WaterSource>();
                if (hit.collider.isTrigger && !candidate) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance; source = candidate; point = hit.point;
            }
            return source != null;
        }
        // Return true even with a full tank so a pond never vacuums objects behind it.
        public bool SetSuction(bool active)
        {
            requested = active && vacuum && vacuum.CanUse;
            HasSource = requested && TryFindSource(out sourcePoint);
            IsFilling = HasSource && amount < capacity;
            if (!IsFilling) fraction = 0;
            return HasSource;
        }
        void Update()
        {
            SetSuction(requested);
            if (!IsFilling) return;
            fraction += Time.deltaTime * unitsPerSecond;
            while (fraction >= 1 && amount < capacity) { fraction--; amount++; }
        }
        void LateUpdate()
        {
            stream.enabled = IsFilling;
            if (stream.enabled) { stream.SetPosition(0, vacuum.muzzle.position); stream.SetPosition(1, sourcePoint); }
        }
        public bool Shoot()
        {
            if (!vacuum || !vacuum.CanUse || amount <= 0 || Time.time < nextShot || IsFilling) return false;
            Vector3 point = vacuum.muzzle.position + vacuum.muzzle.forward * .22f;
            if (!ClearPath(vacuum.muzzle.position, point) || (Camera.main && !ClearPath(Camera.main.transform.position, point))) return false;
            foreach (var col in Physics.OverlapSphere(point, .085f, ~0, QueryTriggerInteraction.Ignore)) if (!Owned(col.transform)) return false;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Gota de agua";
            go.transform.position = point; go.transform.localScale = Vector3.one * .16f;
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            go.GetComponent<Renderer>().sharedMaterial = waterMaterial;
            var rb = go.AddComponent<Rigidbody>(); rb.mass = .08f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var projectile = go.AddComponent<Area1WaterProjectile>(); projectile.material = waterMaterial;
            var collider = go.GetComponent<Collider>();
            foreach (var own in GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(collider, own);
            if (vacuum.playerRoot) foreach (var own in vacuum.playerRoot.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(collider, own);
            rb.linearVelocity = vacuum.muzzle.forward * 12;
            amount--; nextShot = Time.time + .25f;
            RanchVRControls.Haptic(.2f);
            return true;
        }
        void OnDisable() { requested = IsFilling = HasSource = false; fraction = 0; if (stream) stream.enabled = false; }
    }
}
