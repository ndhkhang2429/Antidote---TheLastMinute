using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện HUD (Máu và Năng lượng).
/// Thay thế phiên bản cũ, đã gỡ bỏ Poison Timer.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("HP Bar")]
    public Image hpBarFill;

    [Header("Stamina Bar")]
    public Image staminaBarFill;

    [Header("Systems Reference")]
    public HealthSystem playerHealth;
    public PlayerStamina playerStamina;

    // ── Bảng màu UI ────────────────────────────────────────
    private readonly Color _colorHigh = new Color(0.51f, 0.78f, 0.52f);
    private readonly Color _colorMedium = new Color(1f, 0.72f, 0.30f);
    private readonly Color _colorLow = new Color(0.90f, 0.35f, 0.35f);

    private readonly Color _staminaNormal = new Color(0.2f, 0.6f, 1f, 0.8f);
    private readonly Color _staminaExhausted = new Color(0.90f, 0.2f, 0.2f, 0.9f); // Chớp đỏ khi kiệt sức

    private void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged += OnHealthChanged;
            playerHealth.OnHealed += OnHealthChanged;
            UpdateHPBar(playerHealth.CurrentHP, playerHealth.MaxHP);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= OnHealthChanged;
            playerHealth.OnHealed -= OnHealthChanged;
        }
    }

    private void Update()
    {
        if (playerStamina != null)
        {
            UpdateStaminaUI();
        }
    }

    // ── HP Bar ─────────────────────────────────────────────
    private void OnHealthChanged(float currentHP, float maxHP)
    {
        UpdateHPBar(currentHP, maxHP);
    }

    private void UpdateHPBar(float currentHP, float maxHP)
    {
        if (hpBarFill == null) return;

        float pct = currentHP / maxHP;
        hpBarFill.fillAmount = pct;

        if (pct > 0.6f) hpBarFill.color = _colorHigh;
        else if (pct > 0.3f) hpBarFill.color = _colorMedium;
        else hpBarFill.color = _colorLow;
    }

    // ── Stamina Bar ────────────────────────────────────────
    private void UpdateStaminaUI()
    {
        if (staminaBarFill == null) return;

        float pct = playerStamina.currentStamina / playerStamina.maxStamina;
        staminaBarFill.fillAmount = pct;

        // Cảnh báo màu đỏ nếu đang trong trạng thái kiệt sức (đợi hồi 20%)
        if (playerStamina.isExhausted)
        {
            staminaBarFill.color = _staminaExhausted;
        }
        else
        {
            staminaBarFill.color = _staminaNormal;
        }
    }
}