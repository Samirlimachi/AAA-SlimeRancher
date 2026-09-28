using UnityEngine;

namespace SlimeRancher.Area1
{
    // Player body sounds: footsteps while moving on the ground (joystick or real walking, measured at the
    // headset), a jump sound when leaving the ground upward, and a landing sound after a jump or fall.
    // Added to the XR rig by Area1Interactions.
    public sealed class Area1PlayerSounds : MonoBehaviour
    {
        [Tooltip("Metros entre paso y paso.")]
        public float stepLength = .75f;
        [Tooltip("Velocidad mínima (m/s) para que suenen pasos.")]
        public float minWalkSpeed = .5f;

        CharacterController controller;
        Vector3 lastHead;
        float walked, lastY, airTime;
        bool wasGrounded = true, hasLast;

        void Awake() => controller = GetComponent<CharacterController>();

        bool Grounded()
        {
            if (controller && controller.enabled) return controller.isGrounded ||
                Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, .25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, .25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        void LateUpdate()
        {
            var camera = Camera.main;
            if (!camera || Time.deltaTime <= 0) return;
            var head = camera.transform.position;
            bool grounded = Grounded();
            float verticalSpeed = (transform.position.y - lastY) / Time.deltaTime;

            if (!hasLast) { lastHead = head; lastY = transform.position.y; hasLast = true; wasGrounded = grounded; return; }

            // Jump: leaving the ground while going up.
            if (wasGrounded && !grounded && verticalSpeed > .8f) Area1Audio.Play2D(b => b.jugadorSalto);
            // Landing after some time in the air.
            if (!grounded) airTime += Time.deltaTime;
            else
            {
                if (!wasGrounded && airTime > .25f) Area1Audio.Play2D(b => b.jugadorAterrizaje);
                airTime = 0;
            }

            // Footsteps: horizontal distance travelled by the head while on the ground.
            var moved = head - lastHead;
            moved.y = 0;
            float speed = moved.magnitude / Time.deltaTime;
            if (grounded && speed > minWalkSpeed && speed < 12) // ignore teleports/snaps
            {
                walked += moved.magnitude;
                if (walked >= stepLength) { walked = 0; Area1Audio.Play2D(b => b.jugadorCamina); }
            }
            else if (speed <= minWalkSpeed) walked = stepLength * .6f; // first step comes quickly when starting to walk

            lastHead = head;
            lastY = transform.position.y;
            wasGrounded = grounded;
        }
    }
}
