using UnityEngine;

public class WeaponAudioController : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Weapon Audio Clips")]
    [SerializeField] private AudioClip _fireClip;
    [SerializeField] private AudioClip _reloadClip;
    [SerializeField] private AudioClip _emptyClip;
    [SerializeField] private AudioClip _drawClip;
    [SerializeField] private AudioClip _holsterClip;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float _fireVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float _reloadVolume = 0.85f;

    [Range(0f, 1f)]
    [SerializeField] private float _emptyVolume = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float _drawVolume = 0.8f;

    [Range(0f, 1f)]
    [SerializeField] private float _holsterVolume = 0.8f;

    [Header("Pitch Variation")]
    [SerializeField] private bool _randomizeFirePitch = true;

    [SerializeField]
    private Vector2 _firePitchRange =
        new Vector2(0.97f, 1.03f);

    private void Awake()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (_audioSource == null)
        {
            Debug.LogError(
                "[WeaponAudioController] Không tìm thấy AudioSource.",
                this
            );
        }
    }

    public void PlayFire()
    {
        if (_audioSource == null || _fireClip == null)
        {
            return;
        }

        float oldPitch = _audioSource.pitch;

        if (_randomizeFirePitch)
        {
            _audioSource.pitch = Random.Range(
                _firePitchRange.x,
                _firePitchRange.y
            );
        }

        _audioSource.PlayOneShot(
            _fireClip,
            _fireVolume
        );

        _audioSource.pitch = oldPitch;
    }

    public void PlayReload()
    {
        PlayOneShot(_reloadClip, _reloadVolume);
    }

    public void PlayEmpty()
    {
        PlayOneShot(_emptyClip, _emptyVolume);
    }

    public void PlayDraw()
    {
        PlayOneShot(_drawClip, _drawVolume);
    }

    public void PlayHolsterDetached()
    {
        if (_holsterClip == null)
        {
            return;
        }

        GameObject temporaryAudioObject =
            new GameObject("Temporary_Holster_Audio");

        AudioSource temporarySource =
            temporaryAudioObject.AddComponent<AudioSource>();

        temporarySource.clip = _holsterClip;
        temporarySource.volume = _holsterVolume;
        temporarySource.pitch = 1f;

        // Đi qua cùng Audio Mixer Group với vũ khí.
        temporarySource.outputAudioMixerGroup =
            _audioSource != null
                ? _audioSource.outputAudioMixerGroup
                : null;

        // Đây là âm thanh của vũ khí người chơi.
        temporarySource.spatialBlend = 0f;
        temporarySource.reverbZoneMix = 0f;
        temporarySource.playOnAwake = false;
        temporarySource.loop = false;
        temporarySource.priority = 80;

        temporarySource.Play();

        Destroy(
            temporaryAudioObject,
            _holsterClip.length + 0.1f
        );
    }

    private void PlayOneShot(
        AudioClip clip,
        float volume)
    {
        if (_audioSource == null || clip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(
            clip,
            volume
        );
    }
}