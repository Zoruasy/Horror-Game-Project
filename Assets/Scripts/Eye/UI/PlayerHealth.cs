using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    public event Action<float> OnHealthChanged;

    private void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public float Normalized => currentHealth / maxHealth;
}