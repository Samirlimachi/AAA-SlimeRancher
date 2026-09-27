using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // Wall board that works like a shop for enemy waves 1-5.
    // A level becomes buyable after completing the previous one; once bought it stays unlocked (PlayerPrefs).
    public sealed class Area1WaveBoard : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Wave
        {
            public int enemies;
            public int waterHits;
            public float moveSpeed;
            public float spawnInterval;
            public int price;
            public int reward;
            public bool boss;
        }

        public Wave[] waves =
        {
            new Wave { enemies = 3, waterHits = 2, moveSpeed = 1.8f, spawnInterval = 2.5f, price = 0, reward = 60 },
            new Wave { enemies = 5, waterHits = 3, moveSpeed = 2.2f, spawnInterval = 2f, price = 100, reward = 90 },
            new Wave { enemies = 7, waterHits = 3, moveSpeed = 2.6f, spawnInterval = 1.6f, price = 150, reward = 130 },
            new Wave { enemies = 9, waterHits = 4, moveSpeed = 3f, spawnInterval = 1.3f, price = 200, reward = 180 },
            new Wave { enemies = 8, waterHits = 4, moveSpeed = 3.1f, spawnInterval = 1.3f, price = 300, reward = 300, boss = true }
        };
        [Header("Spawn area (world)")]
        public Vector2 spawnX = new Vector2(-14, 14);
        public Vector2 spawnZ = new Vector2(10, 16);
        public Vector3 bossSpawn = new Vector3(0, 4.5f, 14);
        public float enemyDetectionRange = 35;
        [Tooltip("Jefe de la oleada 5 (prefab). Si falta, se usa un jefe colocado en la escena.")]
        public GameObject bossPrefab;

        public const string CompletedKey = "Area1.Oleadas.Completadas";
        public const string PurchasedKey = "Area1.Oleadas.Compradas";

        int completed, purchased, activeLevel, toSpawn;
        bool spawning;
        float nextPress, nextRefresh;
        GameObject bossTemplate;
        readonly List<GameObject> alive = new List<GameObject>();
        [Header("Scene objects (Area1 > Construir tablero y tiendas)")]
        public Area1BoardSlot[] slots;
        public Text header, status;
        bool built;

        RanchGame Game => RanchGame.Instance;

        void Awake()
        {
            completed = PlayerPrefs.GetInt(CompletedKey, 0);
            purchased = Mathf.Max(1, PlayerPrefs.GetInt(PurchasedKey, 1));
            built = Area1Board.IsBuilt(slots, waves.Length, header, this);
            if (!built) return;
            for (int i = 0; i < slots.Length; i++)
            {
                int level = i + 1;
                Area1Board.Hook(slots[i], () => Press(level));
            }
        }

        void Start()
        {
            bossTemplate = bossPrefab;
            if (!bossTemplate)
            {
                // Legacy: a boss placed in the scene becomes the template instead of attacking from the start.
                var boss = FindAnyObjectByType<Area1EnemyBoss>();
                if (boss)
                {
                    bossTemplate = boss.gameObject;
                    bossTemplate.SetActive(false);
                }
            }
            if (Game) Game.Died += OnPlayerDied;
            Refresh();
        }

        void Press(int level)
        {
            if (Time.time < nextPress || !Game || !built) return;
            nextPress = Time.time + .5f;
            var wave = waves[level - 1];
            if (activeLevel > 0)
                Game.Notify("Termina la oleada " + activeLevel + " antes de elegir otra.");
            else if (completed < level - 1)
                Game.Notify("Nivel " + level + " bloqueado: completa primero el nivel " + (level - 1) + ".");
            else if (level > purchased)
            {
                if (Game.coins < wave.price)
                    Game.Notify("Necesitas " + wave.price + " monedas para desbloquear el nivel " + level + ".");
                else
                {
                    Game.coins -= wave.price;
                    purchased = level;
                    PlayerPrefs.SetInt(PurchasedKey, purchased);
                    PlayerPrefs.Save();
                    Game.Notify("¡Nivel " + level + " desbloqueado para siempre! Presiona otra vez para empezar la oleada.");
                }
            }
            else StartCoroutine(RunWave(level));
            Refresh();
        }

        IEnumerator RunWave(int level)
        {
            var wave = waves[level - 1];
            activeLevel = level;
            spawning = true;
            toSpawn = wave.enemies;
            alive.Clear();
            Game.Notify("¡Oleada " + level + "! " + wave.enemies + " slimes enemigos" + (wave.boss ? " y el JEFE" : "") + ". Usa agua para neutralizarlos.");
            if (wave.boss && bossTemplate)
            {
                var boss = Instantiate(bossTemplate, bossSpawn, Quaternion.identity);
                boss.name = "JEFE ENEMIGO - Oleada " + level;
                boss.SetActive(true);
                var brain = boss.GetComponent<Area1EnemyBoss>();
                brain.detectionRange = Mathf.Max(brain.detectionRange, enemyDetectionRange);
                brain.Summoned += minion => alive.Add(minion); // summoned slimes must be defeated too
                alive.Add(boss);
            }
            for (int i = 0; i < wave.enemies; i++)
            {
                var point = new Vector3(Random.Range(spawnX.x, spawnX.y), .5f, Random.Range(spawnZ.x, spawnZ.y));
                var item = Game.Spawn(RanchItemKind.EnemySlime, point, Quaternion.Euler(0, 180, 0));
                var enemy = item.GetComponent<Area1EnemySlime>();
                if (enemy)
                {
                    enemy.waterHits = wave.waterHits;
                    enemy.moveSpeed = wave.moveSpeed;
                    enemy.detectionRange = enemyDetectionRange;
                }
                alive.Add(item.gameObject);
                toSpawn--;
                Refresh();
                yield return new WaitForSeconds(wave.spawnInterval);
            }
            spawning = false;
        }

        int Remaining()
        {
            // Defeated enemies are destroyed (RanchItem.Consume / boss defeat), leaving null or inactive entries.
            alive.RemoveAll(enemy => !enemy || !enemy.activeInHierarchy);
            return alive.Count;
        }

        void Update()
        {
            if (activeLevel > 0 && !spawning && Remaining() == 0) Complete();
            if (Time.time >= nextRefresh) Refresh();
        }

        void Complete()
        {
            int level = activeLevel;
            var wave = waves[level - 1];
            activeLevel = 0;
            if (level > completed)
            {
                completed = level;
                PlayerPrefs.SetInt(CompletedKey, completed);
                PlayerPrefs.Save();
            }
            if (Game) Game.coins += wave.reward;
            bool nextUnlocked = level < waves.Length && purchased <= level;
            // The yellow banner replaces the grey message bar for this event.
            var hud = FindAnyObjectByType<Area1HUD>();
            if (hud)
            {
                bool last = level == waves.Length;
                hud.Celebrate(last ? "¡FELICIDADES, GANASTE!" : "¡OLEADA " + level + " COMPLETADA!",
                    (last ? "Derrotaste al jefe  ·  " : "¡Felicidades!  ·  ") + "+" + wave.reward + " monedas" +
                    (nextUnlocked ? "  ·  Nivel " + (level + 1) + " disponible" : ""));
            }
            else if (Game)
                Game.Notify("¡Oleada " + level + " completada! +" + wave.reward + " monedas." +
                    (nextUnlocked ? " Nivel " + (level + 1) + " disponible en el tablero." : ""));
            Refresh();
        }

        void Refresh()
        {
            nextRefresh = Time.time + .25f;
            if (!built) return;
            header.text = "Monedas: " + (Game ? Game.coins : 0) + "     Completados: " + completed + " / " + waves.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                int level = i + 1;
                var wave = waves[i];
                string state;
                Color color;
                int price = 0;
                if (activeLevel == level) { state = "EN CURSO"; color = Area1Board.Running; }
                else if (completed < level - 1) { state = "BLOQUEADO"; color = Area1Board.Locked; }
                else if (level > purchased) { state = "COMPRAR"; color = Area1Board.Buyable; price = wave.price; }
                else if (completed >= level) { state = "COMPLETADO\nJUGAR"; color = Area1Board.Done; }
                else { state = "JUGAR"; color = Area1Board.Owned; }
                if (activeLevel > 0 && activeLevel != level) color *= .55f;
                Area1Board.Show(slots[i], color, "NIVEL " + level + "\n" + wave.enemies + " slimes" + (wave.boss ? " + JEFE" : ""), state, price);
            }
            status.text = activeLevel > 0
                ? "OLEADA " + activeLevel + " EN CURSO\nEnemigos restantes: " + (Remaining() + toSpawn)
                : "Apunta a un nivel y presiona GRIP.\nCompleta un nivel para poder comprar el siguiente.";
        }

        // Dying during a wave loses it: remaining enemies vanish and the defeat banner shows.
        void OnPlayerDied()
        {
            if (activeLevel <= 0) return;
            int level = activeLevel;
            StopAllCoroutines();
            spawning = false;
            toSpawn = 0;
            activeLevel = 0;
            foreach (var enemy in alive)
            {
                if (!enemy) continue;
                Area1Effects.Impact(enemy.transform.position, new Color(.5f, .2f, .6f), .8f);
                enemy.SetActive(false);
                Destroy(enemy);
            }
            alive.Clear();
            var hud = FindAnyObjectByType<Area1HUD>();
            if (hud)
            {
                Game.ClearMessage(); // the banner replaces the grey respawn message
                hud.ShowDefeat("OLEADA " + level + " FALLIDA", "Los slimes malos te vencieron  ·  ¡Inténtalo de nuevo en el tablero!");
            }
            else Game.Notify("Oleada " + level + " fallida. Inténtalo de nuevo en el tablero.");
            Refresh();
        }

        void OnDestroy()
        {
            if (Game) Game.Died -= OnPlayerDied;
        }

        void OnDisable()
        {
            StopAllCoroutines();
            spawning = false;
            toSpawn = 0;
        }
    }
}
