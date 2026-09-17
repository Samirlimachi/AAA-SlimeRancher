using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace SlimeRancher.Area1
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public sealed class Area1Sprint : MonoBehaviour
    {
        [SerializeField] ContinuousMoveProvider moveProvider;
        [SerializeField, Min(0.1f)] float walkSpeed = 2.5f;
        [SerializeField, Min(0.1f)] float sprintSpeed = 5f;
        InputAction sprint;
        public bool IsSprinting => sprint != null && sprint.IsPressed();

        void Awake()
        {
            if (moveProvider == null)
                moveProvider = GetComponentInChildren<ContinuousMoveProvider>(true);
            sprint = new InputAction("Area1 Sprint", InputActionType.Button);
            sprint.AddBinding("<XRController>{LeftHand}/primary2DAxisClick");
            sprint.AddBinding("<Keyboard>/leftShift");
        }
        void OnEnable() => sprint?.Enable();
        void Update()
        {
            if (moveProvider != null)
                {
                bool moving = moveProvider.leftHandMoveInput.ReadValue().sqrMagnitude > .03f;
                var game = SlimeRancherVR.RanchGame.Instance;
                bool running = game ? game.Sprint(IsSprinting && moving, Time.deltaTime) : IsSprinting;
                moveProvider.moveSpeed = running ? sprintSpeed : walkSpeed;
            }
        }
        void OnDisable()
        {
            sprint?.Disable();
            if (moveProvider != null) moveProvider.moveSpeed = walkSpeed;
        }
        void OnDestroy() => sprint?.Dispose();
    }
}

