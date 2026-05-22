using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image healthBarFill;
    public Image manaBarFill;

    [Header("Data Reference")]
    public PlayerStatsSO playerStatsSO; // Kéo file PlayerStatsData vào đây

    void OnEnable()
    {
        if (playerStatsSO != null)
        {
            // Đăng ký lắng nghe: Khi dữ liệu thay đổi, tự động gọi hàm UpdateUI
            playerStatsSO.OnStatsChanged += UpdateUI;
        }
    }

    void OnDisable()
    {
        if (playerStatsSO != null)
        {
            // Hủy đăng ký khi tắt UI để tránh tràn bộ nhớ
            playerStatsSO.OnStatsChanged -= UpdateUI;
        }
    }

    void Start()
    {
        UpdateUI(); // Cập nhật giao diện lần đầu khi vào game
    }

    private void UpdateUI()
    {
        if (playerStatsSO == null) return;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = playerStatsSO.currentHealth;
        }

        if (manaBarFill != null)
        {
            manaBarFill.fillAmount = playerStatsSO.currentMana / playerStatsSO.maxMana;
        }
    }
}