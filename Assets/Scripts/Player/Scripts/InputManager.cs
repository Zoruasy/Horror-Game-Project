namespace CharacterController
{
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class InputManager : MonoBehaviour, IInputManager
    {
        InputAction moveAction;
        InputAction jumpAction;
        InputAction sprintAction;
        InputAction crouchAction;
        InputAction interactAction;

        void Awake()
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogWarning("InputManager: no project-wide input actions set in Project Settings > Input System.");
                return;
            }

            moveAction = actions.FindAction("Player/Move");
            jumpAction = actions.FindAction("Player/Jump");
            sprintAction = actions.FindAction("Player/Sprint");
            crouchAction = actions.FindAction("Player/Crouch");
            interactAction = actions.FindAction("Player/Interact");
        }

        public Vector2 moveInput =>
            moveAction != null ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;

        // Multiply by a small delta factor to match the feeling of the old Input.GetAxis
        public Vector2 lookInput =>
            Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.05f : Vector2.zero;

        public bool jump => jumpAction != null && jumpAction.IsPressed();

        public bool climbing => interactAction != null && interactAction.WasPressedThisFrame();

        public bool sprint => sprintAction != null && sprintAction.IsPressed();

        public bool crouch => crouchAction != null && crouchAction.IsPressed();

        public bool slide => crouchAction != null && crouchAction.IsPressed();
    }
}
