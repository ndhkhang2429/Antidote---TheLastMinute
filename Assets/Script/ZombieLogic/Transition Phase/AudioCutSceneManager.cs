using System.Collections;
using UnityEngine;

/// <summary>
/// Audio Cutscene Manager - Quản lý tất cả âm thanh cho phase transition
/// Gồm: heartbeat, ambient dread, roar, transformation sounds, music crossfade
/// </summary>
public class AudioCutsceneManager : MonoBehaviour
{
    private static AudioCutsceneManager _instance;
    public static AudioCutsceneManager Instance => _instance;

    [Header("== AUDIO SOURCES ==")]
    [SerializeField] private AudioSource sfxSource; // Cho SFX (roar, tearing, etc.)
    [SerializeField] private AudioSource ambienceSource; // Cho ambient/heartbeat
    [SerializeField] private AudioSource musicSource; // Cho music fade

    [Header("== ROAR & TRANSFORMATION ==")]
    [SerializeField] private AudioClip roarClip;
    [SerializeField] private float roarVolume = 0.8f;
    [Tooltip("Thời gian roar bắt đầu (tính từ bắt đầu cutscene)")]
    [SerializeField] private float roarStartTime = 5.0f;

    [Header("== HEARTBEAT ==")]
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private float heartbeatVolume = 0.4f;
    [Tooltip("Khi nào heartbeat bắt đầu")]
    [SerializeField] private float heartbeatStartTime = 0.5f;
    [Tooltip("Khi nào heartbeat tăng tốc")]
    [SerializeField] private float heartbeatAccelTime = 2.0f;

    [Header("== TRANSFORMATION SOUNDS ==")]
    [SerializeField] private AudioClip bonesCrackClip;
    [SerializeField] private AudioClip fleshTearClip;
    [SerializeField] private AudioClip electricChargeClip;
    [SerializeField] private float transformationSfxVolume = 0.6f;

    [Header("== MUSIC ==")]
    [SerializeField] private AudioClip dreadMusicClip; // Low drone/tension
    [SerializeField] private AudioClip combatMusicClip; // Phase 2 combat theme
    [SerializeField] private float musicFadeDuration = 1.5f;

    [Header("== VOICE EFFECTS ==")]
    [SerializeField] private bool useVoiceDistortion = true;
    [Tooltip("Distortion intensity khi roar")]
    [SerializeField] private float roarDistortionIntensity = 0.7f;

    private AudioSource _heartbeatSource;
    private bool _isPlayingHeartbeat = false;
    private float _heartbeatSpeed = 1f;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
            Destroy(gameObject);

        // Auto-create AudioSources nếu không assign
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_Source");
            sfxObj.transform.parent = transform;
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f; // 2D audio
        }

        if (ambienceSource == null)
        {
            GameObject ambObj = new GameObject("Ambience_Source");
            ambObj.transform.parent = transform;
            ambienceSource = ambObj.AddComponent<AudioSource>();
            ambienceSource.spatialBlend = 0f;
            ambienceSource.loop = false;
        }

        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("Music_Source");
            musicObj.transform.parent = transform;
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
        }

        // Heartbeat source riêng (để control speed)
        GameObject heartObj = new GameObject("Heartbeat_Source");
        heartObj.transform.parent = transform;
        _heartbeatSource = heartObj.AddComponent<AudioSource>();
        _heartbeatSource.spatialBlend = 0f;
        _heartbeatSource.clip = heartbeatClip;
    }

    /// <summary>
    /// Start cutscene audio sequence
    /// Gọi từ CutscenePhaseTransition lúc bắt đầu
    /// </summary>
    public void StartCutsceneAudio()
    {
        StartCoroutine(AudioSequence());
    }

    private IEnumerator AudioSequence()
    {
        // === ACT 1: DREAD BUILDING ===
        yield return StartCoroutine(Act1_Dread());

        // === ACT 2: AWAKENING ===
        yield return StartCoroutine(Act2_Awakening());

        // === ACT 3: TRANSFORMATION ===
        yield return StartCoroutine(Act3_Transformation());

        // === ACT 4: DOMINANCE ===
        yield return StartCoroutine(Act4_Dominance());
    }

    // ============ ACT 1: FALSE VICTORY ============

    private IEnumerator Act1_Dread()
    {
        // Fade out ambient music
        if (musicSource.isPlaying)
        {
            yield return StartCoroutine(FadeAudioSource(musicSource, 0f, 0.5f));
            musicSource.Stop();
        }

        // Start heartbeat (soft)
        yield return new WaitForSeconds(heartbeatStartTime);
        StartHeartbeat(1f);

        yield return new WaitForSeconds(1.5f);
    }

    // ============ ACT 2: THE AWAKENING ============

    private IEnumerator Act2_Awakening()
    {
        float durationOfAct = 2.5f;
        float elapsed = 0f;

        // Heartbeat accelerates as boss wakes
        while (elapsed < durationOfAct)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / durationOfAct;

            // Accelerate heartbeat
            _heartbeatSpeed = Mathf.Lerp(1f, 2f, t);
            if (_heartbeatSource != null)
                _heartbeatSource.pitch = _heartbeatSpeed;

            yield return null;
        }

        // Single loud heartbeat beat moment (horror cliché)
        yield return new WaitForSeconds(0.3f);
        if (sfxSource != null)
        {
            // "LUB-DUB" spike
            _heartbeatSpeed = 2.5f;
            _heartbeatSource.pitch = _heartbeatSpeed;
        }

        yield return new WaitForSeconds(0.2f);
    }

    // ============ ACT 3: THE METAMORPHOSIS ============

    private IEnumerator Act3_Transformation()
    {
        // Stop heartbeat (silence before storm)
        StopHeartbeat();
        yield return new WaitForSeconds(0.3f);

        // === ROAR ===
        if (roarClip != null && sfxSource != null)
        {
            sfxSource.clip = roarClip;
            sfxSource.volume = roarVolume;
            sfxSource.Play();

            // Roar runs for duration
            yield return new WaitForSeconds(roarClip.length);
        }

        // === TRANSFORMATION SOUNDS (bones, flesh, electricity) ===
        // Bones crack
        if (bonesCrackClip != null)
        {
            yield return StartCoroutine(PlaySFXAt(bonesCrackClip, 0.3f, transformationSfxVolume));
        }

        // Flesh tear
        if (fleshTearClip != null)
        {
            yield return StartCoroutine(PlaySFXAt(fleshTearClip, 0.8f, transformationSfxVolume));
        }

        // Electricity charge (long)
        if (electricChargeClip != null)
        {
            yield return StartCoroutine(PlaySFXAt(electricChargeClip, 1.2f, transformationSfxVolume * 0.8f));
        }

        yield return new WaitForSeconds(0.5f);
    }

    // ============ ACT 4: DOMINANCE ============

    private IEnumerator Act4_Dominance()
    {
        // Boss breathing heavy (use roar clip at low volume)
        if (roarClip != null && ambienceSource != null)
        {
            ambienceSource.clip = roarClip;
            ambienceSource.volume = 0.2f;
            ambienceSource.pitch = 0.8f; // Lower pitch for breathing
            ambienceSource.Play();
            yield return new WaitForSeconds(1.5f);
            yield return StartCoroutine(FadeAudioSource(ambienceSource, 0f, 0.5f));
        }

        // Combat music starts
        if (combatMusicClip != null)
        {
            musicSource.clip = combatMusicClip;
            musicSource.volume = 0f;
            musicSource.Play();
            yield return StartCoroutine(FadeAudioSource(musicSource, 0.7f, musicFadeDuration));
        }
    }

    // ============ HELPER METHODS ============

    /// <summary>
    /// Play một SFX clip tại thời điểm cụ thể
    /// </summary>
    private IEnumerator PlaySFXAt(AudioClip clip, float delay, float volume)
    {
        yield return new WaitForSeconds(delay);
        if (sfxSource != null && clip != null)
        {
            sfxSource.clip = clip;
            sfxSource.volume = volume;
            sfxSource.pitch = 1f;
            sfxSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
    }

    /// <summary>
    /// Fade audio source từ current volume đến target
    /// </summary>
    private IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        source.volume = targetVolume;
    }

    /// <summary>
    /// Bắt đầu heartbeat
    /// </summary>
    private void StartHeartbeat(float speed)
    {
        if (_heartbeatSource == null || heartbeatClip == null) return;

        _isPlayingHeartbeat = true;
        _heartbeatSpeed = speed;
        _heartbeatSource.volume = heartbeatVolume;
        _heartbeatSource.pitch = speed;
        _heartbeatSource.Play();
    }

    /// <summary>
    /// Dừng heartbeat
    /// </summary>
    private void StopHeartbeat()
    {
        if (_heartbeatSource != null)
        {
            _heartbeatSource.Stop();
            _isPlayingHeartbeat = false;
        }
    }

    /// <summary>
    /// Apply voice distortion effect (nếu muốn, có thể dùng Audio Mixer filter)
    /// </summary>
    private void ApplyRoarDistortion(float intensity)
    {
        if (useVoiceDistortion && sfxSource != null)
        {
            // Unity không có built-in distortion, nhưng có thể:
            // 1. Dùng Audio Mixer với Distortion effect
            // 2. Hoặc process audio offline + import distorted clip
            // Placeholder: increase pitch slightly để "rougher" sound
            sfxSource.pitch = 1f + (intensity * 0.2f);
        }
    }

    /// <summary>
    /// Stop tất cả audio (khi cutscene kết thúc)
    /// </summary>
    public void StopAllAudio()
    {
        if (sfxSource != null) sfxSource.Stop();
        if (ambienceSource != null) ambienceSource.Stop();
        if (_heartbeatSource != null) _heartbeatSource.Stop();
        // Music continues
    }

    /// <summary>
    /// Mute tất cả audio (useful cho pause menu)
    /// </summary>
    public void MuteAllAudio(bool mute)
    {
        if (sfxSource != null) sfxSource.mute = mute;
        if (ambienceSource != null) ambienceSource.mute = mute;
        if (_heartbeatSource != null) _heartbeatSource.mute = mute;
        if (musicSource != null) musicSource.mute = mute;
    }

    /// <summary>
    /// Get heartbeat speed (useful cho debugging)
    /// </summary>
    public float GetHeartbeatSpeed() => _heartbeatSpeed;
}