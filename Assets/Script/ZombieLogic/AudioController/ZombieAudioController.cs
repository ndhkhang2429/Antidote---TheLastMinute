using UnityEngine;

/// <summary>
/// Gắn component này vào CÙNG GameObject với ZombieBase (hoặc subclass của nó)
/// trên MỌI prefab zombie (Normal, Tank, Runner, Axe, Spitter, Boss...).
/// ZombieBase tự tìm và gọi các hàm Play... tại đúng thời điểm (Alert lúc Scream,
/// Hurt lúc TakeDamage, Death lúc Die, Attack qua Animation Event).
/// Không cần sửa gì thêm ở từng subclass.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ZombieAudioController : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Dùng cho các âm thanh phát 1 lần: Alert, Attack, Hurt, Death")]
    [SerializeField] private AudioSource oneShotSource;
    [Tooltip("Dùng riêng cho tiếng rên loop lúc Patrol/Chase")]
    [SerializeField] private AudioSource idleSource;

    [Header("Idle Groan (lúc Patrol / Chase, im lặng khi Combat)")]
    [SerializeField] private AudioClip[] idleGroanClips;
    [SerializeField] private float idleGroanIntervalMin = 4f;
    [SerializeField] private float idleGroanIntervalMax = 9f;
    [SerializeField][Range(0f, 1f)] private float idleGroanVolume = 0.5f;

    [Header("Alert (lúc phát hiện player - Scream)")]
    [SerializeField] private AudioClip[] alertClips;
    [SerializeField][Range(0f, 1f)] private float alertVolume = 1f;

    [Header("Attack (gọi qua Animation Event trên clip Attack)")]
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField][Range(0f, 1f)] private float attackVolume = 0.9f;

    [Header("Hurt (lúc TakeDamage)")]
    [SerializeField] private AudioClip[] hurtClips;
    [SerializeField][Range(0f, 1f)] private float hurtVolume = 0.8f;

    [Header("Death")]
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField][Range(0f, 1f)] private float deathVolume = 1f;

    [Header("Pitch Variation (tránh nghe lặp máy móc)")]
    [SerializeField] private float pitchMin = 0.92f;
    [SerializeField] private float pitchMax = 1.08f;

    private ZombieBase zombieBase;
    private float idleTimer;

    private void Awake()
    {
        zombieBase = GetComponent<ZombieBase>();
        ResetIdleTimer();
    }

    private void Update()
    {
        HandleIdleGroan();
    }

    // ================= IDLE GROAN =================

    private void HandleIdleGroan()
    {
        if (zombieBase == null || zombieBase.IsDead) return;
        if (idleSource == null || idleGroanClips == null || idleGroanClips.Length == 0) return;

        // Không rên khi đang combat, tránh chồng tiếng với attack/scream
        if (zombieBase.IsInCombat) return;

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            AudioClip clip = idleGroanClips[Random.Range(0, idleGroanClips.Length)];
            idleSource.pitch = Random.Range(pitchMin, pitchMax);
            idleSource.PlayOneShot(clip, idleGroanVolume);
            ResetIdleTimer();
        }
    }

    private void ResetIdleTimer()
    {
        idleTimer = Random.Range(idleGroanIntervalMin, idleGroanIntervalMax);
    }

    // ================= ONE-SHOT EVENTS =================

    public void PlayAlert()
    {
        PlayFromArray(alertClips, alertVolume);
    }

    public void PlayAttack()
    {
        PlayFromArray(attackClips, attackVolume);
    }

    public void PlayHurt()
    {
        PlayFromArray(hurtClips, hurtVolume);
    }

    public void PlayDeath()
    {
        PlayFromArray(deathClips, deathVolume);
    }

    /// <summary>
    /// Dùng cho các sự kiện âm thanh riêng của từng loại zombie
    /// (vd: Pounce, Frenzy của ZombieRunner) mà không có sẵn field chuẩn ở đây.
    /// Subclass tự khai báo mảng AudioClip riêng, rồi gọi hàm này qua oneShotSource chung.
    /// </summary>
    public void PlaySound(AudioClip[] clips, float volume = 1f)
    {
        PlayFromArray(clips, volume);
    }

    private void PlayFromArray(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0 || oneShotSource == null) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        oneShotSource.pitch = Random.Range(pitchMin, pitchMax);
        oneShotSource.PlayOneShot(clip, volume);
    }
}