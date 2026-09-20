using UnityEngine;
using SlimeRancherVR;
namespace SlimeRancher.Area1
{
    public sealed class Area1WaterProjectile : MonoBehaviour
    {
        public Material material;
        bool hit;
        void Start() { Destroy(gameObject, 3); }
        void OnCollisionEnter(Collision collision)
        {
            if (hit) return; hit = true;
            var enemy = collision.collider.GetComponentInParent<Area1EnemySlime>();
            if (enemy) enemy.HitByWater();
            var item = collision.collider.GetComponentInParent<RanchItem>();
            if (item && item.enabled && !item.Body.isKinematic)
            {
                var direction = item.transform.position - transform.position;
                item.Body.AddForce(direction.normalized * 1.2f + Vector3.up * .4f, ForceMode.Impulse);
                item.graceUntil = Time.time + .5f;
            }
            for (int i = 0; i < 5; i++)
            {
                var drop = GameObject.CreatePrimitive(PrimitiveType.Sphere); drop.name = "Salpicadura";
                drop.transform.position = transform.position + Random.insideUnitSphere * .14f;
                drop.transform.localScale = Vector3.one * Random.Range(.035f, .065f);
                drop.layer = LayerMask.NameToLayer("Ignore Raycast");
                drop.GetComponent<Collider>().enabled = false;
                drop.GetComponent<Renderer>().sharedMaterial = material;
                Destroy(drop, .18f);
            }
            Destroy(gameObject);
        }
    }
}
