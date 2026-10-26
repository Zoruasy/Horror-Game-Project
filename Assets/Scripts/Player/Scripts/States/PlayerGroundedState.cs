
namespace CharacterController
{
    using UnityEngine;

    public class PlayerGroundedState : PlayerBaseState
    {
        // Tracks the previous crouch button state
        private bool wasCrouchPressed;

        public PlayerGroundedState(
            FirstPersonController currentContext,
            PlayerStateFactory playerStateFactory
        ) : base(currentContext, playerStateFactory)
        {
        }

        public override void EnterState()
        {
            // Keep the player grounded
            ctx.moveDirection.y = -2f;

            // Prevent the same button press from triggering crouch again
            wasCrouchPressed = ctx.input.crouch;
        }

        public override void UpdateState()
        {
            // Check state transitions before handling movement
            if (CheckAndSwitchState())
            {
                return;
            }

            // Restore standing camera height
            ctx.targetCameraY = ctx.standingCameraHeight;

            // Check whether the player is sprinting
            bool isSprinting =
                ctx.input.sprint &&
                ctx.input.moveInput.y > 0f;

            float speed = isSprinting
                ? ctx.sprintSpeed
                : ctx.walkSpeed;

            // Update visual effects
            ctx.targetFov = isSprinting
                ? ctx.sprintFov
                : ctx.normalFov;

            ctx.currentBobIntensity =
                ctx.bobAmount * (isSprinting ? 1.5f : 1f);

            ctx.currentBobSpeed =
                ctx.bobSpeed * (isSprinting ? 1.3f : 1f);

            ctx.targetTilt = 0f;

            // Restore standing capsule height
            RestoreStandingHeight();

            // Handle grounded movement
            HandleGroundMovement(speed);
        }

        public override void ExitState()
        {
        }

        public override void CheckSwitchStates()
        {
            CheckAndSwitchState();
        }

        private bool CheckAndSwitchState()
        {
            bool crouchPressed = ctx.input.crouch;

            // Detect a new crouch button press
            bool justPressed =
                crouchPressed && !wasCrouchPressed;

            // Store the button state for the next frame
            wasCrouchPressed = crouchPressed;

            // Enter crouching on a new button press
            if (justPressed && ctx.isGrounded)
            {
                SwitchState(factory.Crouching());
                return true;
            }

            // Jump while grounded
            if (ctx.input.jump && ctx.isGrounded)
            {
                SwitchState(factory.Jumping());
                return true;
            }

            // Enter falling state when leaving the ground
            if (!ctx.isGrounded)
            {
                SwitchState(factory.Fall());
                return true;
            }

            return false;
        }

        private void RestoreStandingHeight()
        {
            float targetHeight =
                ctx.standingCharacterControllerHeight;

            // Smoothly restore the standing capsule height
            if (ctx.enableSmoothCrouch)
            {
                float newHeight = Mathf.MoveTowards(
                    ctx.characterController.height,
                    targetHeight,
                    ctx.crouchTransitionSpeed * Time.deltaTime
                );

                SetCapsuleHeight(newHeight);
            }
            else
            {
                // Instantly restore the standing capsule
                ctx.characterController.height =
                    targetHeight;

                ctx.characterController.center =
                    ctx.standingCharacterControllerCenter;
            }
        }

        private void SetCapsuleHeight(float height)
        {
            // Keep the capsule height valid
            float minimumHeight =
                ctx.characterController.radius * 2f + 0.01f;

            height = Mathf.Max(height, minimumHeight);

            // Update the capsule height
            ctx.characterController.height = height;

            // Keep the capsule bottom at the same position
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

        private void HandleGroundMovement(float speed)
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

            // Calculate target movement velocity
            Vector3 targetVelocity =
                targetMove * speed;

            // Apply acceleration or deceleration
            float accelRate =
                input.sqrMagnitude > 0.01f
                    ? ctx.groundAcceleration
                    : ctx.groundDeceleration;

            ctx.currentVelocity = Vector3.MoveTowards(
                ctx.currentVelocity,
                targetVelocity,
                accelRate * Time.deltaTime
            );

            // Apply downward velocity to maintain ground contact
            Vector3 finalVelocity = ctx.currentVelocity;
            finalVelocity.y = -5f;

            // Move the character controller
            ctx.characterController.Move(
                finalVelocity * Time.deltaTime
            );
        }
    }
}
