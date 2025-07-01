using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int maxArmor = 100;
    public int currentHealth { get; private set; }
    public int currentArmor { get; private set; }
    public bool isDead { get; private set; } = false;

    private void Start() {
        currentHealth = maxHealth;
        currentArmor = maxArmor;
        isDead = false;
    }
    public void TakeDamage(int damageAmount)
    {
        if (isDead || damageAmount <= 0) return;

        if (currentArmor > 0)
        {
            int leftover = damageAmount - currentArmor;
            currentArmor = Mathf.Max(currentArmor - damageAmount, 0);

            if (leftover > 0)
                currentHealth = Mathf.Max(currentHealth - leftover, 0);
        }
        else
        {
            currentHealth = Mathf.Max(currentHealth - damageAmount, 0);
        }

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0 || currentHealth >= maxHealth) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"Healed: HP={currentHealth}");
        Debug.Log("Healed 20 HP");

    }

    public void AddArmor()
    {
        if (isDead || currentArmor >= maxArmor) return;

        currentArmor = Mathf.Min(currentArmor + 25, maxArmor);
        Debug.Log($"Armor Added: Armor={currentArmor}");
        Debug.Log("Added 25 armor");

    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player Died");
        // TODO: End game
    }
}
