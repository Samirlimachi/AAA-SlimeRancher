using UnityEngine;
namespace SlimeRancherVR {
public enum RanchItemKind { PinkSlime, Carrot, PinkPlort, Chicken, ElderChicken, BlueSlime, EnemySlime }
[CreateAssetMenu(menuName="Slime Rancher/Objeto del rancho")]
public sealed class RanchItemData : ScriptableObject {
 public RanchItemKind kind;
 public string itemName;
 public Color color=Color.white;
 public GameObject prefab;
 [Min(1)] public int stackLimit=20;
 [Min(0)] public int saleValue;
 [Min(.05f)] public float radius=.18f;
}
}
