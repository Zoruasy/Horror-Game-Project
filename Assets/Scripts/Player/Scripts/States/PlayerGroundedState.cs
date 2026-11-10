
namespace CharacterController
{
    using UnityEngine;

    public class PlayerGroundedState : PlayerBaseState
    {
     
        private bool wasCrouchPressed;

        public PlayerGroundedState(
            FirstPersonController currentContext,
            PlayerStateFactory playerStateFactory
        ) : base(currentContext, playerStateFactory)
        {
        }

        public override void EnterState()
        {
            // having the grounding on the player
            ctx.moveDirection.y = -2f;

           
            wasCrouchPressed = ctx.input.crouch;
        }

        public override void UpdateState()
        {
            
            if (CheckAndSwitchState())
            {
                return;
            }

            
            ctx.targetCameraY = ctx.standingCameraHeight;

            // Check whether the player is sprinting
            bool isSprinting =
                ctx.input.sprint &&
                ctx.input.moveInput.y > 0f &&
                ctx.staminaSystem != null &&
                ctx.staminaSystem.CanSprint;

           
            float speed = isSprinting
                ? ctx.sprintSpeed
                : ctx.walkSpeed;

            // Sprint FOV
            ctx.targetFov = isSprinting
                ? ctx.sprintFov
                : ctx.normalFov;

            // Headbob intensity
            ctx.currentBobIntensity =
                ctx.bobAmount * (isSprinting ? 1.5f : 1f);

            
            ctx.currentBobSpeed =
                ctx.bobSpeed * (isSprinting ? 2f : 1f);

           
            ctx.targetTilt = 0f;

            
            RestoreStandingHeight();

            
            HandleGroundMovement(speed);
        }

        public override void ExitState()
        {
        }

        public override void CheckSwitchStates()
        {
            // Climb from the ground with E
            if (ctx.input.climbing && ctx.CheckLedge(out _))
            {
                SwitchState(factory.LedgeGrab());
                return;
            }
        }

        private bool CheckAndSwitchState()
        {
            bool crouchPressed = ctx.input.crouch;

            
            bool justPressed =
                crouchPressed && !wasCrouchPressed;

           
            wasCrouchPressed = crouchPressed;

          
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
                
                ctx.characterController.height =
                    targetHeight;

                ctx.characterController.center =
                    ctx.standingCharacterControllerCenter;
            }
        }

        private void SetCapsuleHeight(float height)
        {
           
            float minimumHeight =
                ctx.characterController.radius * 2f + 0.01f;

            height = Mathf.Max(height, minimumHeight);

            // Keep the capsule bottom in place
            Vector3 standingCenter =
                ctx.standingCharacterControllerCenter;

            float standingHeight =
                ctx.standingCharacterControllerHeight;

            ctx.characterController.height = height;

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
           
            Vector2 input = ctx.input.moveInput;

            
            Vector3 targetMove =
                ctx.transform.right * input.x +
                ctx.transform.forward * input.y;

           
            targetMove = Vector3.ClampMagnitude(
                targetMove,
                1f
            );

            
            Vector3 targetVelocity =
                targetMove * speed;

           
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
