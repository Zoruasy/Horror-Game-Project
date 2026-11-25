using UnityEngine;
using UnityEngine.UI;

namespace CharacterController
{
  
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Stamina")]
        public float maxStamina = 3f;
        public float stamina = 3f;
        public float drainPerSecond = 1f;
        public PlayerStamina staminaSystem;
        private bool isCrouching;

        [Header("Regeneration")]
        public float regenDelay = 0.75f;
        public float exhaustedRegenDelay = 1f;
        public float fastRegen = 3f;
        public float halfRegen = 0.5f;
        public float slowRegen = 0.5f;

        [Header("UI")]
        public RawImage staminaFillImage;
        public RectTransform staminaBar;
        public GameObject staminaBarUI;
        public float hideAfterFullSeconds = 1f;

        private bool staminaExhausted;
        private float regenTimer;
        private float fullTimer;

        public bool CanSprint => !staminaExhausted && stamina > 0f;
        public bool IsExhausted => staminaExhausted;

        private void Awake()
        {
            maxStamina = Mathf.Max(0.01f, maxStamina);
            stamina = Mathf.Clamp(stamina, 0f, maxStamina);
            staminaExhausted = stamina <= 0f;
            if (staminaBarUI != null)
                staminaBarUI.SetActive(false);
            UpdateBar();
            staminaSystem = GetComponent<PlayerStamina>();
        }

        
        public void Tick(bool sprintRequested)
        {
            bool sprinting = sprintRequested && CanSprint && !isCrouching;

            if (sprinting)
            {
                stamina -= Mathf.Max(0f, drainPerSecond) * Time.deltaTime;
                regenTimer = Mathf.Max(0f, regenDelay);
                fullTimer = 0f;

                if (stamina <= 0f)
                {
                    stamina = 0f;
                    staminaExhausted = true;
                    regenTimer = Mathf.Max(0f, exhaustedRegenDelay);
                }

                if (staminaBarUI != null)
                    staminaBarUI.SetActive(true);
            }
            else if (regenTimer > 0f)
            {
                regenTimer = Mathf.Max(0f, regenTimer - Time.deltaTime);
            }
            else if (stamina < maxStamina)
            {
                float regenSpeed = staminaExhausted
                    ? slowRegen
                    : stamina <= maxStamina * 0.5f ? halfRegen : fastRegen;

                stamina += Mathf.Max(0f, regenSpeed) * Time.deltaTime;
            }

            stamina = Mathf.Clamp(stamina, 0f, maxStamina);
            if (staminaExhausted && stamina >= maxStamina)
                staminaExhausted = false;

            if (!sprinting && staminaBarUI != null && staminaBarUI.activeSelf)
            {
                if (stamina >= maxStamina)
                {
                    fullTimer += Time.deltaTime;
                    if (fullTimer >= hideAfterFullSeconds)
                    {
                        staminaBarUI.SetActive(false);
                        fullTimer = 0f;
                    }
                }
                else
                {
                    fullTimer = 0f;
                }
            }

            UpdateBar();
        }

        private void UpdateBar()
        {
            if (staminaFillImage != null)
                staminaFillImage.color = staminaExhausted ? Color.red : Color.green;

            if (staminaBar != null)
                staminaBar.anchorMax = new Vector2(stamina / maxStamina, 1f);
        }

        // so crouching doesnt drain stamina
        public void SetCrouching(bool crouching)
        {
            isCrouching = crouching;
        }
        // increasing headbob

    }
}
