using UnityEngine;
using UnityEngine.UI;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // Wall shop for permanent player upgrades (PlayerPrefs): max health, water damage, water tank size
    // and the plort collector tier. Each upgrade is identified by its key, not its position.
    public sealed class Area1UpgradeShop : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Upgrade
        {
            public string title;
            public string key;
            public int[] prices;
        }

        [Header("Base values and per-level increase")]
        public float baseHealth = 100, healthPerLevel = 25;
        public int baseDamage = 1, damagePerLevel = 1;
        public int baseWater = 30, waterPerLevel = 10;

        public const string HealthKey = "Area1.Mejoras.Vida", DamageKey = "Area1.Mejoras.Danio",
            WaterKey = "Area1.Mejoras.Agua", CollectorKey = "Area1.Mejoras.Recolector";

        public Upgrade[] upgrades =
        {
            new Upgrade { title = "VIDA MAXIMA", key = HealthKey, prices = new[] { 100, 200, 300, 400 } },
            new Upgrade { title = "DAÑO DE AGUA", key = DamageKey, prices = new[] { 150, 300, 500 } },
            new Upgrade { title = "TANQUE DE AGUA", key = WaterKey, prices = new[] { 80, 160, 240, 320, 400 } },
            new Upgrade { title = "RECOLECTOR", key = CollectorKey, prices = new[] { 250, 600 } }
        };

        [Header("Scene objects (Area1 > Construir tablero y tiendas)")]
        public Area1BoardSlot[] slots;
        public Text header, status;
        bool built;
        float nextPress, nextRefresh;
        Area1WaterVacuum tank;
        Area1CollectorTiers collector;
        RanchGame Game => RanchGame.Instance;

        int Level(int index) => Mathf.Clamp(PlayerPrefs.GetInt(upgrades[index].key, 0), 0, upgrades[index].prices.Length);

        int Level(string key)
        {
            for (int i = 0; i < upgrades.Length; i++) if (upgrades[i].key == key) return Level(i);
            return 0;
        }

        void Awake()
        {
            built = Area1Board.IsBuilt(slots, upgrades.Length, header, this);
            if (!built) return;
            status.text = "Apunta a una mejora y presiona GRIP.\nLas mejoras se guardan para siempre.";
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                Area1Board.Hook(slots[i], () => Buy(index));
            }
        }

        void Start()
        {
            tank = FindAnyObjectByType<Area1WaterVacuum>();
            collector = FindAnyObjectByType<Area1CollectorTiers>();
            // A loaded game keeps its saved health; a new one starts full.
            Apply(!(Game && Game.Loaded));
        }

        // Pushes the bought levels into the game. Health refills to the new maximum when asked.
        void Apply(bool refillHealth)
        {
            if (Game)
            {
                Game.maxHealth = baseHealth + healthPerLevel * Level(HealthKey);
                Game.health = refillHealth ? Game.maxHealth : Mathf.Min(Game.health, Game.maxHealth);
            }
            if (tank)
            {
                tank.waterDamage = baseDamage + damagePerLevel * Level(DamageKey);
                tank.capacity = baseWater + waterPerLevel * Level(WaterKey);
            }
            // Swaps the collector model right away when its upgrade is bought.
            if (collector) collector.SetLevel(Level(CollectorKey));
        }

        string Value(int index, int level)
        {
            switch (upgrades[index].key)
            {
                case HealthKey: return (baseHealth + healthPerLevel * level) + " vida";
                case DamageKey: return "x" + (baseDamage + damagePerLevel * level) + " daño";
                case WaterKey: return (baseWater + waterPerLevel * level) + " agua";
                case CollectorKey: return collector ? collector.TierName(level) : "Nivel " + level;
                default: return "Nivel " + level;
            }
        }

        void Buy(int index)
        {
            if (Time.time < nextPress || !Game || !built) return;
            nextPress = Time.time + .4f;
            var upgrade = upgrades[index];
            int level = Level(index);
            if (level >= upgrade.prices.Length)
                Game.Notify(upgrade.title + " ya está al máximo.");
            else if (Game.coins < upgrade.prices[level])
                Game.Notify("Necesitas " + upgrade.prices[level] + " monedas para mejorar " + upgrade.title + ".");
            else
            {
                Game.coins -= upgrade.prices[level];
                PlayerPrefs.SetInt(upgrade.key, level + 1);
                PlayerPrefs.Save();
                Apply(upgrades[index].key == HealthKey);
                Game.Notify("¡Mejora comprada! " + upgrade.title + ": " + Value(index, level + 1) + ".");
            }
            Refresh();
        }

        void Update()
        {
            if (Time.time >= nextRefresh) Refresh();
        }

        void Refresh()
        {
            nextRefresh = Time.time + .25f;
            if (!Game || !built) return;
            header.text = "Monedas: " + Game.coins;
            for (int i = 0; i < upgrades.Length; i++)
            {
                var upgrade = upgrades[i];
                int level = Level(i);
                bool maxed = level >= upgrade.prices.Length;
                bool affordable = !maxed && Game.coins >= upgrade.prices[level];
                Area1Board.Show(slots[i], maxed ? Area1Board.Done : affordable ? Area1Board.Owned : Area1Board.Locked,
                    upgrade.title + "\nNivel " + level + " / " + upgrade.prices.Length,
                    "Ahora: " + Value(i, level) + (maxed ? "\nMAXIMO" : "\nSiguiente: " + Value(i, level + 1)),
                    maxed ? 0 : upgrade.prices[level]);
            }
        }
    }
}
