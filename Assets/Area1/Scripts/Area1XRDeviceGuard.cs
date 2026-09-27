using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace SlimeRancher.Area1
{
    // XRI auto-creates the XR Interaction Simulator in the Editor even when a real headset is running.
    // The simulator then removes the real HMD from the Input System and adds fake controllers, which
    // breaks head tracking (the view turns the wrong way) and steals the controller buttons.
    // With a real headset active we skip the simulator; without one it keeps working as before.
    // If the simulator still got in first, it is destroyed and the real HMD is added back, so the
    // camera follows the head position again (walking around the room), not only its rotation.
    public sealed class Area1XRDeviceGuard : MonoBehaviour
    {
        const string SettingsField = "m_AutomaticallyInstantiateSimulatorPrefab";
        static ScriptableObject settings;
        static bool restoreSetting;
        // Real HMDs seen so far; the simulator removes them from the Input System.
        static readonly List<UnityEngine.InputSystem.InputDevice> realHeadsets = new List<UnityEngine.InputSystem.InputDevice>();

        // OpenXR only starts a loader when a headset is connected (otherwise it fails with
        // XR_ERROR_FORM_FACTOR_UNAVAILABLE), so a running loader already means a real headset,
        // even before XRSettings.isDeviceActive turns true on the first frames.
        public static bool RealHeadset
        {
            get
            {
                var general = XRGeneralSettings.Instance;
                var loader = general && general.Manager ? general.Manager.activeLoader : null;
                if (!loader) return false;
                if (XRSettings.isDeviceActive) return true;
                var display = loader.GetLoadedSubsystem<XRDisplaySubsystem>();
                return display != null && display.running;
            }
        }

        // True only while simulated controllers exist, so input readers can pick the right source.
        public static bool SimulatorActive
        {
            get
            {
                foreach (var device in InputSystem.devices)
                    if (device is XRSimulatedController) return true;
                return false;
            }
        }

        // Runs after XR has started and before XRI's AfterSceneLoad simulator loader.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            RememberHeadsets();
            InputSystem.onDeviceChange += (device, change) =>
            {
                if (change == InputDeviceChange.Added) RememberHeadsets();
            };
            if (RealHeadset)
            {
                settings = Resources.Load<ScriptableObject>("XRDeviceSimulatorSettings");
                var field = settings ? settings.GetType().GetField(SettingsField, BindingFlags.Instance | BindingFlags.NonPublic) : null;
                if (field != null && (bool)field.GetValue(settings))
                {
                    field.SetValue(settings, false);
                    restoreSetting = true;
                }
            }
            var guard = new GameObject("Area1 XR Device Guard");
            guard.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(guard);
            guard.AddComponent<Area1XRDeviceGuard>();
        }

        float nextCheck;
        float restoreHeadsetAt = -1;

        static void RememberHeadsets()
        {
            foreach (var device in InputSystem.devices)
                if (device is XRHMD && !(device is XRSimulatedHMD) && !realHeadsets.Contains(device)) realHeadsets.Add(device);
        }

        void Start()
        {
            // The simulator loader has already run; put the project setting back for simulator sessions.
            if (!restoreSetting || !settings) return;
            settings.GetType().GetField(SettingsField, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(settings, true);
            restoreSetting = false;
        }

        void Update()
        {
            // One frame after destroying the simulator (its OnDisable has run), give the HMD back.
            if (restoreHeadsetAt >= 0 && Time.unscaledTime >= restoreHeadsetAt)
            {
                restoreHeadsetAt = -1;
                foreach (var headset in realHeadsets)
                    if (headset != null && !headset.added) InputSystem.AddDevice(headset);
            }
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .25f;
            if (!RealHeadset) return;
            // Fallback for a simulator created before the guard could stop it.
            var simulator = FindAnyObjectByType<XRInteractionSimulator>();
            if (!simulator) return;
            Debug.Log("Visor VR real detectado: se quita el XR Interaction Simulator y se restaura el seguimiento del visor.");
            Destroy(simulator.gameObject);
            restoreHeadsetAt = Time.unscaledTime + .05f;
        }

        void OnDestroy()
        {
            if (!restoreSetting || !settings) return;
            settings.GetType().GetField(SettingsField, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(settings, true);
            restoreSetting = false;
        }
    }
}
