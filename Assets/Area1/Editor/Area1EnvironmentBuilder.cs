using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SlimeRancherVR;

namespace SlimeRancher.Area1.Editor
{
    // Enlarges AREA1 and decorates it in a Slime Rancher mood: a ring of big boulders (PIEDRA 1) along
    // the (now invisible) boundary walls, rolling green hills and outer ground beyond them, rock clusters
    // inside, 3D grass tufts and flowers, and soft distance fog. Gameplay spots (spawn, pond, collector,
    // boards, shops, respawn points) are kept clear. Everything goes under "Decoracion AREA1".
    // Runs by itself once when AREA1 is open without decoration; the menu item rebuilds it.
    [InitializeOnLoad]
    public static class Area1EnvironmentBuilder
    {
        const string ScenePath = "Assets/00_Scenes/AREA1.unity";
        const string RootName = "Decoracion AREA1";
        const string RockFolder = "Assets/04_Models/PIEDRA 1";
        const string MaterialFolder = "Assets/Area1/Materiales";
        const float HalfSize = 35;          // play area is 70 x 70 m (was 40 x 40)
        const float GrassTileMeters = 4;    // ground texture repeats every 4 m

        static Area1EnvironmentBuilder() => EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path != ScenePath) return;
            if (!GameObject.Find(RootName)) Build();
        };

        [MenuItem("Area1/Decorar y agrandar entorno")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath || Application.isPlaying)
            {
                Debug.LogWarning("Abre AREA1 fuera de Play para decorar el entorno.");
                return;
            }
            Directory.CreateDirectory(MaterialFolder);
            var old = GameObject.Find(RootName);
            if (old) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;
            var random = new System.Random(2026);

            Enlarge();
            var blocked = BlockedAreas();
            var rock = RockAsset();
            var rockMaterial = Textured("Piedra", RockFolder, .15f);
            var grassMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Area1/Suelo.mat");

            // Outer ground and hills beyond the walls (visual only).
            var outer = Primitive(PrimitiveType.Cube, "Suelo exterior", root, new Vector3(0, -.27f, 0), new Vector3(HalfSize * 2 + 60, .5f, HalfSize * 2 + 60), grassMaterial);
            Object.DestroyImmediate(outer.GetComponent<Collider>());
            var hills = new GameObject("Colinas").transform;
            hills.SetParent(root, false);
            for (int i = 0; i < 22; i++)
            {
                float angle = i / 22f * Mathf.PI * 2 + (float)random.NextDouble() * .2f;
                float distance = HalfSize + 9 + (float)random.NextDouble() * 10;
                var position = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                float width = 16 + (float)random.NextDouble() * 14;
                float height = 5 + (float)random.NextDouble() * 7;
                var hill = Primitive(PrimitiveType.Sphere, "Colina", hills, position, new Vector3(width, height, width * (.8f + (float)random.NextDouble() * .4f)), grassMaterial);
                Object.DestroyImmediate(hill.GetComponent<Collider>());
            }

            // Boulder ring along the boundary.
            var ring = new GameObject("Rocas del borde").transform;
            ring.SetParent(root, false);
            if (rock)
            {
                const float step = 11;
                for (int side = 0; side < 4; side++)
                    for (float t = -HalfSize; t < HalfSize; t += step)
                    {
                        float along = t + (float)random.NextDouble() * 3;
                        float inset = HalfSize - 1.2f - (float)random.NextDouble() * 1.5f;
                        var position = side == 0 ? new Vector3(along, 0, -inset) : side == 1 ? new Vector3(inset, 0, along)
                            : side == 2 ? new Vector3(-along, 0, inset) : new Vector3(-inset, 0, -along);
                        if (Blocked(blocked, position, 3)) continue;
                        Rock(rock, rockMaterial, ring, position, 3.5f + (float)random.NextDouble() * 2.5f, random, true);
                    }

                // A few clusters inside the play area, away from the middle.
                var clusters = new GameObject("Grupos de rocas").transform;
                clusters.SetParent(root, false);
                int placed = 0;
                for (int attempt = 0; attempt < 200 && placed < 6; attempt++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2;
                    float distance = 11 + (float)random.NextDouble() * (HalfSize - 16);
                    var center = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                    if (Blocked(blocked, center, 4) || InWaveSpawn(center, 3)) continue;
                    placed++;
                    int count = 2 + random.Next(2);
                    for (int r = 0; r < count; r++)
                    {
                        var offset = new Vector3((float)random.NextDouble() - .5f, 0, (float)random.NextDouble() - .5f) * 2.6f;
                        Rock(rock, rockMaterial, clusters, center + offset, (r == 0 ? 1.4f : .6f) + (float)random.NextDouble() * .8f, random, r == 0);
                    }
                }
            }
            else Debug.LogWarning("No se encontro el modelo de piedra en " + RockFolder);

            // Grass tufts and flowers, combined into a few meshes.
            var tuftTexture = TuftTexture("PastoMata", false);
            var flowerTexture = TuftTexture("PastoFlores", true);
            var tuftMaterial = Cutout("PastoMata", tuftTexture);
            var flowerMaterial = Cutout("PastoFlores", flowerTexture);
            var tufts = new List<(Vector3, float)>();
            var flowers = new List<(Vector3, float)>();
            for (int i = 0; i < 900; i++)
            {
                var position = new Vector3(((float)random.NextDouble() * 2 - 1) * (HalfSize - 2), 0, ((float)random.NextDouble() * 2 - 1) * (HalfSize - 2));
                if (Blocked(blocked, position, 1.2f)) continue;
                float size = .7f + (float)random.NextDouble() * .7f;
                if (random.NextDouble() < .18) flowers.Add((position, size)); else tufts.Add((position, size));
            }
            Combine(root, "Matas de pasto", tuftMaterial, tufts, random);
            Combine(root, "Flores", flowerMaterial, flowers, random);

            // Soft pastel distance fog, like the Slime Rancher horizon.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.74f, .86f, .95f);
            RenderSettings.fogStartDistance = 30;
            RenderSettings.fogEndDistance = 130;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("AREA1: entorno agrandado a " + HalfSize * 2 + " m y decorado (rocas, colinas, pasto, flores, neblina). Guarda la escena con Ctrl+S.");
        }

        // Floor and boundary walls to the new size; walls stay as invisible colliders behind the rocks.
        static void Enlarge()
        {
            var floor = GameObject.Find("Suelo AREA1");
            if (floor) floor.transform.localScale = new Vector3(HalfSize * 2, floor.transform.localScale.y, HalfSize * 2);
            var walls = new[]
            {
                ("Limite 1", new Vector3(-HalfSize, 1.5f, 0), new Vector3(.4f, 3, HalfSize * 2)),
                ("Limite 2", new Vector3(HalfSize, 1.5f, 0), new Vector3(.4f, 3, HalfSize * 2)),
                ("Limite 3", new Vector3(0, 1.5f, -HalfSize), new Vector3(HalfSize * 2, 3, .4f)),
                ("Limite 4", new Vector3(0, 1.5f, HalfSize), new Vector3(HalfSize * 2, 3, .4f))
            };
            foreach (var (name, position, scale) in walls)
            {
                var wall = GameObject.Find(name);
                if (!wall) continue;
                wall.transform.position = position;
                wall.transform.localScale = scale;
                var renderer = wall.GetComponent<Renderer>();
                if (renderer) renderer.enabled = false;
            }
            var ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/Area1/Suelo.mat");
            if (ground)
            {
                var tiling = Vector2.one * (HalfSize * 2 / GrassTileMeters);
                ground.SetTextureScale("_BaseMap", tiling);
                ground.SetTextureScale("_MainTex", tiling);
                EditorUtility.SetDirty(ground);
            }
        }

        // Places to keep free (x, z, radius): gameplay objects and the player start.
        static List<Vector3> BlockedAreas()
        {
            var list = new List<Vector3> { new Vector3(0, -4, 4), new Vector3(0, 0, 7) };
            void Add(Component c, float radius) { if (c) list.Add(new Vector3(c.transform.position.x, c.transform.position.z, radius)); }
            foreach (var c in Object.FindObjectsByType<Area1WaterSource>()) Add(c, 6);
            foreach (var c in Object.FindObjectsByType<Area1PlortCollector>()) Add(c, 3);
            foreach (var c in Object.FindObjectsByType<Area1CollectorTiers>()) Add(c, 3);
            foreach (var c in Object.FindObjectsByType<Area1WaveBoard>()) Add(c, 4);
            foreach (var c in Object.FindObjectsByType<Area1FoodShop>()) Add(c, 4.5f);
            foreach (var c in Object.FindObjectsByType<Area1UpgradeShop>()) Add(c, 4.5f);
            foreach (var c in Object.FindObjectsByType<Area1RespawnPoint>()) Add(c, 1.5f);
            foreach (var c in Object.FindObjectsByType<SlimeVacuum>()) Add(c, 2.5f);
            return list;
        }

        // Keep rocks out of the area where wave enemies appear, so none spawns inside a boulder.
        static bool InWaveSpawn(Vector3 position, float margin) => Object.FindObjectsByType<Area1WaveBoard>().Any(board =>
            position.x > board.spawnX.x - margin && position.x < board.spawnX.y + margin &&
            position.z > board.spawnZ.x - margin && position.z < board.spawnZ.y + margin);

        static bool Blocked(List<Vector3> blocked, Vector3 position, float radius) =>
            blocked.Any(b => new Vector2(position.x - b.x, position.z - b.y).magnitude < b.z + radius);

        static void Rock(GameObject asset, Material material, Transform parent, Vector3 position, float size, System.Random random, bool shadows)
        {
            var holder = new GameObject("Roca").transform;
            holder.SetParent(parent, false);
            var model = (GameObject)Object.Instantiate(asset, holder, false);
            model.name = asset.name;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
            // Fit: tallest side = size, base slightly buried so it sits in the grass.
            var bounds = Bounds(holder, model);
            float scale = size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            model.transform.localScale *= scale;
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
            holder.SetPositionAndRotation(position + Vector3.down * size * .12f,
                Quaternion.Euler(((float)random.NextDouble() - .5f) * 10, (float)random.NextDouble() * 360, ((float)random.NextDouble() - .5f) * 10));
            holder.localScale = new Vector3(1 + ((float)random.NextDouble() - .5f) * .5f, 1, 1 + ((float)random.NextDouble() - .5f) * .5f);
            // Cheap box collider instead of a 90k-vertex mesh collider.
            var world = Bounds(holder, model, true);
            var collider = holder.gameObject.AddComponent<BoxCollider>();
            collider.center = holder.InverseTransformPoint(world.center);
            collider.size = Vector3.Scale(world.size, new Vector3(1 / holder.lossyScale.x, 1 / holder.lossyScale.y, 1 / holder.lossyScale.z)) * .85f;
            GameObjectUtility.SetStaticEditorFlags(holder.gameObject, StaticEditorFlags.BatchingStatic);
            foreach (Transform child in holder.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.BatchingStatic);
        }

        static Bounds Bounds(Transform space, GameObject model, bool world = false)
        {
            var result = new Bounds();
            bool first = true;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.sharedMesh) continue;
                var b = filter.sharedMesh.bounds;
                for (int n = 0; n < 8; n++)
                {
                    var p = filter.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((n & 1) == 0 ? -1 : 1, (n & 2) == 0 ? -1 : 1, (n & 4) == 0 ? -1 : 1)));
                    if (!world) p = space.InverseTransformPoint(p);
                    if (first) { result = new Bounds(p, Vector3.zero); first = false; }
                    else result.Encapsulate(p);
                }
            }
            return result;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (material) go.GetComponent<Renderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static GameObject RockAsset()
        {
            var path = AssetDatabase.FindAssets("t:Model", new[] { RockFolder }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            return path == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static Material Textured(string name, string folder, float smoothness)
        {
            var material = LoadOrCreate(name, "Universal Render Pipeline/Lit");
            var texture = AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>).FirstOrDefault();
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        // Alpha-tested, double-sided URP Lit for grass cards.
        static Material Cutout(string name, Texture2D texture)
        {
            var material = LoadOrCreate(name, "Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_AlphaClip", 1);
            material.SetFloat("_Cutoff", .45f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cull", 0);
            material.SetFloat("_Smoothness", 0);
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material LoadOrCreate(string name, string shader)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        // A fan of cartoon blades (optionally with flowers at the tips) on a transparent background.
        static Texture2D TuftTexture(string name, bool flowers)
        {
            string path = MaterialFolder + "/" + name + ".png";
            const int size = 128;
            var random = new System.Random(flowers ? 5 : 3);
            var pixels = new Color[size * size];
            var deep = new Color(.2f, .47f, .15f);
            var light = new Color(.62f, .86f, .32f);
            for (int b = 0; b < 16; b++)
            {
                float baseX = size * (.35f + (float)random.NextDouble() * .3f);
                float tipX = size * (.08f + (float)random.NextDouble() * .84f);
                float tipY = size * (.55f + (float)random.NextDouble() * .42f);
                float width = 3 + (float)random.NextDouble() * 3;
                for (int y = 0; y < (int)tipY; y++)
                {
                    float t = y / tipY;
                    float cx = Mathf.Lerp(baseX, tipX, t * t);
                    float half = width * (1 - t);
                    for (int x = Mathf.Max(0, (int)(cx - half - 1)); x <= Mathf.Min(size - 1, (int)(cx + half + 1)); x++)
                    {
                        float coverage = Mathf.Clamp01(half + .5f - Mathf.Abs(x + .5f - cx));
                        if (coverage <= 0) continue;
                        var c = Color.Lerp(deep, light, t);
                        c.a = 1;
                        int i = y * size + x;
                        pixels[i] = Color.Lerp(pixels[i], c, coverage);
                        pixels[i].a = Mathf.Max(pixels[i].a, coverage);
                    }
                }
                if (flowers && b % 4 == 0)
                {
                    var petal = new[] { Color.white, new Color(1, .86f, .3f), new Color(1, .55f, .75f), new Color(.65f, .8f, 1) }[random.Next(4)];
                    for (int dy = -7; dy <= 7; dy++)
                        for (int dx = -7; dx <= 7; dx++)
                        {
                            int x = (int)tipX + dx, y = (int)tipY + dy - 2;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            if (x < 0 || y < 0 || x >= size || y >= size || d > 6.5f) continue;
                            var c = d < 2.5f ? new Color(1, .8f, .2f) : petal;
                            pixels[y * size + x] = new Color(c.r, c.g, c.b, 1);
                        }
                }
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.mipMapsPreserveCoverage = true;
            importer.alphaTestReferenceValue = .45f;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Three crossed quads per tuft, merged into one mesh (few draw calls for hundreds of tufts).
        static void Combine(Transform root, string name, Material material, List<(Vector3 position, float size)> items, System.Random random)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            foreach (var (position, size) in items)
            {
                float yaw = (float)random.NextDouble() * 180;
                float width = .5f * size, height = .42f * size;
                for (int q = 0; q < 3; q++)
                {
                    var right = Quaternion.Euler(0, yaw + q * 60, 0) * Vector3.right * width * .5f;
                    int start = vertices.Count;
                    vertices.Add(position - right);
                    vertices.Add(position + right);
                    vertices.Add(position + right + Vector3.up * height);
                    vertices.Add(position - right + Vector3.up * height);
                    uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                    for (int n = 0; n < 4; n++) normals.Add(Vector3.up); // lit like the ground below
                    triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
                }
            }
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            string path = MaterialFolder + "/" + name.Replace(" ", "") + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }
    }
}
