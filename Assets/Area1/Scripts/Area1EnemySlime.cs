using UnityEngine;
using SlimeRancherVR;
namespace SlimeRancher.Area1
{
    [RequireComponent(typeof(RanchItem))]
    public sealed class Area1EnemySlime : MonoBehaviour
    {
        public int waterHits = 3;
        void OnCollisionStay(Collision collision)
        {
            var game = RanchGame.Instance;
            var item = GetComponent<RanchItem>();
            if (!game || !game.player || !item.enabled || item.Consumed) return;
            if (collision.transform.IsChildOf(game.player))
            {
                game.Damage(10);
                game.Notify("¡Slime enemigo! Usa agua para neutralizarlo.");
            }
        }
        public void HitByWater()
        {
            if (--waterHits > 0) return;
            GetComponent<RanchItem>().Consume();
            if (RanchGame.Instance) RanchGame.Instance.Notify("Slime enemigo neutralizado.");
        }
    }
}
