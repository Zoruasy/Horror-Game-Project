
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
            // Camera naar crouchhoogte
            ctx.targetCameraY = ctx.crouchingCameraHeight;

            // Direct crouchen als smooth crouch uitstaat
            if (!ctx.enableSmoothCrouch)
            {
                SetCapsuleHeight(
                    ctx.crouchingCharacterControllerHeight
                );
            }
        }

        public override void UpdateState()
        {
            // Camera laag houden
            ctx.targetCameraY = ctx.crouchingCameraHeight;

            // Visuele instellingen
            ctx.targetFov = ctx.normalFov;
            ctx.currentBobIntensity = 0f;
            ctx.currentBobSpeed = 0f;
            ctx.targetTilt = 0f;

            // Smooth crouching
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

            // Speler bewegen
            HandleCrouchMovement();

            // Controleren of speler mag opstaan
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            // Direct opstaan als smooth crouch uitstaat
            if (!ctx.enableSmoothCrouch && !ctx.HasCeiling())
            {
                ctx.characterController.height =
                    ctx.standingCharacterControllerHeight;

                ctx.characterController.center =
                    ctx.standingCharacterControllerCenter;
            }

            // Bij smooth crouch regelt GroundedState
            // het teruggaan naar de normale hoogte
        }

        public override void CheckSwitchStates()
        {
            // Ctrl loslaten = opstaan
            // Alleen als er geen plafond boven je zit
            if (!ctx.input.crouch && !ctx.HasCeiling())
            {
                SwitchState(factory.Grounded());
                return;
            }

            // Niet naar FallState wisselen tijdens crouchen
            // Dit voorkomt de ongewenste state switch
        }

        private void SetCapsuleHeight(float height)
        {
            // Capsule mag niet kleiner zijn dan diameter
            float minHeight =
                ctx.characterController.radius * 2f
                + minHeightEpsilon;

            height = Mathf.Max(height, minHeight);

            // Capsulehoogte aanpassen
            ctx.characterController.height = height;

            // Capsulecenter aanpassen
            // Hierdoor blijft de onderkant op dezelfde plek
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
            // Input ophalen
            Vector2 input = ctx.input.moveInput;

            // Bewegingsrichting berekenen
            Vector3 targetMove =
                ctx.transform.right * input.x +
                ctx.transform.forward * input.y;

            // Diagonaal niet sneller lopen
            targetMove = Vector3.ClampMagnitude(
                targetMove,
                1f
            );

            // Crouch snelheid
            Vector3 targetVelocity =
                targetMove * ctx.crouchSpeed;

            // Acceleratie en deceleratie
            float accelRate =
                input.sqrMagnitude > 0.01f
                    ? ctx.groundAcceleration
                    : ctx.groundDeceleration;

            ctx.currentVelocity = Vector3.MoveTowards(
                ctx.currentVelocity,
                targetVelocity,
                accelRate * Time.deltaTime
            );

            // Beweging toepassen
            Vector3 finalVelocity = ctx.currentVelocity;

            // Speler tegen de grond houden
            finalVelocity.y = -5f;

            ctx.characterController.Move(
                finalVelocity * Time.deltaTime
            );
        }
    }
}
