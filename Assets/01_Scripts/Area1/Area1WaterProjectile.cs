using UnityEngine;
using SlimeRancherVR;
namespace SlimeRancher.Area1
{
    public sealed class Area1WaterProjectile : MonoBehaviour
    {
        public Material material;
        public int damage = 1;
        bool hit;
        void Start() { Destroy(gameObject, 3); }
        void OnCollisionEnter(Collision collision)
        {
            if (hit) return; hit = true;
            var boss = collision.collider.GetComponentInParent<Area1EnemyBoss>();
            if (boss) boss.HitByWater(damage);
            var enemy = collision.collider.GetComponentInParent<Area1EnemySlime>();
            if (enemy) enemy.HitByWater(damage);
            var item = collision.collider.GetComponentInParent<RanchItem>();
            if (item && item.enabled && !item.Body.isKinematic)
            {
                var direction = item.transform.position - transform.position;
                item.Body.AddForce(direction.normalized * 1.2f + Vector3.up * .4f, ForceMode.Impulse);
                item.graceUntil = Time.time + .5f;
            }
            var contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Area1Audio.Play(b => b.aguaChoque, collision.contactCount > 0 ? contact.point : transform.position);
            Area1Effects.WaterSplash(collision.contactCount > 0 ? contact.point : transform.position,
                collision.contactCount > 0 ? contact.normal : Vector3.up);
            // Let the droplet trail fade out instead of vanishing with the shot.
            foreach (var trail in GetComponentsInChildren<ParticleSystem>())
            {
                trail.transform.SetParent(null, true);
                var main = trail.main;
                main.stopAction = ParticleSystemStopAction.Destroy;
                trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            Destroy(gameObject);
        }
    }
}
