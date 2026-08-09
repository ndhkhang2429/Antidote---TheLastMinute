using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicController : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource;

    [Range(0f, 1f)]
    [SerializeField] private float targetVolume = 0.28f;

    [Min(0.1f)]
    [SerializeField] private float fadeInDuration = 2.5f;

    [Min(0.1f)]
    [SerializeField] private float fadeOutDuration = 1.5f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (musicSource == null ||
            musicSource.clip == null)
        {
            return;
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0f;

        musicSource.Play();

        StartFade(
            targetVolume,
            fadeInDuration
        );
    }

    public IEnumerator FadeOut()
    {
        if (musicSource == null ||
            !musicSource.isPlaying)
        {
            yield break;
        }

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        float startVolume =
            musicSource.volume;

        float elapsed = 0f;
        float safeDuration =
            Mathf.Max(0.1f, fadeOutDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / safeDuration
            );

            musicSource.volume =
                Mathf.Lerp(
                    startVolume,
                    0f,
                    progress
                );

            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }

    private void StartFade(
        float target,
        float duration)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(
            FadeVolume(target, duration)
        );
    }

    private IEnumerator FadeVolume(
        float target,
        float duration)
    {
        float startVolume =
            musicSource.volume;

        float elapsed = 0f;
        float safeDuration =
            Mathf.Max(0.1f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / safeDuration
            );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            musicSource.volume =
                Mathf.Lerp(
                    startVolume,
                    target,
                    smoothProgress
                );

            yield return null;
        }

        musicSource.volume = target;
        _fadeCoroutine = null;
    }
}