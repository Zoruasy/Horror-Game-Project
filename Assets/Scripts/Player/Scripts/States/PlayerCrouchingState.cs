namespace CharacterController
{
    using UnityEngine;

    public class PlayerCrouchingState : PlayerBaseState
    {
        private const float minHeightEpsilon = 0.01f;

        private bool wasCrouchPressed;
        private PlayerStamina staminaSystem;

        public PlayerCrouchingState(
            FirstPersonController currentContext,
            PlayerStateFactory playerStateFactory
        ) : base(currentContext, playerStateFactory)
        {
        }

        public override void EnterState()
        {
            wasCrouchPressed = ctx.input.crouch;

            // Disable sprinting stamina while crouching
            staminaSystem = ctx.GetComponent<PlayerStamina>();

            if (staminaSystem != null)
                staminaSystem.SetCrouching(true);

            // Lower the camera
            ctx.targetCameraY = ctx.crouchingCameraHeight;

            if (!ctx.enableSmoothCrouch)
            {
                SetCapsuleHeight(
                    ctx.crouchingCharacterControllerHeight
                );
            }
        }

        public override void UpdateState()
        {
            ctx.targetCameraY = ctx.crouchingCameraHeight;

            ctx.targetFov = ctx.normalFov;
            ctx.currentBobIntensity = 0f;
            ctx.currentBobSpeed = 0f;
            ctx.targetTilt = 0f;

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

            HandleCrouchMovement();
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            // Allow sprinting stamina again
            if (staminaSystem != null)
                staminaSystem.SetCrouching(false);

            if (!ctx.enableSmoothCrouch)
            {
                ctx.characterController.height =
                    ctx.standingCharacterControllerHeight;

                ctx.characterController.center =
                    ctx.standingCharacterControllerCenter;
            }
        }

        public override void CheckSwitchStates()
        {
            bool crouchPressed = ctx.input.crouch;

            bool justPressed =
                crouchPressed && !wasCrouchPressed;

            wasCrouchPressed = crouchPressed;

            if (justPressed && !ctx.HasCeiling())
            {
                SwitchState(factory.Grounded());
                return;
            }
        }

        private void SetCapsuleHeight(float height)
        {
            float minHeight =
                ctx.characterController.radius * 2f
                + minHeightEpsilon;

            height = Mathf.Max(height, minHeight);

            ctx.characterController.height = height;

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
            Vector2 input = ctx.input.moveInput;

            Vector3 targetMove =
                ctx.transform.right * input.x +
                ctx.transform.forward * input.y;

            targetMove = Vector3.ClampMagnitude(
                targetMove,
                1f
            );

            Vector3 targetVelocity =
                targetMove * ctx.crouchSpeed;

            float accelRate =
                input.sqrMagnitude > 0.01f
                    ? ctx.groundAcceleration
                    : ctx.groundDeceleration;

            ctx.currentVelocity = Vector3.MoveTowards(
                ctx.currentVelocity,
                targetVelocity,
                accelRate * Time.deltaTime
            );

            Vector3 finalVelocity = ctx.currentVelocity;

            finalVelocity.y = -5f;

            ctx.characterController.Move(
                finalVelocity * Time.deltaTime
            );
        }
    }
}