namespace CharacterController
{
    using UnityEngine;

    public class PlayerCrouchingState : PlayerBaseState
    {
        public PlayerCrouchingState(FirstPersonController currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) { }

        public override void EnterState()
        {
            // Visuals
            ctx.targetCameraY = ctx.crouchingCameraHeight;
            ctx.targetFov = ctx.normalFov;
            ctx.currentBobIntensity = 0f;
            ctx.currentBobSpeed = 0f;
            ctx.targetTilt = 0f;

            // Immediate physical crouch if smoothing disabled
            if (!ctx.enableSmoothCrouch && ctx.characterController != null)
            {
                ctx.characterController.height = ctx.crouchingCharacterControllerHeight;
                ctx.characterController.center = new Vector3(0f, ctx.crouchingCharacterControllerHeight / 2f, 0f);

                if (ctx.cameraParent != null)
                    ctx.cameraParent.localPosition = new Vector3(ctx.cameraParent.localPosition.x, ctx.crouchingCameraHeight, ctx.cameraParent.localPosition.z);
            }
        }

        public override void UpdateState()
        {
            // Keep visuals steady while crouching
            ctx.targetFov = ctx.normalFov;
            ctx.currentBobIntensity = 0f;
            ctx.currentBobSpeed = 0f;
            ctx.targetTilt = 0f;

            // Smooth height transition (if enabled)
            if (ctx.enableSmoothCrouch && ctx.characterController != null)
            {
                ctx.characterController.height = Mathf.MoveTowards(
                    ctx.characterController.height,
                    ctx.crouchingCharacterControllerHeight,
                    Time.deltaTime * ctx.crouchTransitionSpeed
                );

                ctx.characterController.center = Vector3.MoveTowards(
                    ctx.characterController.center,
                    new Vector3(0f, ctx.crouchingCharacterControllerHeight / 2f, 0f),
                    Time.deltaTime * (ctx.crouchTransitionSpeed / 2f)
                );
            }

            HandleCrouchMovement();
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            // Restore camera target height
            ctx.targetCameraY = ctx.standingCameraHeight;

            // Immediate physical stand if smoothing disabled
            if (!ctx.enableSmoothCrouch && ctx.characterController != null)
            {
                ctx.characterController.height = ctx.standingCharacterControllerHeight;
                ctx.characterController.center = ctx.standingCharacterControllerCenter;

                if (ctx.cameraParent != null)
                    ctx.cameraParent.localPosition = new Vector3(ctx.cameraParent.localPosition.x, ctx.standingCameraHeight, ctx.cameraParent.localPosition.z);
            }
        }

        public override void CheckSwitchStates()
        {
            if (ctx.input.jump && ctx.isGrounded && !ctx.HasCeiling())
            {
                SwitchState(factory.Jumping());
            }
            else if (!ctx.input.crouch)
            {
                // leave crouch when crouch input released (hold-to-crouch)
                SwitchState(factory.Grounded());
            }
        }

        private void HandleCrouchMovement()
        {
            Vector2 input = ctx.input.moveInput;
            Vector3 targetMove = ctx.transform.right * input.x + ctx.transform.forward * input.y;
            targetMove = Vector3.ClampMagnitude(targetMove, 1f);

            Vector3 targetVelocity = targetMove * ctx.crouchSpeed;

            float accel = 30f;
            ctx.currentVelocity = Vector3.MoveTowards(ctx.currentVelocity, targetVelocity, accel * Time.deltaTime);

            Vector3 finalVelocity = ctx.currentVelocity;
            finalVelocity.y = -5f;
            ctx.characterController.Move(finalVelocity * Time.deltaTime);
        }
    }
}