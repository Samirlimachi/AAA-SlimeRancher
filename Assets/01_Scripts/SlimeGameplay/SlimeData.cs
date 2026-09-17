using UnityEngine;
namespace SlimeRancherVR {
[CreateAssetMenu(menuName="Slime Rancher/Slime Data")]
public sealed class SlimeData : ScriptableObject {
 public string displayName="Slime rosado";
 public Color color=new Color(1f,.12f,.45f);
 [Min(1)]public int capacity=20;
 [Min(.1f)]public float suctionRange=6f;
 [Range(5,60)]public float suctionHalfAngle=28f;
 [Min(.1f)]public float pullSpeed=5f;
 [Min(0)]public float hopStrength=1.7f;
}
}
