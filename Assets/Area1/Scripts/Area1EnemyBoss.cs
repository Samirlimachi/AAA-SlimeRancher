using System.Collections.Generic;
using UnityEngine;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class Area1EnemyBoss : MonoBehaviour
    {
        public int maxWaterHits = 12;
        public float detectionRange = 20f;
        public float flightSpeed = 3.2f;
        public float attackDistance = 2.3f;
        public float attackCooldown = 1.1f;
        public float flightHeight = 1.8f;
        public int hitsToDefeatSlime = 3;
        public float playerDamage = 18f;

        public int WaterHitsRemaining { get; private set; }
        public bool Defeated { get; private set; }

        Rigidbody body;
        RanchItem targetSlime;
        Vector3 home;
        float nextSearch, nextAttack;
        readonly Dictionary<RanchItem, int> slimeDamage = new Dictionary<RanchItem, int>();

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            home = transform.position;
            WaterHitsRemaining = Mathf.Max(1, maxWaterHits);
        }

        bool ValidSlime(RanchItem item)
        {
            if (!item || item.Consumed || !item.gameObject.activeInHierarchy || !item.data) return false;
            if (item.GetComponent<Area1EnemySlime>()) return false;
            return item.data.kind == RanchItemKind.PinkSlime || item.data.kind == RanchItemKind.BlueSlime;
        }

        RanchItem FindSlime()
        {
            RanchItem nearest = null;
            float best = detectionRange * detectionRange;
            foreach (var item in FindObjectsByType<RanchItem>(FindObjectsSortMode.None))
            {
                if (!ValidSlime(item)) continue;
                float distance = (item.transform.position - transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = item;
            }
            return nearest;
        }

        void FixedUpdate()
        {
            if (Defeated || !body) return;
            if (!ValidSlime(targetSlime) || Time.time >= nextSearch)
            {
                targetSlime = FindSlime();
                nextSearch = Time.time + .4f;
            }

            var game = RanchGame.Instance;
            Transform player = game ? game.player : null;
            float playerDistance = player ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
            float slimeDistance = ValidSlime(targetSlime) ? Vector3.Distance(transform.position, targetSlime.transform.position) : float.MaxValue;
            bool attackPlayer = player && playerDistance <= detectionRange && playerDistance <= slimeDistance;

            Vector3 targetPosition;
            if (attackPlayer) targetPosition = player.position;
            else if (ValidSlime(targetSlime)) targetPosition = targetSlime.transform.position;
            else
            {
                float orbit = Time.time * .45f;
                targetPosition = home + new Vector3(Mathf.Sin(orbit) * 3f, 0, Mathf.Cos(orbit) * 3f);
            }

            float bob = Mathf.Sin(Time.time * 2.2f) * .28f;
            Vector3 flightTarget = targetPosition + Vector3.up * (flightHeight + bob);
            body.MovePosition(Vector3.MoveTowards(body.position, flightTarget, flightSpeed * Time.fixedDeltaTime));

            Vector3 facing = targetPosition - transform.position;
            facing.y = 0;
            if (facing.sqrMagnitude > .01f)
                body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(facing), 5f * Time.fixedDeltaTime));

            float attackRange = attackDistance + (attackPlayer ? .4f : 0f);
            if (Vector3.Distance(transform.position, targetPosition) > attackRange || Time.time < nextAttack) return;
            nextAttack = Time.time + attackCooldown;

            if (attackPlayer)
            {
                game.Damage(playerDamage);
                game.Notify("¡El jefe enemigo te atacó!");
                return;
            }

            if (!ValidSlime(targetSlime)) return;
            targetSlime.Body.AddForce((targetSlime.transform.position - transform.position).normalized * 1.2f + Vector3.up * 1.3f, ForceMode.Impulse);
            slimeDamage.TryGetValue(targetSlime, out int hits);
            hits++;
            slimeDamage[targetSlime] = hits;
            if (hits < hitsToDefeatSlime) return;
            var defeatedSlime = targetSlime;
            targetSlime = null;
            slimeDamage.Remove(defeatedSlime);
            defeatedSlime.Consume();
        }

        public void HitByWater()
        {
            if (Defeated) return;
            WaterHitsRemaining--;
            var game = RanchGame.Instance;
            if (WaterHitsRemaining > 0)
            {
                if (game) game.Notify("Jefe enemigo: faltan " + WaterHitsRemaining + " impactos de agua");
                return;
            }
            Defeated = true;
            if (game) game.Notify("¡Jefe enemigo derrotado con agua!");
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
