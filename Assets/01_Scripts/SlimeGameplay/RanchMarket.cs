using UnityEngine;
namespace SlimeRancherVR {
[RequireComponent(typeof(BoxCollider))]
public sealed class RanchMarket : MonoBehaviour {
 public bool Sell(RanchItem item){if(!RanchGame.Instance||!item||!item.data||item.data.kind!=RanchItemKind.PinkPlort||item.Consumed)return false;int value=item.data.saleValue;if(!item.Consume())return false;RanchGame.Instance.coins+=value;RanchGame.Instance.Notify("Plort vendido: +"+value+" monedas");return true;}
 void OnTriggerEnter(Collider other){Sell(other.GetComponentInParent<RanchItem>());}
}
}
