using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class BossCombatAudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource movementSource;
    [SerializeField] private AudioSource impactSource;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepClips;

    [Range(0f, 1f)]
    [SerializeField] private float footstepVolume = 0.55f;

    [Header("Melee")]
    [SerializeField] private AudioClip meleeSwingClip;
    [SerializeField] private AudioClip meleeImpactClip;

    [Range(0f, 1f)]
    [SerializeField] private float meleeSwingVolume = 0.65f;

    [Range(0f, 1f)]
    [SerializeField] private float meleeImpactVolume = 0.75f;

    [Header("Stomp")]
    [SerializeField] private AudioClip stompImpactClip;

    [Range(0f, 1f)]
    [SerializeField] private float stompVolume = 0.85f;

    [Header("Jump Attack")]
    [SerializeField] private AudioClip jumpTakeoffClip;
    [SerializeField] private AudioClip jumpLandingClip;

    [Range(0f, 1f)]
    [SerializeField] private float jumpTakeoffVolume = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float jumpLandingVolume = 1f;

    [Header("Voice")]
    [SerializeField] private AudioClip chargeRoarClip;
    [SerializeField] private AudioClip summonRoarClip;
    [SerializeField] private AudioClip[] hurtClips;
    [SerializeField] private AudioClip deathRoarClip;

    [Range(0f, 1f)]
    [SerializeField] private float chargeRoarVolume = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float summonRoarVolume = 0.8f;

    [Range(0f, 1f)]
    [SerializeField] private float hurtVolume = 0.55f;

    [Range(0f, 1f)]
    [SerializeField] private float deathRoarVolume = 0.9f;

    [Header("Death Impact")]
    [SerializeField] private AudioClip bodyFallClip;

    [Range(0f, 1f)]
    [SerializeField] private float bodyFallVolume = 0.9f;

    [Header("Randomization")]
    [SerializeField]
    private Vector2 footstepPitchRange =
        new Vector2(0.9f, 1.05f);

    [SerializeField]
    private Vector2 hurtPitchRange =
        new Vector2(0.92f, 1.05f);

    [SerializeField]
    private Vector2 meleeSwingPitchRange =
        new Vector2(0.95f, 1.05f);

    [Min(0f)]
    [SerializeField] private float hurtCooldown = 0.45f;

    private HealthSystem _healthSystem;
    private float _nextHurtTime;
    private bool _deathAudioPlayed;
    private bool _healthEventsRegistered;

    private void Awake()
    {
        _healthSystem = GetComponent<HealthSystem>();

        ResetSourcePitch(voiceSource);
        ResetSourcePitch(movementSource);
        ResetSourcePitch(impactSource);
    }

    private void OnEnable()
    {
        RegisterHealthEvents();
    }

    private void OnDisable()
    {
        UnregisterHealthEvents();
    }

    private void RegisterHealthEvents()
    {
        if (_healthEventsRegistered)
            return;

        if (_healthSystem == null)
            _healthSystem = GetComponent<HealthSystem>();

        if (_healthSystem == null)
        {
            Debug.LogWarning(
                "[BossAudio] Không tìm thấy HealthSystem.",
                this);

            return;
        }

        _healthSystem.OnDamaged += HandleBossDamaged;
        _healthSystem.OnDeath += HandleBossDeath;

        _healthEventsRegistered = true;
    }

    private void UnregisterHealthEvents()
    {
        if (!_healthEventsRegistered ||
            _healthSystem == null)
        {
            return;
        }

        _healthSystem.OnDamaged -= HandleBossDamaged;
        _healthSystem.OnDeath -= HandleBossDeath;

        _healthEventsRegistered = false;
    }

    private void HandleBossDamaged(
        float currentHP,
        float maxHP)
    {
        // Đòn cuối sẽ được OnDeath xử lý.
        // Không phát Hurt nếu HP đã về 0.
        if (currentHP <= 0f)
            return;

        PlayHurt();
    }

    private void HandleBossDeath()
    {
        PlayDeathRoar();
    }

    // =========================================================
    // ANIMATION EVENTS
    // =========================================================

    public void Event_PlayBossFootstep()
    {
        AudioClip clip = GetRandomClip(footstepClips);

        PlayRandomPitch(
            movementSource,
            clip,
            footstepVolume,
            footstepPitchRange);
    }

    public void Event_PlayBossMeleeSwing()
    {
        PlayRandomPitch(
            movementSource,
            meleeSwingClip,
            meleeSwingVolume,
            meleeSwingPitchRange);
    }

    public void Event_PlayBossMeleeImpact()
    {
        PlayMeleeImpact();
    }

    public void Event_PlayBossStompImpact()
    {
        PlayOneShot(
            impactSource,
            stompImpactClip,
            stompVolume);
    }

    public void Event_PlayBossJumpTakeoff()
    {
        PlayOneShot(
            movementSource,
            jumpTakeoffClip,
            jumpTakeoffVolume);
    }

    public void Event_PlayBossJumpLanding()
    {
        PlayOneShot(
            impactSource,
            jumpLandingClip,
            jumpLandingVolume);
    }

    public void Event_PlayBossChargeRoar()
    {
        PlayChargeRoar();
    }

    public void Event_PlayBossSummonRoar()
    {
        PlayOneShot(
            voiceSource,
            summonRoarClip,
            summonRoarVolume);
    }

    public void Event_PlayBossBodyFall()
    {
        PlayOneShot(
            impactSource,
            bodyFallClip,
            bodyFallVolume);
    }

    // =========================================================
    // PUBLIC AUDIO METHODS
    // =========================================================

    public void PlayMeleeImpact()
    {
        if (_deathAudioPlayed)
            return;

        PlayOneShot(
            impactSource,
            meleeImpactClip,
            meleeImpactVolume);
    }

    public void PlayChargeRoar()
    {
        if (_deathAudioPlayed)
            return;

        PlayOneShot(
            voiceSource,
            chargeRoarClip,
            chargeRoarVolume);
    }

    public void PlayHurt()
    {
        if (_deathAudioPlayed)
            return;

        if (Time.time < _nextHurtTime)
            return;

        AudioClip clip = GetRandomClip(hurtClips);

        if (clip == null)
            return;

        _nextHurtTime = Time.time + hurtCooldown;

        PlayRandomPitch(
            voiceSource,
            clip,
            hurtVolume,
            hurtPitchRange);
    }

    public void PlayDeathRoar()
    {
        if (_deathAudioPlayed)
            return;

        _deathAudioPlayed = true;

        // Ngừng hurt/charge/summon đang phát để Death Roar rõ ràng.
        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.pitch = 1f;

            if (deathRoarClip != null)
            {
                voiceSource.PlayOneShot(
                    deathRoarClip,
                    deathRoarVolume);
            }
        }
    }

    // Dùng nếu boss được reset để test lại.
    public void ResetBossAudioState()
    {
        _deathAudioPlayed = false;
        _nextHurtTime = 0f;

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.pitch = 1f;
        }

        ResetSourcePitch(movementSource);
        ResetSourcePitch(impactSource);
    }

    // =========================================================
    // PRIVATE HELPERS
    // =========================================================

    private void PlayOneShot(
        AudioSource source,
        AudioClip clip,
        float volume)
    {
        if (source == null || clip == null)
            return;

        source.pitch = 1f;
        source.PlayOneShot(clip, volume);
    }

    private void PlayRandomPitch(
        AudioSource source,
        AudioClip clip,
        float volume,
        Vector2 pitchRange)
    {
        if (source == null || clip == null)
            return;

        float minimumPitch =
            Mathf.Min(pitchRange.x, pitchRange.y);

        float maximumPitch =
            Mathf.Max(pitchRange.x, pitchRange.y);

        source.pitch = Random.Range(
            minimumPitch,
            maximumPitch);

        source.PlayOneShot(clip, volume);
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        int randomIndex = Random.Range(0, clips.Length);
        return clips[randomIndex];
    }

    private void ResetSourcePitch(AudioSource source)
    {
        if (source != null)
            source.pitch = 1f;
    }
}