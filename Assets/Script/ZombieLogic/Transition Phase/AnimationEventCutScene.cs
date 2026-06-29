using UnityEngine;

/// <summary>
/// Animation Event Cutscene Handler
/// Gắn trực tiếp vào Boss GameObject để nhận animation events
/// Các event được trigger từ Animation Timeline (frame-accurate)
/// </summary>
public class AnimationEventCutscene : MonoBehaviour
{
    private MutatedBossZombie _boss;
    private Transform _bossTransform;

    [Header("== VFX REFERENCES ==")]
    [SerializeField] private GameObject eyeGlowVfxPrefab;
    [SerializeField] private GameObject bodyAuraVfxPrefab;
    [SerializeField] private GameObject transformationBurstVfxPrefab;
    [SerializeField] private GameObject scaleUpParticlesPrefab;

    [Header("== EFFECT SETTINGS ==")]
    [SerializeField] private float eyeGlowDuration = 1.5f;
    [SerializeField] private float auraIntensity = 1.2f;

    private void Start()
    {
        _boss = GetComponent<MutatedBossZombie>();
        _bossTransform = transform;
    }

    // ============ ANIMATION EVENTS ============
    // Gọi từ Animation Timeline - các event này gắn trên keyframe cụ thể

    /// <summary>
    /// Trigger khi boss bắt đầu stand up (frame ~10)
    /// Dùng để phát hiệu ứng mắt bắt đầu sáng
    /// </summary>
    public void Event_EyesGlowStart()
    {
        if (eyeGlowVfxPrefab != null)
        {
            // Spawn tại vị trí head
            Vector3 headPos = _bossTransform.position + Vector3.up * 2f;
            GameObject eyeGlow = Instantiate(eyeGlowVfxPrefab, headPos, Quaternion.identity, _bossTransform);
            Destroy(eyeGlow, eyeGlowDuration);
        }

        // Screen effect: brief bloom spike
        ScreenEffectsController.Instance.SetBloom(1.5f);
    }

    /// <summary>
    /// Trigger khi boss rotation 90 độ (mid-stand-up)
    /// Hiệu ứng body bắt đầu phát sáng
    /// </summary>
    public void Event_BodyAuraAppear()
    {
        if (bodyAuraVfxPrefab != null)
        {
            // Spawn xung quanh body center
            Vector3 bodyPos = _bossTransform.position + Vector3.up * 1.5f;
            GameObject aura = Instantiate(bodyAuraVfxPrefab, bodyPos, Quaternion.identity, _bossTransform);
            Destroy(aura, 3f);
        }

        // Screen effect: vignette tăng
        if (ScreenEffectsController.Instance != null)
        {
            ScreenEffectsController.Instance.SetVignette(0.3f);
        }
    }

    /// <summary>
    /// Trigger khi boss bắt đầu roar (frame ~120 nếu 30FPS)
    /// Phát hiệu ứng chính - transformation burst
    /// </summary>
    public void Event_RoarTransformationBurst()
    {
        if (transformationBurstVfxPrefab != null)
        {
            // Burst từ center of body
            Vector3 burstPos = _bossTransform.position + Vector3.up * 1.5f;
            GameObject burst = Instantiate(transformationBurstVfxPrefab, burstPos, Quaternion.identity);
            Destroy(burst, 4f);
        }

        // Heavy screen shake
        if (ScreenShakeController.Instance != null)
        {
            ScreenShakeController.Instance.Shake(1.5f, 1.0f, 12f);
        }

        // Bloom peak
        if (ScreenEffectsController.Instance != null)
        {
            ScreenEffectsController.Instance.SetBloom(2.5f);
        }
    }

    /// <summary>
    /// Trigger khi boss scale animation bắt đầu (frame ~140)
    /// Particle burst radiating outward
    /// </summary>
    public void Event_ScaleUpParticles()
    {
        if (scaleUpParticlesPrefab != null)
        {
            // Particles emanate từ feet
            Vector3 feetPos = _bossTransform.position + Vector3.up * 0.2f;
            GameObject particles = Instantiate(scaleUpParticlesPrefab, feetPos, Quaternion.identity);
            Destroy(particles, 2.5f);
        }

        // Ground crack effect
        if (_boss != null)
        {
            // Có thể gọi public method từ boss nếu có
            // _boss.Event_TriggerGroundCrack();
        }
    }

    /// <summary>
    /// Trigger khi stand-up animation kết thúc
    /// Reset effects về normal
    /// </summary>
    public void Event_StandUpComplete()
    {
        // Eyes stop glowing
        ScreenEffectsController.Instance.SetBloom(0.5f);
    }

    /// <summary>
    /// Trigger khi roar animation kết thúc
    /// Peak moment - final shake
    /// </summary>
    public void Event_RoarPeak()
    {
        // Final hard shake
        ScreenShakeController.Instance.Shake(0.6f, 1.2f, 14f);

        // Bloom at peak
        ScreenEffectsController.Instance.SetBloom(2f);
    }

    /// <summary>
    /// Trigger khi transformation hoàn tất
    /// Boss ready for phase 2
    /// </summary>
    public void Event_TransformationComplete()
    {
        // Fade bloom back to normal
        ScreenEffectsController.Instance.SetBloom(0f);

        // Clean up all temporary VFX
        // (Done automatically via Destroy timers)
    }

    // ============ CUSTOM METHODS ============

    /// <summary>
    /// Trigger energy pulse từ body (dùng trong animation)
    /// </summary>
    public void Event_EnergyPulse()
    {
        // Screen effect: brief color shift + distortion
        ScreenEffectsController.Instance.SetChromaticAberration(0.3f);
        ScreenEffectsController.Instance.SetBloom(1.5f);
    }

    /// <summary>
    /// Trigger chain/cable movements (visual indicator của mutation)
    /// </summary>
    public void Event_ChainsTense()
    {
        ScreenShakeController.Instance.Shake(0.3f, 0.2f, 6f);
    }

    /// <summary>
    /// Debug: Log animation event timing
    /// </summary>
    public void Event_DebugLog(string message)
    {
        Debug.Log($"[Animation Event] {message} at time {Time.time}");
    }
}