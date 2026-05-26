using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "Stats/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Base Settings (Chỉ setup, không đổi khi chơi)")]
    [Range(0.01f, 1f)] public float initialHealth = 1.0f;
    public float maxMana = 100f;
    public float baseManaRegenPerSecond = 5f;

    [Header("Runtime Stats (Sẽ thay đổi khi chơi)")]
    public float currentHealth;
    public float currentMana;
    public bool isDead;

    // Delegate/Event để báo cho UI tự động cập nhật, thay vì UI phải check liên tục ở Update
    public Action OnStatsChanged;
    public Action OnPlayerDeath;

    // Hàm này phải được gọi khi bắt đầu Game
    public void ResetStats()
    {
        currentHealth = initialHealth;
        currentMana = maxMana;
        isDead = false;
        OnStatsChanged?.Invoke();
    }

    public bool ConsumeMana(float amount)
    {
        if (isDead) return false;
        if (currentMana >= amount)
        {
            currentMana -= amount;
            OnStatsChanged?.Invoke(); // Báo UI update
            return true;
        }
        return false;
    }

    public void DeductHealthPercent(float percent)
    {
        if (isDead) return;

        currentHealth -= percent;
        if (currentHealth <= 0.001f)
        {
            currentHealth = 0f;
            isDead = true;
            OnPlayerDeath?.Invoke(); // Kích hoạt Game Over
        }
        OnStatsChanged?.Invoke(); // Báo UI update
    }
}