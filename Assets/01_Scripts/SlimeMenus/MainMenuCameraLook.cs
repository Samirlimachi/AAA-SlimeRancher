using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace SlimeRancherVR
{
    public sealed class MainMenuCameraLook : MonoBehaviour
    {
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] float keyboardTurnSpeed = 70f;
        [SerializeField] float maxPitch = 35f;
        float pitch;
        bool mouseCaptured;

        IEnumerator Start()
        {
            yield return null;
            foreach (var simulator in FindObjectsByType<XRInteractionSimulator>())
            {
                simulator.enabled = false;
                simulator.enabled = true;
            }
        }

        void Update()
        {
            if (XRSettings.isDeviceActive || HeadTracked()) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                ReleaseMouse();
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                mouseCaptured = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (!mouseCaptured) return;

            if (mouse != null)
            {
                var delta = mouse.delta.ReadValue();
                transform.Rotate(Vector3.up, delta.x * mouseSensitivity, UnityEngine.Space.World);
                pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -maxPitch, maxPitch);
            }

            float turn = 0;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed) turn -= 1;
                if (keyboard.rightArrowKey.isPressed) turn += 1;
            }
            transform.Rotate(Vector3.up, turn * keyboardTurnSpeed * Time.unscaledDeltaTime, UnityEngine.Space.World);
            transform.localRotation = Quaternion.Euler(pitch, transform.localEulerAngles.y, 0);
        }

        static bool HeadTracked()
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked) && tracked;
        }

        void ReleaseMouse()
        {
            mouseCaptured = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnDisable() => ReleaseMouse();
    }
}