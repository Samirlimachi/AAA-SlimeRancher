using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace SlimeRancher.Area1
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public sealed class Area1Sprint : MonoBehaviour
    {
        [SerializeField] ContinuousMoveProvider moveProvider;
        [SerializeField, Min(0.1f)] float walkSpeed = 2.5f;
        [SerializeField, Min(0.1f)] float sprintSpeed = 5f;
        [Tooltip("En el PC (simulador XR): cuántas veces más rápido va WASD mientras mantienes Shift izquierdo.")]
        [SerializeField, Min(1)] float keyboardSprintMultiplier = 2f;
        InputAction sprint;
        XRInteractionSimulator simulator;
        float simulatorWalkMultiplier = -1, nextSimulatorSearch;
        public bool IsSprinting => sprint != null && sprint.IsPressed();

        void Awake()
        {
            if (moveProvider == null)
                moveProvider = GetComponentInChildren<ContinuousMoveProvider>(true);
            sprint = new InputAction("Area1 Sprint", InputActionType.Button);
            sprint.AddBinding("<XRController>{LeftHand}/primary2DAxisClick");
            // OpenXR controllers name it thumbstickClicked; the usage matches any controller.
            sprint.AddBinding("<XRController>{LeftHand}/{Primary2DAxisClick}");
            sprint.AddBinding("<Keyboard>/leftShift");
        }
        void OnEnable() => sprint?.Enable();
        void Update()
        {
            // Joystick (VR): sprint = click the left stick while moving.
            bool stickMoving = moveProvider != null && moveProvider.leftHandMoveInput.ReadValue().sqrMagnitude > .03f;
            // Keyboard (PC simulator): WASD moves the simulated body, not the move provider.
            var keyboard = Keyboard.current;
            bool keysMoving = keyboard != null && (keyboard.wKey.isPressed || keyboard.aKey.isPressed || keyboard.sKey.isPressed || keyboard.dKey.isPressed);
            var game = SlimeRancherVR.RanchGame.Instance;
            bool wantsRun = IsSprinting && (stickMoving || keysMoving);
            bool running = game ? game.Sprint(wantsRun, Time.deltaTime) : wantsRun; // uses stamina
            if (moveProvider != null) moveProvider.moveSpeed = running && stickMoving ? sprintSpeed : walkSpeed;
            DriveSimulator(running && keysMoving);
        }

        void DriveSimulator(bool running)
        {
            if (!simulator && Time.unscaledTime >= nextSimulatorSearch)
            {
                nextSimulatorSearch = Time.unscaledTime + 1;
                simulator = FindAnyObjectByType<XRInteractionSimulator>();
                if (simulator) simulatorWalkMultiplier = simulator.bodyTranslateMultiplier;
            }
            if (!simulator) return;
            simulator.bodyTranslateMultiplier = running ? simulatorWalkMultiplier * keyboardSprintMultiplier : simulatorWalkMultiplier;
        }
        void OnDisable()
        {
            sprint?.Disable();
            if (moveProvider != null) moveProvider.moveSpeed = walkSpeed;
            if (simulator && simulatorWalkMultiplier > 0) simulator.bodyTranslateMultiplier = simulatorWalkMultiplier;
        }
        void OnDestroy() => sprint?.Dispose();
    }
}

