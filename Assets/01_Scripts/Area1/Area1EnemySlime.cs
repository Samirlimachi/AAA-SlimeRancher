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
        // Getting unstuck: progress check and a temporary sidestep around the obstacle.
        Vector3 progressPoint, sidestep;
        float progressCheckAt, sidestepUntil;
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
            direction = Steer(direction);
            Vector3 desired = direction.normalized * moveSpeed;
            body.linearVelocity = new Vector3(
                Mathf.MoveTowards(body.linearVelocity.x, desired.x, 8f * Time.fixedDeltaTime),
                body.linearVelocity.y,
                Mathf.MoveTowards(body.linearVelocity.z, desired.z, 8f * Time.fixedDeltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.fixedDeltaTime);
        }

        // Go around walls, rocks and structures instead of pushing into them; if no progress is made
        // for a while, hop and slide sideways for a moment to get free.
        Vector3 Steer(Vector3 wanted)
        {
            if (Time.time < sidestepUntil) return sidestep;
            var direction = Area1Steering.FreeDirection(body.position + Vector3.up * .1f, wanted, .22f, 1.1f, transform);
            if (Time.time >= progressCheckAt)
            {
                var moved = body.position - progressPoint;
                moved.y = 0;
                if (moved.magnitude < .25f && progressCheckAt > 0)
                {
                    sidestep = Quaternion.Euler(0, Random.value < .5f ? 90 : -90, 0) * wanted.normalized;
                    sidestepUntil = Time.time + .9f;
                    if (Physics.Raycast(body.position, Vector3.down, .45f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        body.AddForce(Vector3.up * 3.5f, ForceMode.VelocityChange);
                }
                progressPoint = body.position;
                progressCheckAt = Time.time + .8f;
            }
            return direction;
        }

        void FixedUpdate()
        {
            // Held in a hand (kinematic / item disabled): the hand moves it, not the AI.
            if (!item || item.Consumed || !body || body.isKinematic || !item.enabled) return;
            if (!ValidTarget(target) || Time.time >= nextSearch)
            {
                target = FindTarget();
                nextSearch = Time.time + .4f;
            }
            var game = RanchGame.Instance;
            if (game && game.player)
            {
                Vector3 playerDirection = game.PlayerPosition - transform.position;
                playerDirection.y = 0;
                float playerDistance = playerDirection.magnitude;
                float slimeDistance = target ? Vector3.Distance(transform.position, target.transform.position) : float.MaxValue;
                if (playerDistance <= detectionRange && playerDistance <= slimeDistance)
                {
                    if (playerDistance > attackDistance + .35f) MoveTowards(playerDirection);
                    else if (Time.time >= nextAttack)
                    {
                        nextAttack = Time.time + attackCooldown;
                        float before = game.health;
                        game.Damage(10);
                        if (game.health < before) Area1Effects.PlayerHit(transform.position);
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
            Area1Effects.HitTarget(victim.gameObject, transform.position);
            Area1Audio.Play(b => b.ataqueSlime, transform.position);
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
                float before = game.health;
                game.Damage(10);
                if (game.health < before) Area1Effects.PlayerHit(transform.position);
                game.Notify("¡Slime enemigo! Usa agua para neutralizarlo.");
            }
        }
        public void HitByWater(int damage = 1)
        {
            waterHits -= Mathf.Max(1, damage);
            if (waterHits > 0) return;
            GetComponent<RanchItem>().Consume();
            if (RanchGame.Instance) RanchGame.Instance.Notify("Slime enemigo neutralizado.");
        }
    }
}
