
namespace CharacterController
{
    using UnityEngine;

    public class PlayerCrouchingState : PlayerBaseState
    {
        private const float minHeightEpsilon = 0.01f;

        // Tracks whether the crouch button was previously held
        private bool wasCrouchPressed;

        public PlayerCrouchingState(
            FirstPersonController currentContext,
            PlayerStateFactory playerStateFactory
        ) : base(currentContext, playerStateFactory)
        {
        }

        public override void EnterState()
        {
            // Remember the current button state
            wasCrouchPressed = ctx.input.crouch;

            // Lower the camera
            ctx.targetCameraY = ctx.crouchingCameraHeight;

            // Instantly crouch if smooth crouching is disabled
            if (!ctx.enableSmoothCrouch)
            {
                SetCapsuleHeight(
                    ctx.crouchingCharacterControllerHeight
                );
            }
        }

        public override void UpdateState()
        {
            // Keep the camera at crouching height
            ctx.targetCameraY = ctx.crouchingCameraHeight;

            // Reset visual effects while crouching
            ctx.targetFov = ctx.normalFov;
            ctx.currentBobIntensity = 0f;
            ctx.currentBobSpeed = 0f;
            ctx.targetTilt = 0f;

            // Smoothly decrease the capsule height
            if (ctx.enableSmoothCrouch)
            {
                float targetHeight = Mathf.Max(
                    ctx.crouchingCharacterControllerHeight,
                    ctx.characterController.radius * 2f
                        + minHeightEpsilon
                );

                float newHeight = Mathf.MoveTowards(
                    ctx.characterController.height,
                    targetHeight,
                    ctx.crouchTransitionSpeed * Time.deltaTime
                );

                SetCapsuleHeight(newHeight);
            }

            // Handle movement
            HandleCrouchMovement();

            // Check for another crouch button press
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            // Restore standing height when smooth crouch is disabled
            if (!ctx.enableSmoothCrouch)
            {
                ctx.characterController.height =
                    ctx.standingCharacterControllerHeight;

                ctx.characterController.center =
                    ctx.standingCharacterControllerCenter;
            }

            // GroundedState handles smooth standing transitions
        }

        public override void CheckSwitchStates()
        {
            bool crouchPressed = ctx.input.crouch;

            // Detect a new button press
            bool justPressed =
                crouchPressed && !wasCrouchPressed;

            // Update the previous button state
            wasCrouchPressed = crouchPressed;

            // Toggle back to standing on the next press
            if (justPressed && !ctx.HasCeiling())
            {
                SwitchState(factory.Grounded());
                return;
            }

            // Remain crouched when the button is released
        }

        private void SetCapsuleHeight(float height)
        {
            // Prevent invalid capsule heights
            float minHeight =
                ctx.characterController.radius * 2f
                + minHeightEpsilon;

            height = Mathf.Max(height, minHeight);

            // Update capsule height
            ctx.characterController.height = height;

            // Keep the bottom of the capsule in place
            Vector3 standingCenter =
                ctx.standingCharacterControllerCenter;

            float standingHeight =
                ctx.standingCharacterControllerHeight;

            ctx.characterController.center = new Vector3(
                standingCenter.x,
                standingCenter.y
                    - standingHeight / 2f
                    + height / 2f,
                standingCenter.z
            );
        }

        private void HandleCrouchMovement()
        {
            // Read movement input
            Vector2 input = ctx.input.moveInput;

            // Calculate movement direction
            Vector3 targetMove =
                ctx.transform.right * input.x +
                ctx.transform.forward * input.y;

            // Prevent faster diagonal movement
            targetMove = Vector3.ClampMagnitude(
                targetMove,
                1f
            );

            // Calculate crouching velocity
            Vector3 targetVelocity =
                targetMove * ctx.crouchSpeed;

            // Apply acceleration and deceleration
            float accelRate =
                input.sqrMagnitude > 0.01f
                    ? ctx.groundAcceleration
                    : ctx.groundDeceleration;

            ctx.currentVelocity = Vector3.MoveTowards(
                ctx.currentVelocity,
                targetVelocity,
                accelRate * Time.deltaTime
            );

            // Apply movement
            Vector3 finalVelocity = ctx.currentVelocity;

            // Keep the character grounded
            finalVelocity.y = -5f;

            ctx.characterController.Move(
                finalVelocity * Time.deltaTime
            );
        }
    }
}
