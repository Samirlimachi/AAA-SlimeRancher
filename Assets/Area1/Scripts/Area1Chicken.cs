using UnityEngine;
using SlimeRancherVR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SlimeRancher.Area1
{
    [RequireComponent(typeof(RanchItem))]
    public sealed class Area1Chicken : MonoBehaviour
    {
        public float speed = .65f;
        Rigidbody body;
        RanchItem item;
        XRGrabInteractable grab;
        Vector3 home, direction;
        float nextTurn;
        bool wasGrounded;
        void Awake() { body = GetComponent<Rigidbody>(); item = GetComponent<RanchItem>(); grab = GetComponent<XRGrabInteractable>(); }
        void Start() { SetHome(transform.position); }
        public void SetHome(Vector3 position) { home = position; nextTurn = 0; }
        void FixedUpdate()
        {
            // Suction, throwing and XR grabbing take priority over wandering.
            if (!item.enabled || item.Consumed || body.isKinematic || !body.useGravity ||
                (grab && grab.isSelected) || Time.time < item.graceUntil || Mathf.Abs(body.linearVelocity.y) > .4f) { wasGrounded = false; return; }
            if (!Physics.Raycast(body.position + Vector3.up * .05f, Vector3.down, .45f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) { wasGrounded = false; return; }
            if (!wasGrounded) { SetHome(body.position); wasGrounded = true; }
            if (Time.time > nextTurn)
            {
                Vector2 random = Random.insideUnitCircle;
                direction = new Vector3(random.x, 0, random.y).normalized;
                if (Vector3.Distance(body.position, home) > 2.3f) { direction = home - body.position; direction.y = 0; direction.Normalize(); }
                if (Random.value < .3f) direction = Vector3.zero;
                nextTurn = Time.time + Random.Range(1, 3f);
            }
            if (direction.sqrMagnitude > .01f && Physics.Raycast(body.position, direction, .55f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) { direction = -direction; nextTurn = Time.time + 1; }
            var velocity = body.linearVelocity;
            var desired = direction * speed;
            velocity.x = Mathf.MoveTowards(velocity.x, desired.x, 12 * Time.fixedDeltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, desired.z, 12 * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
            if (direction.sqrMagnitude > .01f) body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), 100 * Time.fixedDeltaTime));
        }
    }
}
