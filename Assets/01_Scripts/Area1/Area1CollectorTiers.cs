using UnityEngine;

namespace SlimeRancher.Area1
{
    // A single plort collector spot: only the bought tier is active (0 = green, 1 = red, 2 = black).
    // Area1UpgradeShop calls SetLevel when the "RECOLECTOR" upgrade is bought.
    public sealed class Area1CollectorTiers : MonoBehaviour
    {
        [Tooltip("Recolectores del peor al mejor. Todos se colocan donde esté el primero (el verde).")]
        public GameObject[] tiers;
        [Tooltip("Tamaño de los recolectores (1 = tamaño original del modelo). Se aplica en el editor.")]
        [Min(.5f)] public float collectorScale = 1.6f;

        public int Level { get; private set; }

        // The basic (first) collector marks the spot: moving it moves every tier.
        void Awake() => SnapToFirst();

        public void SnapToFirst()
        {
            if (tiers == null || tiers.Length == 0 || !tiers[0]) return;
            var anchor = tiers[0].transform;
            foreach (var tier in tiers)
                if (tier && tier != tiers[0]) tier.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }

        public void SetLevel(int level)
        {
            if (tiers == null || tiers.Length == 0) return;
            Level = Mathf.Clamp(level, 0, tiers.Length - 1);
            for (int i = 0; i < tiers.Length; i++)
                if (tiers[i]) tiers[i].SetActive(i == Level);
        }

        public string TierName(int level)
        {
            if (tiers == null || level < 0 || level >= tiers.Length || !tiers[level]) return "-";
            var collector = tiers[level].GetComponent<Area1PlortCollector>();
            return collector ? collector.collectorName + " (x" + collector.valueMultiplier + ")" : tiers[level].name;
        }
    }
}
