namespace CharacterController
{
    using UnityEngine;

    public class PlayerLedgeGrabState : PlayerBaseState
    {
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float climbTimer;

        public PlayerLedgeGrabState(
            FirstPersonController context,
            PlayerStateFactory factory
        ) : base(context, factory) { }

        public override void EnterState()
        {
            startPosition = ctx.transform.position;
            climbTimer = 0f;

            if (!ctx.CheckLedge(out targetPosition))
            {
                SwitchState(factory.Fall());
                return;
            }

            ctx.moveDirection.y = 0f;
            ctx.currentVelocity = Vector3.zero;

            if (ctx.useClimbTilt)
                ctx.targetTilt = ctx.climbTiltAmount;
        }

        public override void UpdateState()
        {
            climbTimer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                climbTimer / Mathf.Max(0.01f, ctx.climbDuration)
            );

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            Vector3 position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothProgress
            );

            position.y += Mathf.Sin(progress * Mathf.PI)
                * ctx.climbHeightArc;

            ctx.characterController.Move(
                position - ctx.transform.position
            );

            if (progress >= 1f)
            {
                SwitchState(factory.Fall());
            }
        }

        public override void ExitState()
        {
            ctx.targetTilt = 0f;
            ctx.currentLedgeCooldown = 0.5f;
        }

        public override void CheckSwitchStates() { }
    }
}