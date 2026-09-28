using System.Collections;
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

        [Header("Escupir baba (ataque a distancia)")]
        public float spitRange = 16f;
        public float spitDamage = 12f;
        public Vector2 spitCooldown = new Vector2(4f, 7f);
        [Tooltip("Tiempo que se infla antes de escupir: aviso para esquivar.")]
        public float spitChargeTime = .6f;

        [Header("Invocar slimes malos")]
        public float summonCooldown = 16f;
        public int summonCount = 2;
        public int maxMinions = 4;

        // Each summoned slime, so the wave can count it.
        public event System.Action<GameObject> Summoned;

        public int WaterHitsRemaining { get; private set; }
        public bool Defeated { get; private set; }

        Rigidbody body;
        RanchItem targetSlime;
        Vector3 home;
        float nextSearch, nextAttack, nextSpit, nextSummon;
        bool charging;
        Vector3 baseScale;
        readonly List<GameObject> minions = new List<GameObject>();
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
            baseScale = transform.localScale;
            nextSpit = Time.time + 3;
            nextSummon = Time.time + 8;
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
            var playerPosition = game ? game.PlayerPosition : Vector3.zero; // real (walked) position, not the rig origin
            float playerDistance = player ? Vector3.Distance(transform.position, playerPosition) : float.MaxValue;
            float slimeDistance = ValidSlime(targetSlime) ? Vector3.Distance(transform.position, targetSlime.transform.position) : float.MaxValue;
            bool attackPlayer = player && playerDistance <= detectionRange && playerDistance <= slimeDistance;

            // Special attacks: summon helpers, or spit goo when the player keeps its distance.
            if (player && !charging)
            {
                if (Time.time >= nextSummon && playerDistance <= detectionRange && AliveMinions() < maxMinions) Summon();
                else if (Time.time >= nextSpit && playerDistance <= spitRange && playerDistance > attackDistance + 1) StartCoroutine(Spit(player));
            }
            if (charging) return; // hovers in place while swelling up

            Vector3 targetPosition;
            if (attackPlayer) targetPosition = playerPosition;
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
                float before = game.health;
                game.Damage(playerDamage);
                if (game.health < before) Area1Effects.PlayerHit(transform.position, 1.4f);
                game.Notify("¡El jefe enemigo te atacó!");
                return;
            }

            if (!ValidSlime(targetSlime)) return;
            targetSlime.Body.AddForce((targetSlime.transform.position - transform.position).normalized * 1.2f + Vector3.up * 1.3f, ForceMode.Impulse);
            Area1Effects.HitTarget(targetSlime.gameObject, transform.position, 1.5f);
            Area1Audio.Play(b => b.ataqueSlime, transform.position);
            slimeDamage.TryGetValue(targetSlime, out int hits);
            hits++;
            slimeDamage[targetSlime] = hits;
            if (hits < hitsToDefeatSlime) return;
            var defeatedSlime = targetSlime;
            targetSlime = null;
            slimeDamage.Remove(defeatedSlime);
            defeatedSlime.Consume();
        }

        IEnumerator Spit(Transform player)
        {
            charging = true;
            nextSpit = Time.time + Random.Range(spitCooldown.x, spitCooldown.y);
            for (float t = 0; t < spitChargeTime; t += Time.deltaTime)
            {
                transform.localScale = baseScale * (1 + Mathf.Sin(t / spitChargeTime * Mathf.PI) * .18f);
                var look = (RanchGame.Instance ? RanchGame.Instance.PlayerPosition : player.position) - transform.position;
                look.y = 0;
                if (look.sqrMagnitude > .01f) body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(look), 10 * Time.deltaTime));
                yield return null;
            }
            transform.localScale = baseScale;
            if (!Defeated)
            {
                var camera = Camera.main;
                var target = camera ? camera.transform.position + Vector3.down * .35f : player.position + Vector3.up * 1.2f;
                Area1BossSpit.Launch(transform.position + transform.forward * .8f + Vector3.down * .2f, target, spitDamage, transform);
            }
            charging = false;
        }

        void Summon()
        {
            nextSummon = Time.time + summonCooldown;
            var game = RanchGame.Instance;
            if (!game || !game.Data(RanchItemKind.EnemySlime)) return;
            for (int i = 0; i < summonCount; i++)
            {
                var offset = Quaternion.Euler(0, i * 360f / summonCount + Random.Range(-30f, 30f), 0) * Vector3.forward * 1.8f;
                var point = new Vector3(transform.position.x, .5f, transform.position.z) + offset;
                var item = game.Spawn(RanchItemKind.EnemySlime, point, Quaternion.Euler(0, Random.Range(0, 360f), 0));
                var enemy = item.GetComponent<Area1EnemySlime>();
                if (enemy) enemy.detectionRange = Mathf.Max(enemy.detectionRange, detectionRange);
                minions.Add(item.gameObject);
                Area1Effects.Impact(point + Vector3.up * .3f, Area1Effects.Goo, 1.2f);
                Summoned?.Invoke(item.gameObject);
            }
            game.Notify("¡El jefe invocó slimes malos!");
        }

        int AliveMinions()
        {
            minions.RemoveAll(minion => !minion || !minion.activeInHierarchy);
            return minions.Count;
        }

        public void HitByWater(int damage = 1)
        {
            if (Defeated) return;
            WaterHitsRemaining = Mathf.Max(0, WaterHitsRemaining - Mathf.Max(1, damage));
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
