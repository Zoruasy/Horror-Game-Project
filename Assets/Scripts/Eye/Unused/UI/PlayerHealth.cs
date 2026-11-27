using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth = -1f;

    private float Current
    {
        get
        {
            if (currentHealth < 0f) currentHealth = maxHealth;
            return currentHealth;
        }
    }

    public float Normalized => Current / maxHealth;

    public event Action<float> OnHealthChanged;

    private void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(Current - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }
}