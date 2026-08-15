using UnityEngine;

public class WardenSimpleAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private HealthSystem healthSystem;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip roarClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float roarVolume = 0.9f;

    [Range(0f, 1f)]
    [SerializeField] private float attackVolume = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float deathVolume = 1f;

    private bool _roarPlayed;
    private bool _deathPlayed;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= PlayDeath;
            healthSystem.OnDeath += PlayDeath;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.OnDeath -= PlayDeath;
    }

    /// <summary>
    /// Được ZombieWardenOne gọi khi bắt đầu chiến đấu.
    /// </summary>
    public void PlayRoar()
    {
        if (_roarPlayed || _deathPlayed)
            return;

        _roarPlayed = true;

        PlayClip(
            roarClip,
            roarVolume
        );
    }

    /// <summary>
    /// Được gọi bằng Animation Event trong Attack1–5.
    /// </summary>
    public void PlayAttack()
    {
        if (_deathPlayed)
            return;

        PlayClip(
            attackClip,
            attackVolume
        );
    }

    /// <summary>
    /// Tự động được gọi khi HealthSystem phát OnDeath.
    /// </summary>
    public void PlayDeath()
    {
        if (_deathPlayed)
            return;

        _deathPlayed = true;

        if (audioSource == null ||
            deathClip == null)
        {
            return;
        }

        /*
         * Dừng Roar hoặc Attack đang phát để tiếng Death
         * được nghe rõ.
         */
        audioSource.Stop();

        audioSource.pitch = 1f;

        audioSource.PlayOneShot(
            deathClip,
            deathVolume
        );
    }

    private void PlayClip(
        AudioClip clip,
        float volume)
    {
        if (audioSource == null ||
            clip == null)
        {
            return;
        }

        audioSource.pitch =
            Random.Range(0.97f, 1.03f);

        audioSource.PlayOneShot(
            clip,
            volume
        );
    }
}