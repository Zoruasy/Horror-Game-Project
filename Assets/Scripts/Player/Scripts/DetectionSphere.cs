
namespace CharacterController
{
    using UnityEngine;

    public class DetectionSphere : MonoBehaviour
    {
        [Header("References")]
        public FirstPersonController player;
        public Transform detectionSphere;

        [Header("Detection Settings")]
        public float standingRadius = 10f;
        public float crouchingRadius = 4f;
        public float sprintingRadius = 15f;

        [Header("Smooth Transition")]
        public float transitionSpeed = 8f;

        private float currentRadius;
        private bool wasCrouching;

        private void Start()
        {
            if (player == null)
                player = GetComponent<FirstPersonController>();

            currentRadius = standingRadius;
        }

        private void Update()
        {
            if (player == null || detectionSphere == null)
                return;

            // Check the player's current state
            bool isCrouching =
                player.CurrentState is PlayerCrouchingState;

            bool isSprinting =
                !isCrouching &&
                player.input.sprint &&
                player.input.moveInput.y > 0f;

            // Determine the target detection radius
            float targetRadius = standingRadius;

            if (isCrouching)
            {
                targetRadius = crouchingRadius;
            }
            else if (isSprinting)
            {
                targetRadius = sprintingRadius;
            }

            // Smoothly change the detection radius
            currentRadius = Mathf.MoveTowards(
                currentRadius,
                targetRadius,
                transitionSpeed * Time.deltaTime
            );

            // Unity's default sphere has a diameter of 1
            float diameter = currentRadius * 2f;

            detectionSphere.localScale = new Vector3(
                diameter,
                diameter,
                diameter
            );
        }

        public float GetDetectionRadius()
        {
            return currentRadius;
        }
    }
}
