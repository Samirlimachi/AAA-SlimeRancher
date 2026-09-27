using UnityEngine;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // Purple goo ball spat by the boss in an arc toward the player. Hurts on contact (body or head),
    // splashes on anything else. Created by Area1EnemyBoss.
    public sealed class Area1BossSpit : MonoBehaviour
    {
        public float damage = 12;
        bool done;
        static Material gooMaterial;

        public static void Launch(Vector3 from, Vector3 target, float damage, Transform owner)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Baba del jefe";
            ball.transform.position = from;
            ball.transform.localScale = Vector3.one * .32f;
            ball.layer = LayerMask.NameToLayer("Ignore Raycast");
            ball.GetComponent<Renderer>().sharedMaterial = GooMaterial();
            var collider = ball.GetComponent<Collider>();
            if (owner) foreach (var own in owner.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(collider, own);
            var body = ball.AddComponent<Rigidbody>();
            body.mass = .3f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Ballistic arc that lands on the target point.
            float time = Mathf.Clamp(Vector3.Distance(from, target) / 9f, .6f, 1.6f);
            body.linearVelocity = (target - from) / time - .5f * Physics.gravity * time;
            var spit = ball.AddComponent<Area1BossSpit>();
            spit.damage = damage;
            Area1Effects.AttachTrail(ball.transform, Area1Effects.Goo);
            Destroy(ball, 5);
        }

        static Material GooMaterial()
        {
            if (gooMaterial) return gooMaterial;
            gooMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            gooMaterial.SetColor("_BaseColor", new Color(.5f, .15f, .65f));
            gooMaterial.SetFloat("_Smoothness", .85f);
            return gooMaterial;
        }

        void Update()
        {
            // Head hits: the headset has no collider of its own.
            var camera = Camera.main;
            if (camera && Vector3.Distance(camera.transform.position, transform.position) < .45f) Hit(true, transform.position, Vector3.up);
        }

        void OnCollisionEnter(Collision collision)
        {
            var game = RanchGame.Instance;
            bool player = game && game.player && collision.transform.IsChildOf(game.player);
            var contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Hit(player, collision.contactCount > 0 ? contact.point : transform.position, collision.contactCount > 0 ? contact.normal : Vector3.up);
        }

        void Hit(bool player, Vector3 point, Vector3 normal)
        {
            if (done) return;
            done = true;
            var game = RanchGame.Instance;
            if (player && game)
            {
                float before = game.health;
                game.Damage(damage);
                if (game.health < before) Area1Effects.PlayerHit(transform.position);
                game.Notify("¡El jefe te escupió baba!");
            }
            Area1Effects.Splash(point, normal, Area1Effects.Goo, 1.3f);
            // Let the trail fade out instead of vanishing with the ball.
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
