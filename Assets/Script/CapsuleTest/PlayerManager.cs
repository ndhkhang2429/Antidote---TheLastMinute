using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public PlayerStats statsSO; // Kéo file PlayerStatsData vào đây

    void Awake()
    {
        if (statsSO != null)
        {
            statsSO.ResetStats(); // Reset máu mana về ban đầu khi Play
            statsSO.OnPlayerDeath += HandleDeath; // Đăng ký sự kiện Chết
        }
    }

    void OnDestroy()
    {
        if (statsSO != null)
            statsSO.OnPlayerDeath -= HandleDeath; // Hủy đăng ký để tránh lỗi bộ nhớ
    }

    void Update()
    {
        // Chết rồi thì đứng im
        if (statsSO == null || statsSO.isDead) return;

        // Logic hồi mana theo % máu bằng Update (máu ít hồi ít)
        if (statsSO.currentHealth > 0 && statsSO.currentMana < statsSO.maxMana)
        {
            float actualRegen = statsSO.baseManaRegenPerSecond * statsSO.currentHealth;
            statsSO.currentMana += actualRegen * Time.deltaTime;

            if (statsSO.currentMana > statsSO.maxMana)
                statsSO.currentMana = statsSO.maxMana;

            statsSO.OnStatsChanged?.Invoke(); // Cập nhật UI liên tục khi hồi mana
        }
    }

    private void HandleDeath()
    {
        Debug.LogError("--- GAME OVER --- Player đã hết máu!");
        Time.timeScale = 0f; // Đóng băng game
    }
}