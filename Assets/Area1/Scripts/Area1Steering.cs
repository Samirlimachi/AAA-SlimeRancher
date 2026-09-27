using UnityEngine;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // Simple obstacle avoidance for slimes: look ahead along the wanted direction and, if a wall, rock
    // or structure is in the way, fan out left/right until a free direction is found.
    // Other slimes, loose items and the player are not obstacles (they get pushed or attacked instead).
    public static class Area1Steering
    {
        static readonly float[] Angles = { 0, 30, -30, 60, -60, 90, -90, 125, -125, 160, -160 };
        static readonly RaycastHit[] Hits = new RaycastHit[8];

        public static bool IsObstacle(Collider collider, Transform self)
        {
            if (!collider || collider.isTrigger || collider.transform.IsChildOf(self)) return false;
            if (collider.GetComponentInParent<RanchItem>()) return false;
            var game = RanchGame.Instance;
            if (game && game.player && collider.transform.IsChildOf(game.player)) return false;
            return true;
        }

        public static bool Blocked(Vector3 origin, Vector3 direction, float radius, float distance, Transform self)
        {
            int count = Physics.SphereCastNonAlloc(origin, radius, direction, Hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (IsObstacle(Hits[i].collider, self)) return true;
            return false;
        }

        // Returns the free direction closest to `wanted` (flat), or `wanted` itself if everything is blocked.
        public static Vector3 FreeDirection(Vector3 origin, Vector3 wanted, float radius, float distance, Transform self)
        {
            wanted.y = 0;
            if (wanted.sqrMagnitude < .0001f) return wanted;
            wanted.Normalize();
            foreach (float angle in Angles)
            {
                var candidate = Quaternion.Euler(0, angle, 0) * wanted;
                if (!Blocked(origin, candidate, radius, distance, self)) return candidate;
            }
            return wanted;
        }
    }
}
