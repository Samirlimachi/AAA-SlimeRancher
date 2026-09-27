using UnityEngine;

namespace SlimeRancher.Area1
{
    // Code-built particle effects in the Slime Rancher style: golden stars when a plort is sold,
    // water mist flowing into the vacuum, spray when shooting, droplet trail and splash on impact.
    // Textures are drawn procedurally and the shader lives in Resources, so nothing needs scene setup.
    public static class Area1Effects
    {
        public static readonly Color Gold = new Color(1, .85f, .3f), Water = new Color(.55f, .85f, 1f, .85f),
            Goo = new Color(.6f, .22f, .8f, .9f);
        static Material starMaterial, glowMaterial, waterMaterial;

        // ---- Plort sold: star burst, sparkle ring and a quick flash ----
        public static void PlortSold(Vector3 position)
        {
            var stars = Create("Estrellas plort", position, Star(), 1.4f);
            var main = stars.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.7f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(.08f, .17f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = new ParticleSystem.MinMaxGradient(Gold, Color.white);
            main.gravityModifier = .35f;
            Burst(stars, 26);
            var shape = stars.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35;
            shape.radius = .12f;
            shape.rotation = new Vector3(-90, 0, 0); // cone points up
            var rotation = stars.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-4, 4);
            FadeAndShrink(stars);
            stars.Play();

            var sparkle = Create("Brillo plort", position, Glow(), 1f);
            main = sparkle.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.03f, .06f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Gold);
            Burst(sparkle, 30);
            shape = sparkle.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = .05f;
            shape.rotation = new Vector3(-90, 0, 0); // horizontal ring
            FadeAndShrink(sparkle);
            sparkle.Play();

            var flash = Create("Destello plort", position, Glow(), .5f);
            main = flash.main;
            main.startLifetime = .3f;
            main.startSpeed = 0;
            main.startSize = .9f;
            main.startColor = new Color(1, .9f, .5f, .8f);
            Burst(flash, 1);
            var flashShape = flash.shape;
            flashShape.enabled = false;
            FadeAndShrink(flash);
            flash.Play();
        }

        // ---- Heal: pink hearts-like stars and soft glow rising around the player's feet ----
        public static readonly Color Love = new Color(1, .4f, .55f);

        public static void Heal(Vector3 position)
        {
            var rise = Create("Curacion", position, Star(), 1.6f);
            var main = rise.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.8f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(.07f, .13f);
            main.startColor = new ParticleSystem.MinMaxGradient(Love, Color.white);
            main.gravityModifier = -.15f; // floats upward
            Burst(rise, 22);
            var shape = rise.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = .35f;
            shape.rotation = new Vector3(-90, 0, 0);
            FadeAndShrink(rise);
            rise.Play();

            var glow = Create("Brillo curacion", position + Vector3.up * .1f, Glow(), .6f);
            main = glow.main;
            main.startLifetime = .45f;
            main.startSpeed = 0;
            main.startSize = 1.1f;
            main.startColor = new Color(Love.r, Love.g, Love.b, .7f);
            Burst(glow, 1);
            var glowShape = glow.shape;
            glowShape.enabled = false;
            FadeAndShrink(glow);
            glow.Play();
        }

        // ---- Hits: shards, a shockwave flash and cartoon stars where the blow lands ----
        public static readonly Color Hurt = new Color(1, .28f, .2f);

        public static void Impact(Vector3 position, Color color, float scale = 1)
        {
            var shards = Create("Choque", position, Glow(), .8f);
            var main = shards.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.25f, .45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f * scale, 4f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(.03f * scale, .07f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.gravityModifier = .5f;
            Burst(shards, 18);
            var shape = shards.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .05f * scale;
            FadeAndShrink(shards);
            shards.Play();

            var wave = Create("Onda de choque", position, Glow(), .4f);
            main = wave.main;
            main.startLifetime = .2f;
            main.startSpeed = 0;
            main.startSize = .7f * scale;
            main.startColor = new Color(color.r, color.g, color.b, .9f);
            Burst(wave, 1);
            var waveShape = wave.shape;
            waveShape.enabled = false;
            var waveSize = wave.sizeOverLifetime;
            waveSize.enabled = true;
            waveSize.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, .3f, 1, 1.2f));
            var waveColor = wave.colorOverLifetime;
            waveColor.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            waveColor.color = fade;
            wave.Play();

            var stars = Create("Estrellas de golpe", position, Star(), .9f);
            main = stars.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.45f, .7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f * scale, 2f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(.07f * scale, .12f * scale);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = new Color(1, .9f, .4f);
            main.gravityModifier = .2f;
            Burst(stars, 6);
            shape = stars.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = .05f;
            shape.rotation = new Vector3(-90, 0, 0);
            var spin = stars.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-6, 6);
            FadeAndShrink(stars);
            stars.Play();
        }

        // A slime (or any object) is hit by an enemy standing at `attacker`.
        public static void HitTarget(GameObject target, Vector3 attacker, float scale = 1)
        {
            if (!target) return;
            var center = target.transform.position;
            var toward = attacker - center;
            toward.y = 0;
            Impact(center + (toward.sqrMagnitude > .0001f ? toward.normalized * .25f : Vector3.zero) + Vector3.up * .1f, Hurt, scale);
            Area1HitFlash.Play(target);
        }

        // The player is hit: impact just in front of the eyes toward the enemy, and both controllers buzz.
        // The red screen flash is drawn by Area1HUD when health drops.
        public static void PlayerHit(Vector3 attacker, float scale = 1)
        {
            Area1Audio.Play2D(b => b.jugadorRecibeDanio);
            var camera = Camera.main;
            if (camera)
            {
                var head = camera.transform.position;
                var point = Vector3.MoveTowards(head, attacker, .55f);
                point.y = head.y - .25f;
                Impact(point, Hurt, .7f * scale);
            }
            if (!SlimeRancherVR.SlimeGameOptions.Haptics) return;
            foreach (var node in new[] { UnityEngine.XR.XRNode.LeftHand, UnityEngine.XR.XRNode.RightHand })
            {
                var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
                if (device.isValid) device.SendHapticImpulse(0, .75f, .18f);
            }
        }

        // ---- Suction: a persistent system that the vacuum drives every frame ----
        public static ParticleSystem CreateSuctionStream(Transform owner)
        {
            var stream = Create("Particulas succion agua", owner.position, WaterDrop(), 0, owner);
            var main = stream.main;
            main.loop = true;
            main.startLifetime = .35f;
            main.startSize = new ParticleSystem.MinMaxCurve(.025f, .06f);
            main.startColor = new ParticleSystem.MinMaxGradient(Water, new Color(.85f, .97f, 1, .9f));
            var emission = stream.emission;
            emission.rateOverTime = 0;
            var shape = stream.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6;
            shape.radius = .18f;
            var size = stream.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, .35f));
            return stream;
        }

        // Aims the stream from the water surface to the nozzle; rate 0 turns it off smoothly.
        public static void DriveSuctionStream(ParticleSystem stream, bool active, Vector3 source, Vector3 nozzle)
        {
            var emission = stream.emission;
            emission.rateOverTime = active ? 90 : 0;
            if (!active) return;
            var offset = nozzle - source;
            stream.transform.SetPositionAndRotation(source, Quaternion.LookRotation(offset.sqrMagnitude > .0001f ? offset : Vector3.up));
            var main = stream.main;
            main.startSpeed = offset.magnitude / main.startLifetime.constant;
            if (!stream.isPlaying) stream.Play();
            // Ripples where the water is being pulled from.
            if (Random.value < .12f) Ripple(source);
        }

        static void Ripple(Vector3 position)
        {
            var splash = Create("Onda agua", position, WaterDrop(), .6f);
            var main = splash.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.25f, .45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.4f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(.02f, .04f);
            main.startColor = Water;
            main.gravityModifier = .6f;
            Burst(splash, 6);
            var shape = splash.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = .08f;
            shape.rotation = new Vector3(-90, 0, 0);
            FadeAndShrink(splash);
            splash.Play();
        }

        // ---- Shooting: spray cone at the nozzle ----
        public static void WaterSpray(Vector3 position, Vector3 direction)
        {
            var spray = Create("Rocio disparo agua", position, WaterDrop(), .6f);
            spray.transform.rotation = Quaternion.LookRotation(direction);
            var main = spray.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.2f, .4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(.02f, .05f);
            main.startColor = Water;
            main.gravityModifier = .8f;
            Burst(spray, 14);
            var shape = spray.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14;
            shape.radius = .02f;
            FadeAndShrink(spray);
            spray.Play();
        }

        // Droplet trail that follows a flying water shot.
        public static void AttachWaterTrail(Transform projectile) => AttachTrail(projectile, Water);

        // Droplet trail in any colour (boss spit uses purple goo).
        public static void AttachTrail(Transform projectile, Color color)
        {
            var trail = Create("Estela agua", projectile.position, WaterDrop(), 0, projectile);
            var main = trail.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.2f, .35f);
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(.03f, .06f);
            main.startColor = color;
            main.gravityModifier = .3f;
            var emission = trail.emission;
            emission.rateOverTime = 0;
            emission.rateOverDistance = 40;
            var shape = trail.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .03f;
            FadeAndShrink(trail);
            trail.Play();
        }

        // Splash where a water shot lands.
        public static void WaterSplash(Vector3 position, Vector3 normal) => Splash(position, normal, Water, 1);

        public static void Splash(Vector3 position, Vector3 normal, Color color, float scale)
        {
            var splash = Create("Salpicadura agua", position, WaterDrop(), .8f);
            splash.transform.rotation = Quaternion.LookRotation(normal.sqrMagnitude > .0001f ? normal : Vector3.up);
            var main = splash.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.3f, .6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * scale, 3f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(.03f * scale, .07f * scale);
            main.startColor = color;
            main.gravityModifier = 1f;
            Burst(splash, 22);
            var shape = splash.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 55;
            shape.radius = .04f;
            FadeAndShrink(splash);
            splash.Play();
        }

        // ---- Building blocks ----
        // One-shot systems (duration > 0) destroy themselves; duration 0 means driven/looping by the caller.
        static ParticleSystem Create(string name, Vector3 position, Material material, float duration, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.transform.position = position;
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = duration > 0 ? duration : 1;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            if (duration > 0) main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = system.emission;
            emission.rateOverTime = 0;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        static void Burst(ParticleSystem system, int count)
        {
            var emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
        }

        static void FadeAndShrink(ParticleSystem system)
        {
            var colors = system.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .55f), new GradientAlphaKey(0, 1) });
            colors.color = gradient;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, .2f));
        }

        static Material Star() => starMaterial ? starMaterial : starMaterial = MakeMaterial(StarTexture(), true);
        static Material Glow() => glowMaterial ? glowMaterial : glowMaterial = MakeMaterial(DotTexture(), true);
        static Material WaterDrop() => waterMaterial ? waterMaterial : waterMaterial = MakeMaterial(DotTexture(), false);

        static Material MakeMaterial(Texture texture, bool additive)
        {
            var material = new Material(Shader.Find("Area1/Particle")) { mainTexture = texture };
            material.SetFloat("_SrcBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Additive", additive ? 1 : 0);
            return material;
        }

        // Soft round dot (water droplets, glow).
        static Texture2D DotTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = Mathf.Clamp01(1 - d);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * a * a));
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        // Five-point star with a soft glow around it.
        static Texture2D StarTexture()
        {
            const int size = 64, samples = 3;
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI / 2 + i * Mathf.PI / 5;
                float radius = i % 2 == 0 ? .48f : .2f;
                points[i] = new Vector2(.5f + Mathf.Cos(angle) * radius, .5f + Mathf.Sin(angle) * radius);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < samples; sy++)
                        for (int sx = 0; sx < samples; sx++)
                            if (Inside(points, new Vector2((x + (sx + .5f) / samples) / size, (y + (sy + .5f) / samples) / size))) inside++;
                    float star = (float)inside / (samples * samples);
                    float glow = Mathf.Clamp01(1 - Vector2.Distance(new Vector2((x + .5f) / size, (y + .5f) / size), new Vector2(.5f, .5f)) * 2.2f) * .35f;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Max(star, glow)));
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        static bool Inside(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                    inside = !inside;
            return inside;
        }
    }
}
