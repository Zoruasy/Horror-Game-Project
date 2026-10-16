
namespace CharacterController
{
    using UnityEngine;

    public class PlayerCrouchingState : PlayerBaseState
    {
        private const float minHeightEpsilon = 0.01f;

        public PlayerCrouchingState(
            FirstPersonController currentContext,
            PlayerStateFactory playerStateFactory
        ) : base(currentContext, playerStateFactory)
        {
        }

        public override void EnterState()
        {
            // Lower the camera to crouching height
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

            // Handle player movement while crouching
            HandleCrouchMovement();

            // Check whether the player can stand up
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            // Instantly restore standing height if smooth crouching is disabled
            if (!ctx.enableSmoothCrouch && !ctx.HasCeiling())
            {
                ctx.characterController.height =
                    ctx.standingCharacterControllerHeight;

                ctx.characterController.center =
                    ctx.standingCharacterControllerCenter;
            }

            // When smooth crouching is enabled, GroundedState
            // handles restoring the capsule to standing height
        }

        public override void CheckSwitchStates()
        {
            // Stand up when crouch is released and there is enough space
            if (!ctx.input.crouch && !ctx.HasCeiling())
            {
                SwitchState(factory.Grounded());
                return;
            }

            // Do not switch to FallState while crouching
            // This prevents unwanted state transitions
        }

        private void SetCapsuleHeight(float height)
        {
            // Prevent the capsule from becoming smaller than its diameter
            float minHeight =
                ctx.characterController.radius * 2f
                + minHeightEpsilon;

            height = Mathf.Max(height, minHeight);

            // Update the capsule height
            ctx.characterController.height = height;

            // Adjust the capsule center to keep its bottom grounded
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
            // Get movement input
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

            // Calculate target crouching velocity
            Vector3 targetVelocity =
                targetMove * ctx.crouchSpeed;

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

            // Apply final movement velocity
            Vector3 finalVelocity = ctx.currentVelocity;

            // Apply downward force to maintain ground contact
            finalVelocity.y = -5f;

            // Move the character controller
            ctx.characterController.Move(
                finalVelocity * Time.deltaTime
            );
        }
    }
}
