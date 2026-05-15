using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kết nối HealthSystem và Timer nhiễm độc với UI.
/// Gắn script này vào Canvas hoặc HUD GameObject.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header ("HP Bar")]
    public Image hpBarFill;

    [Header("Poison Timer")]
    public Image poisonBarFill;
    public TextMeshProUGUI poisonTimerText;

    [Header("Health System")]
    public HealthSystem playerHealth;

    [Header("Setting Poison Timer")]
    public float poisonDuration = 1800f;
    private float _poisonTimeLeft;
    private bool _isPoisoned = true;

    private readonly Color _colorHigh = new Color(0.51f, 0.78f, 0.52f); // Xanh lá
    private readonly Color _colorMedium = new Color(1f, 0.72f, 0.30f); // Cam
    private readonly Color _colorLow = new Color(0.90f, 0.35f, 0.35f); // Đỏ

    private void Start()
    {
        _poisonTimeLeft = poisonDuration;

        if(playerHealth != null)
        {
            playerHealth.OnHPChanged += UpdateHPBar;
            UpdateHPBar(playerHealth.CurrentHP, playerHealth.MaxHP);
        }
        else
        {
            Debug.LogError("[HUDManager] Chưa gán PlayerHealth!");
        }

        UpdatePoisonUI();
    }

    private void Update()
    {
        if (_isPoisoned)
        {
            TickPoisonTimer();
        }
    }

    private void UpdateHPBar(float currentHP, float maxHP)
    {
        if (hpBarFill == null) return;

        float pct = currentHP / maxHP;
        hpBarFill.fillAmount = pct;

        if(pct > 0.6f)
        {
            hpBarFill.color = _colorHigh;
        }
        else if(pct > 0.3f)
        {
            hpBarFill.color = _colorMedium;
        }
        else {  
            hpBarFill.color = _colorLow;
        }
    }

    private void TickPoisonTimer()
    {
        if(_poisonTimeLeft <= 0)
        {
            _poisonTimeLeft = 0;
            _isPoisoned = false;
            OnPoisonExpired();
            return;
        }
        
        _poisonTimeLeft -= Time.deltaTime;
        UpdatePoisonUI();
    }

    private void UpdatePoisonUI()
    {
        if (poisonBarFill != null)
        {
            poisonBarFill.fillAmount = _poisonTimeLeft;
        }
        if (poisonTimerText != null)
        {
            int minutes = Mathf.FloorToInt(_poisonTimeLeft / 60f);
            int seconds = Mathf.FloorToInt(_poisonTimeLeft % 60f);
            poisonTimerText.text = $"{minutes:00}:{seconds:00}";
            poisonTimerText.color = _poisonTimeLeft < 60f
                ? _colorLow      
                : new Color(0.81f, 0.58f, 0.85f);
        }
    }

    private void OnPoisonExpired()
    {
        Debug.Log("[HUDManager] Hết thời gian! Player biến thành zombie!");
        // TODO: Trigger game over hoặc transform thành zombie
    }

    public void AddPoisonTime(float seconds)
    {
        _poisonTimeLeft = Mathf.Min(_poisonTimeLeft + seconds, poisonDuration);
    }

    /// <summary>
    /// Gọi khi player uống thuốc giải hoàn toàn.
    /// </summary>
    public void CurePoison()
    {
        _isPoisoned = false;
        _poisonTimeLeft = poisonDuration;
        UpdatePoisonUI();
        Debug.Log("[HUDManager] Đã giải độc!");
    }
}
