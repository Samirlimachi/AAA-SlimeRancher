using System;
using UnityEngine;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Area1PlortCollector : MonoBehaviour
    {
        public string collectorName = "Recolector verde";
        [Min(1)] public int valueMultiplier = 1;

        public int LastReward { get; private set; }
        public int CollectedCount { get; private set; }

        void Awake()
        {
            var trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        bool IsPlort(RanchItem item)
        {
            return item && item.data && !item.Consumed &&
                item.data.kind.ToString().EndsWith("Plort", StringComparison.Ordinal);
        }

        public bool Collect(RanchItem item)
        {
            var game = RanchGame.Instance;
            if (!game || !IsPlort(item)) return false;
            int reward = Mathf.Max(1, item.data.saleValue) * Mathf.Max(1, valueMultiplier);
            if (!item.Consume()) return false;
            game.coins += reward;
            LastReward = reward;
            CollectedCount++;
            game.Notify(collectorName + ": plort vendido por +" + reward + " monedas");
            return true;
        }

        void OnTriggerEnter(Collider other)
        {
            Collect(other.GetComponentInParent<RanchItem>());
        }

        // Also catches a plort that is placed directly inside the receiver.
        void OnTriggerStay(Collider other)
        {
            Collect(other.GetComponentInParent<RanchItem>());
        }
    }
}
