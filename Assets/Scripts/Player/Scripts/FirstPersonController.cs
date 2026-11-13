using System;
using TMPro;
using UnityEngine.UI;

namespace CharacterController
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using System.Collections;
    
    
    
    using UnityEngine.Rendering;

    public partial class FirstPersonController : MonoBehaviour
    {
        [Header("Settings")] public float walkSpeed = 3f;
        public float sprintSpeed = 5f;

        [Header("Stamina Reference")]
        public PlayerStamina staminaSystem;
        public bool CanSprint => staminaSystem != null && staminaSystem.CanSprint;
        public float crouchSpeed = 1.5f;
        public float jumpSpeed = 4f;
        public float gravity = 9.81f;
        public float slideDuration = 0.7f;
        public float slideSpeed = 6f;
        public float mouseSensitivity = 2f;
        public float strafeTiltAmount = 2f;

        [Header("Attack Settings")]
        public LayerMask attackMask = ~0;
        public float attackRange = 1f;
        public float attackCooldown = 0.5f;
        private float nextAttackTime;
        public float backstabThreshold = 0.3f;
        private Inventory inventory;
        public bool HasWeapon => inventory && inventory.Has<WeaponPickup>();

        [Header("Parry Settings")] 
        public float parryWindowTime = 0.3f;
        [HideInInspector] private Coroutine parryCoroutine;
        [HideInInspector] private bool isParryWindowOpen = false;
        public float parryCooldown = 1f; // not used but can be used later
        [HideInInspector] private bool isParryOnCooldown = false;
        [SerializeField, Min(0f)] private float parryShakeIntensity = 1f;
        [SerializeField, Min(0.01f)] private float parryShakeDuration = 0.16f;
        [SerializeField, Min(0f)] private float parryShakePositionAmplitude = 0.018f;
        [SerializeField, Min(0f)] private float parryShakeRotationAmplitude = 1.6f;
        [SerializeField, Min(0.01f)] private float parryShakeFrequency = 24f;
        [SerializeField, Range(0f, 1f)] private float parryShakeDirectionInfluence = 0.35f;
        [SerializeField] private AnimationCurve parryShakeFadeOut =
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Parry Audio")]
        [SerializeField] private AudioClip successfulParrySound;
        [SerializeField, Range(0f, 1f)] private float successfulParryVolume = 0.8f;
        [SerializeField] private AudioClip attackGruntSound;
        [SerializeField] private AudioClip clothesRufflingSound;
        [SerializeField] private AudioSource parryAudioSource;

        [Header("Movement polish")]
        public float groundAcceleration = 50f;
        public float groundDeceleration = 60f;
        [HideInInspector] public Vector3 currentVelocity;

        [Header("Advanced Mechanics")]
        public bool enableSmoothCrouch = true;
        public float crouchTransitionSpeed = 10f;
        public bool enableSlopeSliding = true;
        public float slideUphillFriction = 3f;
        public float slideSteerControl = 4f;

        [Header("References")]
        public Transform playerCamera;
        public Transform cameraParent;
        [SerializeField] private ScreenShake screenShake;
        public Transform groundCheck;
        public LayerMask groundMask; // ceiling
        public TMP_Text WeaponAvailableUIText;

        [Header("Headbob Settings")]
        public float walkingBobSpeed = 10f;
        public float sprintingBobSpeed = 16f;

        public float walkingBobIntensity = 0.05f;
        public float sprintingBobIntensity = 0.08f;

        [HideInInspector] public CharacterController characterController;
        [HideInInspector] public IInputManager input;
        [HideInInspector] public Vector3 moveDirection;
        [HideInInspector] public bool isGrounded;


        private PlayerBaseState currentState;
        private PlayerStateFactory states;
        private bool hasGroundedState;
        private float xRotation = 0f;
        private float currentTilt;
        private float tiltVelocity;

        public PlayerBaseState CurrentState { get => currentState; set => currentState = value; }

        [Header("Visual Settings")]
        public float normalFov = 60f;
        public float sprintFov = 75f;
        public float slideFovBoost = 5f;
        public float fovChangeSpeed = 8f;
        public float bobAmount = 0.03f;
        public float bobSpeed = 12f;
        public float recoilReturnSpeed = 5f;

        [HideInInspector] public Camera cam;
        [HideInInspector] public float targetFov;
        [HideInInspector] public float currentBobIntensity;
        [HideInInspector] public float currentBobSpeed;
        [HideInInspector] public float targetTilt;

        private float bobTimer;
        private float fovVelocity;
        private float originalCamY;
        private float visionZoomAmount;
        private bool controlsLocked;
        private VisionTurningSE visionTurning;

        [Header("Height Settings")]
        public float standingCameraHeight = 1.75f;
        public float crouchingCameraHeight = 1f;
        public float crouchingCharacterControllerHeight = 1f;
        [HideInInspector] public float standingCharacterControllerHeight = 1.8f;
        [HideInInspector] public Vector3 standingCharacterControllerCenter = new Vector3(0, 0.9f, 0);
        [HideInInspector] public float targetCameraY;

        // cooldown for ledge checks / climbs
        [HideInInspector] public float currentLedgeCooldown;

        [Header("Ledge Settings")]
        public LayerMask ledgeLayer;
        public float ledgeDetectionDistance = 1f;
        public float climbDuration = 0.6f;
        public float climbHeightArc = 0.4f;
        public float climbTiltAmount = -7f;


        [Header("Visual Preferences")]
        public bool useFovKick = true;
        public bool useHeadBob = true;
        public bool useCameraTilt = true;
        public bool useClimbTilt = true;

        [Header("Debug")]
        public bool currentStateDebug = true;

        public enemyChase enemy; 

        void OnGUI()
        {
            if (currentState != null && Application.isEditor && currentStateDebug)
                GUILayout.Label("Current State: " + currentState.GetType().Name);
        }

        private void Awake()
        {
            visionTurning = GetComponent<VisionTurningSE>();
            cam = playerCamera.GetComponent<Camera>();
            if (screenShake == null)
                screenShake = playerCamera.GetComponentInParent<ScreenShake>();

            targetFov = normalFov;
            targetCameraY = standingCameraHeight;
            originalCamY = standingCameraHeight;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            characterController = GetComponent<CharacterController>();
            inventory = GetComponent<Inventory>();
            standingCharacterControllerHeight = characterController.height;
            standingCharacterControllerCenter = characterController.center;

            if (staminaSystem == null)
                staminaSystem = GetComponent<PlayerStamina>();


            input = GetComponent<IInputManager>();
            if (input == null)
            {
                var fallback = GetComponent<InputManager>();
                if (fallback != null)
                    input = fallback;
            }
            

        
            states = new PlayerStateFactory(this);

            currentState = states.Grounded();
            currentState?.EnterState();
            RefreshWeaponUi();
        }

        private void Update()
        {
            if (controlsLocked)
            {
                UpdateVisuals();
                return;
            }

            if (input != null && input.climbing)
            {
                Debug.Log("E was pressed!");

                if (CheckLedge(out Vector3 climbPos))
                {
                    Debug.Log("Climbable found!");
                }
                else
                {
                    Debug.Log("No climbable ledge found!");
                }
            }
            if (currentLedgeCooldown > 0)
                currentLedgeCooldown -= Time.deltaTime;

            bool wasGrounded = isGrounded;
            isGrounded = characterController.isGrounded || Physics.CheckSphere(groundCheck.position, characterController.radius * 0.9f, groundMask, QueryTriggerInteraction.Ignore);
            bool justLanded = hasGroundedState && !wasGrounded && isGrounded;
            hasGroundedState = true;

            if (staminaSystem != null)
                staminaSystem.Tick(input != null && input.sprint && input.moveInput.y > 0f);

            if (currentState != null)
                currentState.UpdateState();



            HandleRotation();
            UpdateVisuals();

            if (input != null && input.attack)
                Attack();

        }


        private void HandleRotation()
        {
            if (visionTurning != null && visionTurning.IsTurning)
                return;

            if (input == null) return;

            float sensitivity = mouseSensitivity * GameSettings.MouseSensitivity;
            float mouseX = input.lookInput.x * sensitivity;
            float mouseY = input.lookInput.y * sensitivity;

            transform.Rotate(Vector3.up * mouseX);

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            float strafeTilt = useCameraTilt ? (-input.moveInput.x * strafeTiltAmount) : 0;
            float combinedTargetTilt = (useCameraTilt ? targetTilt : 0) + strafeTilt;

            currentTilt = Mathf.SmoothDamp(currentTilt, combinedTargetTilt, ref tiltVelocity, 0.1f);
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0, currentTilt);
        }

        public void SetPitchFromExternal(float pitch)
        {
            xRotation = pitch;
        }

        public void SetVisionZoom(float amount)
        {
            visionZoomAmount = Mathf.Max(0f, amount);
        }

        public void SetControlsLocked(bool locked)
        {
            controlsLocked = locked;
        }

        public void UpdateVisuals()
        {
            if (!useFovKick)
            {
                targetFov = normalFov;
            }
            cam.fieldOfView = Mathf.SmoothDamp(
                cam.fieldOfView,
                Mathf.Max(1f, targetFov - visionZoomAmount),
                ref fovVelocity,
                1f / fovChangeSpeed);


            originalCamY = Mathf.Lerp(originalCamY, targetCameraY, Time.deltaTime * 8f);

            float targetBobOffset = 0f;
            if (!controlsLocked && useHeadBob &&
                characterController.velocity.magnitude > 0.1f && isGrounded)
            {
                bobTimer += Time.deltaTime * currentBobSpeed;
                targetBobOffset = Mathf.Sin(bobTimer) * currentBobIntensity;
            }
            else if (controlsLocked)
            {
                bobTimer = 0f;
            }
            else
            {
                bobTimer = Mathf.Lerp(bobTimer, 0, Time.deltaTime * 10f);
            }


            float desiredY = originalCamY + targetBobOffset;

            
            float smoothedY = Mathf.Lerp(cameraParent.localPosition.y, desiredY, Time.deltaTime * 15f);

            cameraParent.localPosition = new Vector3(cameraParent.localPosition.x, smoothedY, cameraParent.localPosition.z);
        }

        public void TriggerCameraShake(float intensity, float duration, Vector3 direction = default)
        {
            if (screenShake == null)
            {
                Debug.LogWarning("No ScreenShake component is assigned or found on the player camera hierarchy.", this);
                return;
            }

            screenShake.TriggerShake(
                intensity,
                duration,
                parryShakePositionAmplitude,
                parryShakeRotationAmplitude,
                parryShakeFrequency,
                parryShakeDirectionInfluence,
                parryShakeFadeOut,
                direction);
        }

        public bool HasCeiling()
        {
            float radius = characterController.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (characterController.height - radius);
            float checkDistance = standingCharacterControllerHeight - characterController.height + 0.1f;

            return Physics.SphereCast(origin, radius, Vector3.up, out _, checkDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

        public void Attack()
        {

            OpenParryWindow();
            if (Time.time < nextAttackTime) return;
            nextAttackTime = Time.time + attackCooldown;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, attackRange, attackMask, QueryTriggerInteraction.Ignore))
            {
                enemyChase enemy = hit.collider.GetComponentInParent<enemyChase>();
                if (enemy != null)
                {

                    Vector3 myForward = transform.forward;
                    Vector3 enemyForward = enemy.transform.forward;
                    myForward.y = 0f;
                    enemyForward.y = 0f;
                    

                    float dot = Vector3.Dot(myForward.normalized, enemyForward.normalized);

                    if (dot >= backstabThreshold &&
                        HasWeapon &&
                        !enemy.IsPlayerDetected &&
                        !enemy.IsStaggered)
                    {
                        enemy.KillEnemy();
                        inventory.Consume(inventory.Get<WeaponPickup>());
                    }
                    else
                    {
                        Debug.Log("No backstab, dot = " + dot);
                    }
                }
            }

            Debug.DrawRay(ray.origin, ray.direction * attackRange, Color.red, 1f);
        }



        private void OpenParryWindow()
        {

            if (isParryOnCooldown == false)
            {
                PlayPlayerSound(attackGruntSound);
                PlayPlayerSound(clothesRufflingSound);
                parryCoroutine = StartCoroutine(ParryWindow());
            }
            else
            {
            Debug.Log("parry is on cooldown");
            }
        }

        private IEnumerator ParryWindow()
        {
            isParryOnCooldown = true;
            isParryWindowOpen = true;
            
            Debug.Log("OpenParryWindow");
            yield return new WaitForSeconds(parryWindowTime);
            Debug.Log("Parry Window Closed");
            
            isParryWindowOpen = false;
            
            yield return new WaitForSeconds(parryCooldown - parryWindowTime);
            isParryOnCooldown = false;
            parryCoroutine = null;
            Debug.Log("Parry cooldown OVER");
            
        }

        public void HitByEnemyAttack(enemyChase enemyThatHitsPlayer)
        {
            if (isParryWindowOpen == true)
            {
                triggerParry(enemyThatHitsPlayer);
            }
            else
            {
                enemyThatHitsPlayer.PlayerDies();
            }
        }
        

        private void triggerParry(enemyChase enemyThatHitsPlayer)
        {
            PlayPlayerSound(successfulParrySound, successfulParryVolume);
            TriggerCameraShake(parryShakeIntensity, parryShakeDuration);
            // stagger enemy functionality on the correct enemy that attacks you
            Debug.Log("will stagger the attacking enemy rn:) ( after attack is implemented");
            enemyThatHitsPlayer.StaggerByPlayerParry();
        }

        private void PlayPlayerSound(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            if (parryAudioSource != null)
                parryAudioSource.PlayOneShot(clip, volume);
            else
                GameSettings.PlayClipAtPoint(clip, transform.position, volume);
        }
        
        
        
        public void PickUpWeapon(){}
        private void OnEnable()
        {
            if (inventory) inventory.Changed += RefreshWeaponUi;
        }

        private void OnDisable()
        {
            if (inventory) inventory.Changed -= RefreshWeaponUi;
        }

        private void RefreshWeaponUi()
        {
            if (WeaponAvailableUIText != null)
            {
                WeaponAvailableUIText.text = HasWeapon ? "Weapon: Equipped" : "Weapon: Not Equipped";
            }
        }

        public bool CheckLedge(out Vector3 climbPosition)
        {
            climbPosition = Vector3.zero;

            if (currentLedgeCooldown > 0f)
                return false;

            float radius = characterController.radius;

            Vector3 wallOrigin =
                transform.position + Vector3.up * 1.5f;

            if (!Physics.Raycast(
                wallOrigin,
                transform.forward,
                out RaycastHit wallHit,
                ledgeDetectionDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            
            if (!wallHit.collider.CompareTag("Climbable"))
                return false;


            Vector3 topOrigin =
                wallHit.point
                + transform.forward * (radius + 0.15f)
                + Vector3.up * 1.2f;

            if (!Physics.Raycast(
                topOrigin,
                Vector3.down,
                out RaycastHit topHit,
                2f,
                ~0,
                QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Only climb on the same collider
            if (topHit.collider != wallHit.collider)
                return false;


            if (Vector3.Angle(topHit.normal, Vector3.up) > 45f)
                return false;

            climbPosition =
                topHit.point
                + transform.forward * (radius + 0.1f);

            return true;

        }




    }
}