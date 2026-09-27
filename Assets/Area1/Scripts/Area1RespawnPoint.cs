using System.Collections.Generic;
using UnityEngine;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // Limited respawning so the ranch is not an endless farm:
    // - Pink slimes: a missing slime comes back slowly, and only while there are fewer than
    //   `maxPinkSlimes` pink slimes in the whole area.
    // - Chickens: never respawn on their own. Only the chickens already in the area exist at the start;
    //   each chicken a slime eats comes back later at a random point of its kind.
    public sealed class Area1RespawnPoint : MonoBehaviour
    {
        public RanchItemKind kind = RanchItemKind.PinkSlime;
        public float spawnRadius = .35f;
        public float claimRadius = 1.25f;

        [Header("Límites de aparición")]
        [Tooltip("Máximo de slimes rosados vivos en toda el área.")]
        public int maxPinkSlimes = 6;
        [Tooltip("Segundos hasta que vuelve un slime rosado que falta.")]
        public float slimeRespawnDelay = 120f;
        [Tooltip("Segundos hasta que vuelve un pollo que se comió un slime.")]
        public float chickenRespawnDelay = 45f;

        public RanchItem Current { get; private set; }
        public int SpawnCount { get; private set; }

        static readonly List<Area1RespawnPoint> points = new List<Area1RespawnPoint>();
        readonly List<float> eatenRespawns = new List<float>();
        bool initialized;
        float missingSince = -1f;

        bool IsChicken => kind == RanchItemKind.Chicken || kind == RanchItemKind.ElderChicken;

        void OnEnable()
        {
            points.Add(this);
            if (points.Count == 1) RanchItem.EatenBySlime += OnEaten;
        }

        void OnDisable()
        {
            points.Remove(this);
            if (points.Count == 0) RanchItem.EatenBySlime -= OnEaten;
        }

        // A slime ate a chicken: one random point of that kind brings a new one back later.
        static void OnEaten(RanchItem food)
        {
            if (!food || !food.data) return;
            var candidates = points.FindAll(p => p.kind == food.data.kind && p.IsChicken);
            if (candidates.Count == 0) return;
            var point = candidates[Random.Range(0, candidates.Count)];
            point.eatenRespawns.Add(Time.time + point.chickenRespawnDelay);
        }

        void Start()
        {
            // RanchGame loads the saved world in Start; wait before claiming or spawning.
            Invoke(nameof(Initialize), 1.25f);
        }

        void Initialize()
        {
            Current = FindNearby();
            initialized = true;
            // Chickens: only the ones already placed in the area at the start.
            if (!Current && !IsChicken && CanSpawnPinkSlime()) Spawn();
        }

        RanchItem FindNearby()
        {
            RanchItem nearest = null;
            float best = claimRadius * claimRadius;
            foreach (var item in FindObjectsByType<RanchItem>())
            {
                if (!item || item.Consumed || !item.data || item.data.kind != kind || !item.gameObject.activeInHierarchy) continue;
                float distance = (item.transform.position - transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = item;
            }
            return nearest;
        }

        bool CanSpawnPinkSlime()
        {
            if (kind != RanchItemKind.PinkSlime) return true;
            int alive = 0;
            foreach (var item in FindObjectsByType<RanchItem>())
                if (item && !item.Consumed && item.data && item.data.kind == RanchItemKind.PinkSlime && item.gameObject.activeInHierarchy) alive++;
            return alive < maxPinkSlimes;
        }

        void Update()
        {
            if (!initialized) return;
            if (IsChicken)
            {
                if (eatenRespawns.Count > 0 && Time.time >= eatenRespawns[0])
                {
                    eatenRespawns.RemoveAt(0);
                    Spawn();
                }
                return;
            }
            if (Current && !Current.Consumed && Current.gameObject.activeInHierarchy)
            {
                missingSince = -1f;
                return;
            }
            if (missingSince < 0) missingSince = Time.time;
            if (Time.time - missingSince < slimeRespawnDelay) return;
            if (CanSpawnPinkSlime()) Spawn();
            else missingSince = Time.time; // area is full: check again after another delay
        }

        void Spawn()
        {
            var game = RanchGame.Instance;
            if (!game || !game.Data(kind) || !game.Data(kind).prefab) return;
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 point = transform.position + new Vector3(circle.x, 0, circle.y);
            Current = game.Spawn(kind, point, Quaternion.Euler(0, Random.Range(0, 360f), 0));
            Current.name = game.Data(kind).itemName + " - generado";
            Current.graceUntil = Time.time + 1f;
            SpawnCount++;
            missingSince = -1f;
            Area1Effects.PlortSold(point + Vector3.up * .2f); // little sparkle so the arrival is noticed
            if (IsChicken) Area1Audio.Play(b => b.spawnPollo, point);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = kind == RanchItemKind.PinkSlime
                ? new Color(1f, .25f, .6f, .8f)
                : kind == RanchItemKind.ElderChicken
                    ? new Color(.25f, .25f, .25f, .8f)
                    : new Color(1f, .85f, .25f, .8f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(.15f, spawnRadius));
        }
    }
}
