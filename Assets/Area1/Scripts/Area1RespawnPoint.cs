using UnityEngine;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    public sealed class Area1RespawnPoint : MonoBehaviour
    {
        public RanchItemKind kind = RanchItemKind.PinkSlime;
        public float respawnDelay = 8f;
        public float spawnRadius = .35f;
        public float claimRadius = 1.25f;

        public RanchItem Current { get; private set; }
        public int SpawnCount { get; private set; }

        bool initialized;
        float missingSince = -1f;

        void Start()
        {
            // RanchGame loads the saved world in Start; wait before claiming or spawning.
            Invoke(nameof(Initialize), 1.25f);
        }

        void Initialize()
        {
            Current = FindNearby();
            initialized = true;
            if (!Current) Spawn();
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

        void Update()
        {
            if (!initialized) return;
            if (Current && !Current.Consumed && Current.gameObject.activeInHierarchy)
            {
                missingSince = -1f;
                return;
            }
            if (missingSince < 0) missingSince = Time.time;
            if (Time.time - missingSince >= respawnDelay) Spawn();
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
