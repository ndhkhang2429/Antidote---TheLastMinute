using UnityEngine;
using UnityEngine.UI;

public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image bloodOverlayImage;

    [Header("Damage Flash")]
    [Tooltip("Độ đậm tối thiểu khi nhận sát thương nhẹ.")]
    [Range(0f, 1f)]
    [SerializeField] private float minDamageAlpha = 0.25f;

    [Tooltip("Độ đậm tối đa khi nhận sát thương lớn.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxDamageAlpha = 0.55f;

    [Tooltip("Thời gian blood screen mờ dần sau khi nhận sát thương.")]
    [Min(0.01f)]
    [SerializeField] private float fadeDuration = 0.6f;

    [Tooltip(
        "Điều chỉnh độ nhạy theo lượng HP đã mất. " +
        "Giá trị càng lớn thì damage nhỏ cũng làm hiệu ứng đậm hơn."
    )]
    [Min(0f)]
    [SerializeField] private float damageAlphaMultiplier = 3f;

    [Header("Low Health Warning")]
    [Tooltip("Bật hiệu ứng viền đỏ nhẹ khi HP thấp.")]
    [SerializeField] private bool enableLowHealthWarning = true;

    [Tooltip("Hiệu ứng HP thấp bắt đầu khi HP nhỏ hơn hoặc bằng mức này.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float lowHealthThreshold = 0.3f;

    [Tooltip("Alpha thấp nhất khi HP thấp.")]
    [Range(0f, 1f)]
    [SerializeField] private float lowHealthMinAlpha = 0.08f;

    [Tooltip("Alpha cao nhất khi viền đỏ pulse.")]
    [Range(0f, 1f)]
    [SerializeField] private float lowHealthMaxAlpha = 0.18f;

    [Tooltip("Tốc độ pulse của hiệu ứng HP thấp.")]
    [Min(0f)]
    [SerializeField] private float pulseSpeed = 3f;

    private HealthSystem _playerHealth;

    private float _previousHPPercent;
    private float _damageFlashAlpha;

    private bool _initialized;

    private void Awake()
    {
        FindHealthSystem();

        if (bloodOverlayImage != null)
        {
            // Blood screen chỉ dùng để hiển thị,
            // không được chặn raycast vào các UI khác.
            bloodOverlayImage.raycastTarget = false;
        }

        SetAlpha(0f);
    }

    private void Start()
    {
        InitializeHealthValue();
    }

    private void Update()
    {
        if (_playerHealth == null)
        {
            FindHealthSystem();

            if (_playerHealth == null)
                return;

            InitializeHealthValue();
        }

        float currentHPPercent = Mathf.Clamp01(_playerHealth.HPPercent);

        if (!_initialized)
        {
            _previousHPPercent = currentHPPercent;
            _initialized = true;
        }

        DetectDamage(currentHPPercent);
        UpdateDamageFlash();
        UpdateFinalOverlay(currentHPPercent);

        _previousHPPercent = currentHPPercent;
    }

    private void FindHealthSystem()
    {
        _playerHealth = GetComponentInParent<HealthSystem>();
    }

    private void InitializeHealthValue()
    {
        if (_playerHealth == null)
            return;

        _previousHPPercent = Mathf.Clamp01(_playerHealth.HPPercent);
        _initialized = true;
    }

    private void DetectDamage(float currentHPPercent)
    {
        // HP giảm nghĩa là player vừa nhận sát thương.
        if (currentHPPercent >= _previousHPPercent)
            return;

        float lostHPPercent = _previousHPPercent - currentHPPercent;

        // Damage nhỏ vẫn nhìn thấy được,
        // damage lớn sẽ làm blood screen đậm hơn.
        float newFlashAlpha = Mathf.Lerp(
            minDamageAlpha,
            maxDamageAlpha,
            Mathf.Clamp01(lostHPPercent * damageAlphaMultiplier)
        );

        // Nếu đang hiện hiệu ứng mà tiếp tục bị đánh,
        // giữ lại mức alpha cao hơn thay vì làm yếu đi.
        _damageFlashAlpha = Mathf.Max(_damageFlashAlpha, newFlashAlpha);
    }

    private void UpdateDamageFlash()
    {
        if (_damageFlashAlpha <= 0f)
            return;

        float fadeSpeed = maxDamageAlpha / Mathf.Max(0.01f, fadeDuration);

        _damageFlashAlpha = Mathf.MoveTowards(
            _damageFlashAlpha,
            0f,
            fadeSpeed * Time.deltaTime
        );
    }

    private void UpdateFinalOverlay(float currentHPPercent)
    {
        float lowHealthAlpha = CalculateLowHealthAlpha(currentHPPercent);

        // Khi vừa nhận damage, ưu tiên damage flash.
        // Khi flash biến mất, chỉ còn cảnh báo HP thấp nhẹ ở viền.
        float finalAlpha = Mathf.Max(
            _damageFlashAlpha,
            lowHealthAlpha
        );

        SetAlpha(finalAlpha);
    }

    private float CalculateLowHealthAlpha(float currentHPPercent)
    {
        if (!enableLowHealthWarning)
            return 0f;

        if (currentHPPercent <= 0f)
            return lowHealthMaxAlpha;

        if (currentHPPercent > lowHealthThreshold)
            return 0f;

        // HP càng thấp thì pulse càng rõ.
        float dangerAmount = Mathf.InverseLerp(
            lowHealthThreshold,
            0f,
            currentHPPercent
        );

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        float pulseAlpha = Mathf.Lerp(
            lowHealthMinAlpha,
            lowHealthMaxAlpha,
            pulse
        );

        return Mathf.Lerp(
            lowHealthMinAlpha,
            pulseAlpha,
            dangerAmount
        );
    }

    private void SetAlpha(float alpha)
    {
        if (bloodOverlayImage == null)
            return;

        Color color = bloodOverlayImage.color;
        color.a = Mathf.Clamp01(alpha);
        bloodOverlayImage.color = color;
    }

    private void OnDisable()
    {
        _damageFlashAlpha = 0f;
        SetAlpha(0f);
    }
}