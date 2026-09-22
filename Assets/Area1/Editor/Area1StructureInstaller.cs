using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using SlimeRancher.Area1;

namespace SlimeRancher.Area1.Editor
{
    public static class Area1StructureInstaller
    {
        const string SourceFolder = "Assets/04_Models/ESTRUCTURAS/";
        const string PrefabFolder = "Assets/Area1/Structures/";

        [MenuItem("Area1/Crear recolectores interactuables")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/00_Scenes/AREA1.unity")
                throw new System.Exception("Abre AREA1 antes de crear los recolectores.");

            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();
            var definitions = new[]
            {
                new Definition("Recolector Azul.glb", "Recolector Azul", new Vector3(-2.5f, 0, 2.5f), Area1StructureAction.GenerateWater, "Recolector azul: +1 agua."),
                new Definition("Recolector Rojo.glb", "Recolector Rojo", new Vector3(0, 0, 2.5f), Area1StructureAction.GeneratePinkPlort, "Recolector rojo: +1 plort rosa."),
                new Definition("Recolector verde.glb", "Recolector Verde", new Vector3(2.5f, 0, 2.5f), Area1StructureAction.GenerateCarrot, "Recolector verde: +1 zanahoria.")
            };

            foreach (var definition in definitions)
            {
                var sourcePath = FindModelPath(definition.fileName);
                var source = sourcePath == null ? null : AssetDatabase.LoadMainAssetAtPath(sourcePath) as GameObject;
                if (!source && sourcePath != null)
                    source = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<GameObject>().FirstOrDefault();
                if (!source)
                {
                    Debug.LogWarning("No se encontro el modelo: " + sourcePath);
                    continue;
                }

                var root = new GameObject(definition.objectName);
                SceneManager.MoveGameObjectToScene(root, scene);
                var visual = Object.Instantiate(source, root.transform);
                visual.name = "Modelo";
                root.name = definition.objectName;
                root.transform.position = definition.position;
                root.transform.rotation = Quaternion.identity;

                var bounds = CalculateBounds(root);
                var collider = root.GetComponent<BoxCollider>() ?? root.AddComponent<BoxCollider>();
                collider.center = root.transform.InverseTransformPoint(bounds.center);
                collider.size = root.transform.InverseTransformVector(bounds.size);
                var simpleInteractable = root.AddComponent<XRSimpleInteractable>();
                simpleInteractable.colliders.Clear();
                simpleInteractable.colliders.Add(collider);
                var interactable = root.AddComponent<Area1StructureInteractable>();
                interactable.action = definition.action;
                interactable.resourceAmount = 1;
                interactable.interactionMessage = definition.message;

                var oldInstances = FindSceneInstances(definition.objectName);
                foreach (var old in oldInstances)
                    if (old != root) Object.DestroyImmediate(old);

                var prefabPath = PrefabFolder + definition.objectName + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("Recolector creado: " + definition.objectName + " desde " + sourcePath);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Recolectores interactuables creados en AREA1.");
        }

        static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        static string FindModelPath(string fileName)
        {
            var exact = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName))
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => Path.GetFileName(path).Equals(fileName, System.StringComparison.OrdinalIgnoreCase) && path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase));
            return exact;
        }

        static GameObject[] FindSceneInstances(string objectName)
        {
            var result = new System.Collections.Generic.List<GameObject>();
            foreach (var transform in Object.FindObjectsByType<Transform>())
                if (transform.name == objectName && transform.gameObject.scene == SceneManager.GetActiveScene()) result.Add(transform.gameObject);
            return result.ToArray();
        }

        readonly struct Definition
        {
            public readonly string fileName, objectName, message;
            public readonly Vector3 position;
            public readonly Area1StructureAction action;
            public Definition(string fileName, string objectName, Vector3 position, Area1StructureAction action, string message)
            {
                this.fileName = fileName;
                this.objectName = objectName;
                this.position = position;
                this.action = action;
                this.message = message;
            }
        }
    }
}