using UnityEngine;
namespace SlimeRancherVR {
public sealed class RanchHazard : MonoBehaviour {
 void OnTriggerStay(Collider col){var game=RanchGame.Instance;if(game&&col.transform.IsChildOf(game.player))game.Damage(25);}
}
}
