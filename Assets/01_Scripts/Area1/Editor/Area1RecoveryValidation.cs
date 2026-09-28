using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SlimeRancher.Area1.Editor
{
    // Batch: -executeMethod SlimeRancher.Area1.Editor.Area1RecoveryValidation.Run
    public static class Area1RecoveryValidation
    {
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/00_Scenes/AREA1.unity");
            var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var failures = new System.Collections.Generic.List<string>();
            foreach (var t in objects)
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    failures.Add("Missing script: " + t.name);
                if (PrefabUtility.IsPrefabAssetMissing(t.gameObject))
                    failures.Add("Missing prefab: " + t.name);
            }
            foreach (var mesh in objects.Select(t => t.GetComponent<MeshFilter>()).Where(m => m))
                if (!mesh.sharedMesh) failures.Add("Missing mesh: " + mesh.name);
            foreach (var mesh in objects.Select(t => t.GetComponent<SkinnedMeshRenderer>()).Where(m => m))
                if (!mesh.sharedMesh) failures.Add("Missing skinned mesh: " + mesh.name);
            foreach (var renderer in objects.Select(t => t.GetComponent<Renderer>()).Where(r => r))
                foreach (var material in renderer.sharedMaterials)
                    if (!material || !material.shader || ShaderUtil.ShaderHasError(material.shader))
                        failures.Add("Missing or invalid material/shader: " + renderer.name);
            var game = UnityEngine.Object.FindAnyObjectByType<SlimeRancherVR.RanchGame>();
            if (!game || game.catalog.Length != 7) failures.Add("Missing item catalog");
            else foreach (var data in game.catalog)
            {
                if (!data || !data.prefab) { failures.Add("Missing item prefab"); continue; }
                if (!data.prefab.GetComponent<Rigidbody>() ||
                    !data.prefab.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>())
                    failures.Add("Missing physics/XR grab: " + data.name);
                foreach (var filter in data.prefab.GetComponentsInChildren<MeshFilter>(true))
                    if (!filter.sharedMesh) failures.Add("Missing prefab mesh: " + data.name);
            }
            var vacuum = UnityEngine.Object.FindAnyObjectByType<SlimeRancherVR.SlimeVacuum>();
            if (!vacuum || !vacuum.muzzle || !vacuum.playerRoot) failures.Add("Missing vacuum references");
            if (failures.Count > 0)
            {
                foreach (var failure in failures) Debug.LogError("RECOVERY FAIL: " + failure);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw new Exception(string.Join("; ", failures));
            }
            Debug.Log("RECOVERY PASS: AREA1 loaded; scene meshes, prefabs, scripts, materials, catalog, XR grabs and vacuum references valid.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
