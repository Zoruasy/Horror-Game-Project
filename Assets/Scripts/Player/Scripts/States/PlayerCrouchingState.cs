namespace CharacterController
{
    using UnityEngine;

    public class PlayerCrouchingState : PlayerBaseState
    {
        public PlayerCrouchingState(FirstPersonController currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) { }

        public override void EnterState()
        {
            // immediate crouch
            if (!ctx.enableSmoothCrouch)
            {
                ctx.characterController.height = ctx.crouchingCharacterControllerHeight;
                ctx.characterController.center = new Vector3(0, ctx.crouchingCharacterControllerHeight / 2f, 0);
            }
        }

        public override void UpdateState()
        {
            ctx.targetFov = ctx.normalFov;
            ctx.currentBobIntensity = 0;
            ctx.currentBobSpeed = 0;
            ctx.targetTilt = 0;

            if (ctx.enableSmoothCrouch)
            {
                ctx.characterController.height = Mathf.MoveTowards(
                    ctx.characterController.height,
                    ctx.crouchingCharacterControllerHeight,
                    Time.deltaTime * ctx.crouchTransitionSpeed
                );

                ctx.characterController.center = Vector3.MoveTowards(
                    ctx.characterController.center,
                    new Vector3(0, ctx.crouchingCharacterControllerHeight / 2f, 0),
                    Time.deltaTime * (ctx.crouchTransitionSpeed / 2f)
                );
            }

            HandleCrouchMovement();
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            // Ensure height returns
            if (!ctx.enableSmoothCrouch)
            {
                ctx.characterController.height = ctx.standingCharacterControllerHeight;
                ctx.characterController.center = ctx.standingCharacterControllerCenter;
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