using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioSource breathSource;
    [SerializeField] private PlayerStamina playerStamina; // kéo Player object vào
    [SerializeField] private HealthSystem healthSystem;   // kéo Player object vào (nơi có HealthSystem)

    [Header("Footstep - Concrete Clips")]
    [SerializeField] private AudioClip[] footstepConcrete;

    [Header("Footstep Settings")]
    [SerializeField] private float walkVolume = 0.5f;
    [SerializeField] private float runVolume = 0.8f;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.05f;

    [Header("Footstep Timing (FPS - không dùng Animation Event)")]
    [SerializeField] private float walkStepInterval = 0.5f; // thời gian giữa 2 bước khi đi bộ
    [SerializeField] private float runStepInterval = 0.32f;  // thời gian giữa 2 bước khi chạy

    [Header("Breathing Clips")]
    [SerializeField] private AudioClip breathRun;
    [SerializeField] private AudioClip breathExhausted;
    [SerializeField][Range(0f, 1.5f)] private float breathRunVolume = 1f;
    [SerializeField][Range(0f, 1.5f)] private float breathExhaustedVolume = 1f;

    [Header("Jump & Land Clips")]
    [SerializeField] private AudioClip[] jumpClips;
    [SerializeField] private AudioClip[] landClips;
    [SerializeField] private float jumpVolume = 0.7f;
    [SerializeField] private float landVolume = 0.8f;

    [Header("Hurt Clips")]
    [SerializeField] private AudioClip[] hurtClips;
    [SerializeField] private float hurtVolume = 0.85f;

    [Header("Low Health Heartbeat")]
    [SerializeField] private AudioSource heartbeatSource;
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField][Range(0f, 1f)] private float lowHealthThreshold = 0.3f; // dưới 30% HP thì bắt đầu nghe tim đập
    [SerializeField] private float heartbeatMinVolume = 0.25f; // âm lượng ngay lúc vừa xuống ngưỡng
    [SerializeField] private float heartbeatMaxVolume = 1f;    // âm lượng khi HP gần 0

    private bool isMoving = false;
    private bool isRunning = false;
    private float stepTimer = 0f;

    void Update()
    {
        HandleFootstepTimer();
        UpdateBreathing();
        UpdateHeartbeat();
    }

    void OnEnable()
    {
        if (healthSystem != null)
            healthSystem.OnDamaged += HandleDamaged;
    }

    void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.OnDamaged -= HandleDamaged;
    }

    // Được HealthSystem gọi mỗi khi Player nhận damage (currentHP, maxHP)
    private void HandleDamaged(float currentHP, float maxHP)
    {
        PlayHurtSound();
    }

    private void PlayHurtSound()
    {
        if (hurtClips == null || hurtClips.Length == 0 || footstepSource == null) return;
        AudioClip clip = hurtClips[Random.Range(0, hurtClips.Length)];
        footstepSource.pitch = Random.Range(pitchMin, pitchMax);
        footstepSource.PlayOneShot(clip, hurtVolume);
    }

    // ================= FOOTSTEP =================

    // Gọi từ FirstPersonController mỗi frame trong Move(), thay cho Animation Event
    public void SetMovementState(bool moving, bool running)
    {
        isMoving = moving;
        isRunning = running;
    }

    private void HandleFootstepTimer()
    {
        if (!isMoving)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = isRunning ? runStepInterval : walkStepInterval;
        }
    }

    private void PlayFootstep()
    {
        if (footstepConcrete == null || footstepConcrete.Length == 0 || footstepSource == null) return;

        AudioClip clip = footstepConcrete[Random.Range(0, footstepConcrete.Length)];
        footstepSource.pitch = Random.Range(pitchMin, pitchMax);
        footstepSource.PlayOneShot(clip, isRunning ? runVolume : walkVolume);
    }

    // ================= JUMP & LAND =================

    // Gọi từ FirstPersonController ngay lúc bắt đầu nhảy (lúc set _verticalVelocity)
    public void PlayJumpSound()
    {
        PlayFromArray(jumpClips, jumpVolume);
    }

    // Gọi từ FirstPersonController ngay lúc Grounded chuyển từ false -> true
    public void PlayLandSound()
    {
        PlayFromArray(landClips, landVolume);
    }

    private void PlayFromArray(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0 || footstepSource == null) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        footstepSource.pitch = Random.Range(pitchMin, pitchMax);
        footstepSource.PlayOneShot(clip, volume);
    }

    // ================= BREATHING =================

    private void UpdateBreathing()
    {
        if (playerStamina == null || breathSource == null) return;

        AudioClip target = null;
        float targetVolume = 1f;

        if (playerStamina.isExhausted)
        {
            target = breathExhausted;
            targetVolume = breathExhaustedVolume;
        }
        else if (isRunning)
        {
            target = breathRun;
            targetVolume = breathRunVolume;
        }
        // else: đứng yên/đi bộ bình thường -> không có tiếng thở (im lặng, đúng chất horror)

        if (target == null)
        {
            if (breathSource.isPlaying) breathSource.Stop();
            return;
        }

        if (breathSource.clip == target && breathSource.isPlaying)
        {
            breathSource.volume = targetVolume; // vẫn cập nhật nếu chỉnh số trong Inspector lúc Play
            return;
        }

        breathSource.clip = target;
        breathSource.volume = targetVolume;
        breathSource.loop = true;
        breathSource.Play();
    }

    // ================= LOW HEALTH HEARTBEAT =================

    private void UpdateHeartbeat()
    {
        if (healthSystem == null || heartbeatSource == null || heartbeatClip == null) return;

        float hpPercent = healthSystem.HPPercent;

        if (hpPercent > lowHealthThreshold || healthSystem.IsDead)
        {
            if (heartbeatSource.isPlaying) heartbeatSource.Stop();
            return;
        }

        if (!heartbeatSource.isPlaying)
        {
            heartbeatSource.clip = heartbeatClip;
            heartbeatSource.loop = true;
            heartbeatSource.Play();
        }

        // HP càng thấp trong khoảng [0, lowHealthThreshold] thì tim đập càng to
        float severity = 1f - Mathf.InverseLerp(0f, lowHealthThreshold, hpPercent); // 0 lúc vừa chạm ngưỡng, 1 lúc gần chết
        heartbeatSource.volume = Mathf.Lerp(heartbeatMinVolume, heartbeatMaxVolume, severity);
    }
}