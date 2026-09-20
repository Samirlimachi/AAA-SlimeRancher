using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace SlimeRancher.Area1
{
    // Simulated controllers have no vibration motor. Real controller bindings are preserved.
    [DefaultExecutionOrder(-30000)]
    [RequireComponent(typeof(HapticImpulsePlayer))]
    public sealed class Area1SimulationHaptics : MonoBehaviour
    {
        HapticImpulsePlayer player;
        XRInputHapticImpulseProvider.InputSourceMode original;
        bool muted;
        void Awake() { player = GetComponent<HapticImpulsePlayer>(); original = player.hapticOutput.inputSourceMode; }
        void Update()
        {
            var output = player.hapticOutput;
            var action = output.inputActionReference ? output.inputActionReference.action : output.inputAction;
            var device = action?.activeControl?.device;
            bool simulated = device != null && device.layout == "XRSimulatedController";
            if (device == null)
                foreach (var input in InputSystem.devices)
                    if (input.layout == "XRSimulatedController") { simulated = true; break; }
            if (simulated == muted) return;
            output.inputSourceMode = simulated ? XRInputHapticImpulseProvider.InputSourceMode.Unused : original;
            muted = simulated;
        }
        void OnDisable() { if (player && muted) player.hapticOutput.inputSourceMode = original; muted = false; }
    }
}
