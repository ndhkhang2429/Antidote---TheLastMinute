using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class BossBattleMusicController : MonoBehaviour
{
    [Header("Music Sources")]
    [SerializeField] private AudioSource phase1MusicSource;
    [SerializeField] private AudioSource phase2MusicSource;

    [Header("Boss References")]
    [SerializeField] private HealthSystem bossHealthSystem;

    [Header("Cutscene Directors")]
    [SerializeField] private PlayableDirector introDirector;
    [SerializeField] private PlayableDirector phase2Director;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float phase1Volume = 0.4f;

    [Range(0f, 1f)]
    [SerializeField] private float phase2Volume = 0.55f;

    [Header("Fade Duration")]
    [Min(0.01f)]
    [SerializeField] private float phase1FadeInDuration = 2f;

    [Min(0.01f)]
    [SerializeField] private float phase2FadeInDuration = 1.5f;

    [Min(0.01f)]
    [SerializeField] private float transitionFadeOutDuration = 0.8f;

    [Min(0.01f)]
    [SerializeField] private float deathFadeOutDuration = 2f;

    private Coroutine _musicRoutine;
    private bool _phase2Started;
    private bool _bossDead;
    private bool _eventsRegistered;

    private void Awake()
    {
        PrepareSource(phase1MusicSource);
        PrepareSource(phase2MusicSource);
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();

        if (_musicRoutine != null)
        {
            StopCoroutine(_musicRoutine);
            _musicRoutine = null;
        }
    }

    private void RegisterEvents()
    {
        if (_eventsRegistered)
            return;

        if (introDirector != null)
            introDirector.stopped += HandleIntroStopped;

        if (phase2Director != null)
        {
            phase2Director.played += HandlePhase2Played;
            phase2Director.stopped += HandlePhase2Stopped;
        }

        if (bossHealthSystem != null)
            bossHealthSystem.OnDeath += HandleBossDeath;

        _eventsRegistered = true;
    }

    private void UnregisterEvents()
    {
        if (!_eventsRegistered)
            return;

        if (introDirector != null)
            introDirector.stopped -= HandleIntroStopped;

        if (phase2Director != null)
        {
            phase2Director.played -= HandlePhase2Played;
            phase2Director.stopped -= HandlePhase2Stopped;
        }

        if (bossHealthSystem != null)
            bossHealthSystem.OnDeath -= HandleBossDeath;

        _eventsRegistered = false;
    }

    private void HandleIntroStopped(PlayableDirector director)
    {
        if (_bossDead || _phase2Started)
            return;

        StartPhase1Music();
    }

    private void HandlePhase2Played(PlayableDirector director)
    {
        if (_bossDead)
            return;

        _phase2Started = true;
        FadeOutForPhase2Transition();
    }

    private void HandlePhase2Stopped(PlayableDirector director)
    {
        if (_bossDead)
            return;

        StartPhase2Music();
    }

    private void HandleBossDeath()
    {
        if (_bossDead)
            return;

        _bossDead = true;
        StopMusicOnBossDeath();
    }

    public void StartPhase1Music()
    {
        if (_bossDead || _phase2Started)
            return;

        StartMusicRoutine(
            phase1MusicSource,
            phase1Volume,
            phase1FadeInDuration,
            phase2MusicSource);
    }

    public void FadeOutForPhase2Transition()
    {
        StartFadeOutRoutine(
            transitionFadeOutDuration,
            true);
    }

    public void StartPhase2Music()
    {
        if (_bossDead)
            return;

        _phase2Started = true;

        StartMusicRoutine(
            phase2MusicSource,
            phase2Volume,
            phase2FadeInDuration,
            phase1MusicSource);
    }

    public void StopMusicOnBossDeath()
    {
        StartFadeOutRoutine(
            deathFadeOutDuration,
            true);
    }

    public void ResetMusic()
    {
        if (_musicRoutine != null)
        {
            StopCoroutine(_musicRoutine);
            _musicRoutine = null;
        }

        _bossDead = false;
        _phase2Started = false;

        StopAndResetSource(phase1MusicSource);
        StopAndResetSource(phase2MusicSource);
    }

    private void StartMusicRoutine(
        AudioSource sourceToPlay,
        float targetVolume,
        float fadeDuration,
        AudioSource sourceToStop)
    {
        if (_musicRoutine != null)
            StopCoroutine(_musicRoutine);

        _musicRoutine = StartCoroutine(
            SwitchMusicRoutine(
                sourceToPlay,
                targetVolume,
                fadeDuration,
                sourceToStop));
    }

    private IEnumerator SwitchMusicRoutine(
        AudioSource sourceToPlay,
        float targetVolume,
        float fadeDuration,
        AudioSource sourceToStop)
    {
        if (sourceToStop != null)
        {
            sourceToStop.Stop();
            sourceToStop.volume = 0f;
        }

        if (sourceToPlay == null)
        {
            _musicRoutine = null;
            yield break;
        }

        sourceToPlay.volume = 0f;

        if (!sourceToPlay.isPlaying)
            sourceToPlay.Play();

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / fadeDuration);

            sourceToPlay.volume = Mathf.Lerp(
                0f,
                targetVolume,
                progress);

            yield return null;
        }

        sourceToPlay.volume = targetVolume;
        _musicRoutine = null;
    }

    private void StartFadeOutRoutine(
        float duration,
        bool stopAfterFade)
    {
        if (_musicRoutine != null)
            StopCoroutine(_musicRoutine);

        _musicRoutine = StartCoroutine(
            FadeOutAllRoutine(
                duration,
                stopAfterFade));
    }

    private IEnumerator FadeOutAllRoutine(
        float duration,
        bool stopAfterFade)
    {
        float phase1StartVolume =
            phase1MusicSource != null
                ? phase1MusicSource.volume
                : 0f;

        float phase2StartVolume =
            phase2MusicSource != null
                ? phase2MusicSource.volume
                : 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / duration);

            if (phase1MusicSource != null)
            {
                phase1MusicSource.volume = Mathf.Lerp(
                    phase1StartVolume,
                    0f,
                    progress);
            }

            if (phase2MusicSource != null)
            {
                phase2MusicSource.volume = Mathf.Lerp(
                    phase2StartVolume,
                    0f,
                    progress);
            }

            yield return null;
        }

        if (phase1MusicSource != null)
            phase1MusicSource.volume = 0f;

        if (phase2MusicSource != null)
            phase2MusicSource.volume = 0f;

        if (stopAfterFade)
        {
            if (phase1MusicSource != null)
                phase1MusicSource.Stop();

            if (phase2MusicSource != null)
                phase2MusicSource.Stop();
        }

        _musicRoutine = null;
    }

    private void PrepareSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }

    private void StopAndResetSource(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.volume = 0f;
        source.pitch = 1f;
    }
}