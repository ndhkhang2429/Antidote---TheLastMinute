using UnityEngine;
using UnityEngine.UI;

public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("UI Reference")]
    public Image bloodOverlayImage;

    [Header("Settings")]
    [Range(0f, 1f)] public float maxAlpha = 0.85f;

    private HealthSystem _playerHealth;

    private void Awake()
    {
        _playerHealth = GetComponentInParent<HealthSystem>();
        SetAlpha(0f);
    }

    private void Update()
    {
        if (_playerHealth == null) return;

        // HP đầy → alpha = 0, HP = 0 → alpha = maxAlpha
        float targetAlpha = (1f - _playerHealth.HPPercent) * maxAlpha;
        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (bloodOverlayImage == null) return;
        var c = bloodOverlayImage.color;
        c.a = alpha;
        bloodOverlayImage.color = c;
    }
}