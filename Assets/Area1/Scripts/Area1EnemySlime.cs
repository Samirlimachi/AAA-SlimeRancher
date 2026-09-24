using System.Collections.Generic;
using UnityEngine;
using SlimeRancherVR;
namespace SlimeRancher.Area1
{
    [RequireComponent(typeof(RanchItem))]
    public sealed class Area1EnemySlime : MonoBehaviour
    {
        public int waterHits = 3;
        public float detectionRange = 12f;
        public float moveSpeed = 2.2f;
        public float attackDistance = .8f;
        public float attackCooldown = 1f;
        public int hitsToDefeatPink = 3;
        Rigidbody body;
        RanchItem item;
        PinkSlime target;
        float nextSearch, nextAttack;
        readonly Dictionary<RanchItem, int> damage = new Dictionary<RanchItem, int>();

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            item = GetComponent<RanchItem>();
        }

        bool ValidTarget(PinkSlime pink)
        {
            if (!pink || !pink.gameObject.activeInHierarchy || pink.GetComponent<Area1EnemySlime>()) return false;
            var pinkItem = pink.GetComponent<RanchItem>();
            return pinkItem && !pinkItem.Consumed && pinkItem.data && pinkItem.data.kind == RanchItemKind.PinkSlime;
        }

        PinkSlime FindTarget()
        {
            PinkSlime nearest = null;
            float best = detectionRange * detectionRange;
            foreach (var pink in FindObjectsByType<PinkSlime>(FindObjectsSortMode.None))
            {
                if (!ValidTarget(pink)) continue;
                float distance = (pink.transform.position - transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = pink;
            }
            return nearest;
        }

        void MoveTowards(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude < .001f) return;
            Vector3 desired = direction.normalized * moveSpeed;
            body.linearVelocity = new Vector3(
                Mathf.MoveTowards(body.linearVelocity.x, desired.x, 8f * Time.fixedDeltaTime),
                body.linearVelocity.y,
                Mathf.MoveTowards(body.linearVelocity.z, desired.z, 8f * Time.fixedDeltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.fixedDeltaTime);
        }

        void FixedUpdate()
        {
            if (!item || item.Consumed || !body) return;
            if (!ValidTarget(target) || Time.time >= nextSearch)
            {
                target = FindTarget();
                nextSearch = Time.time + .4f;
            }
            var game = RanchGame.Instance;
            if (game && game.player)
            {
                Vector3 playerDirection = game.player.position - transform.position;
                playerDirection.y = 0;
                float playerDistance = playerDirection.magnitude;
                float slimeDistance = target ? Vector3.Distance(transform.position, target.transform.position) : float.MaxValue;
                if (playerDistance <= detectionRange && playerDistance <= slimeDistance)
                {
                    if (playerDistance > attackDistance + .35f) MoveTowards(playerDirection);
                    else if (Time.time >= nextAttack)
                    {
                        nextAttack = Time.time + attackCooldown;
                        game.Damage(10);
                        game.Notify("¡Un slime enemigo te atacó! Usa agua para neutralizarlo.");
                    }
                    return;
                }
            }
            if (!target) return;
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0;
            float distance = direction.magnitude;
            if (distance > attackDistance)
            {
                MoveTowards(direction);
                return;
            }
            if (Time.time < nextAttack) return;
            nextAttack = Time.time + attackCooldown;
            var victim = target.GetComponent<RanchItem>();
            if (!victim || victim.Consumed) return;
            victim.Body.AddForce(direction.normalized * .45f + Vector3.up * .25f, ForceMode.Impulse);
            damage.TryGetValue(victim, out int hits);
            hits++;
            damage[victim] = hits;
            if (hits >= hitsToDefeatPink)
            {
                victim.Consume();
                damage.Remove(victim);
                target = null;
            }
        }

        void OnCollisionStay(Collision collision)
        {
            var game = RanchGame.Instance;
            if (!game || !game.player || !item.enabled || item.Consumed) return;
            if (collision.transform.IsChildOf(game.player))
            {
                game.Damage(10);
                game.Notify("¡Slime enemigo! Usa agua para neutralizarlo.");
            }
        }
        public void HitByWater()
        {
            if (--waterHits > 0) return;
            GetComponent<RanchItem>().Consume();
            if (RanchGame.Instance) RanchGame.Instance.Notify("Slime enemigo neutralizado.");
        }
    }
}
