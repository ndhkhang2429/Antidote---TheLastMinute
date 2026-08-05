using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Hiển thị thanh máu và tự đăng ký event từ HealthSystem của boss.</summary>
public class BossHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text bossNameText;

    [Header("Display")]
    [SerializeField] private string bossDisplayName = "THE MUTATED";
    [SerializeField] private Color phase1Color = new Color(0.65f, 0.05f, 0.05f);
    [SerializeField] private Color phase2Color = new Color(0.85f, 0.2f, 0.05f);
    [SerializeField] private float hideAfterDeathDelay = 2f;

    private MutatedBossZombie _boss;
    private HealthSystem _health;
    private bool _showingPhase2;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }

        if (bossNameText != null)
            bossNameText.text = bossDisplayName;
    }

    private void Update()
    {
        if (_boss == null || _showingPhase2 == _boss.IsPhase2) return;
        _showingPhase2 = _boss.IsPhase2;
        UpdateFillColor();
    }

    public void ShowBoss(MutatedBossZombie boss)
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        Unsubscribe();

        _boss = boss;
        _health = boss != null ? boss.BossHealth : null;

        if (_health == null)
        {
            Debug.LogError("[BossHealthUI] Boss không có HealthSystem.", this);
            Hide();
            return;
        }

        _health.OnDamaged += HandleHealthChanged;
        _health.OnHealed += HandleHealthChanged;
        _health.OnDeath += HandleBossDeath;

        _showingPhase2 = _boss.IsPhase2;
        UpdateHealth(_health.CurrentHP, _health.MaxHP);
        UpdateFillColor();

        if (bossNameText != null)
            bossNameText.text = bossDisplayName;

        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        StopAllCoroutines();
        Unsubscribe();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void HandleHealthChanged(float currentHP, float maxHP)
    {
        UpdateHealth(currentHP, maxHP);
    }

    private void UpdateHealth(float currentHP, float maxHP)
    {
        float normalized = maxHP > 0f ? Mathf.Clamp01(currentHP / maxHP) : 0f;
        if (healthSlider != null)
            healthSlider.value = normalized;
    }

    private void UpdateFillColor()
    {
        if (healthFill != null)
            healthFill.color = _showingPhase2 ? phase2Color : phase1Color;
    }

    private void HandleBossDeath()
    {
        StartCoroutine(HideAfterDeath());
    }

    private IEnumerator HideAfterDeath()
    {
        yield return new WaitForSeconds(hideAfterDeathDelay);
        Hide();
    }

    private void Unsubscribe()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleHealthChanged;
            _health.OnHealed -= HandleHealthChanged;
            _health.OnDeath -= HandleBossDeath;
        }

        _health = null;
        _boss = null;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}