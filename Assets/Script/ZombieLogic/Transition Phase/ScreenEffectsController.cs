using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Screen Effects Controller - Quản lý post-processing effects
/// Yêu cầu: Một Universal Render Pipeline Volume trong scene
/// </summary>
public class ScreenEffectsController : MonoBehaviour
{
    private static ScreenEffectsController _instance;
    public static ScreenEffectsController Instance => _instance;

    [Header("== Post-Processing Volume ==")]
    [SerializeField] private Volume postProcessVolume;

    // Effects references
    private Vignette _vignette;
    private Bloom _bloom;
    private ChromaticAberration _chromaticAberration;
    private FilmGrain _filmGrain;
    private LensDistortion _lensDistortion;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
            Destroy(gameObject);

        if (postProcessVolume == null)
            postProcessVolume = FindObjectOfType<Volume>();

        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out _vignette);
            postProcessVolume.profile.TryGet(out _bloom);
            postProcessVolume.profile.TryGet(out _chromaticAberration);
            postProcessVolume.profile.TryGet(out _filmGrain);
            postProcessVolume.profile.TryGet(out _lensDistortion);
        }
    }

    // ============ VIGNETTE CONTROL ============

    public void SetVignette(float intensity)
    {
        if (_vignette != null)
        {
            _vignette.intensity.value = Mathf.Clamp01(intensity);
        }
    }

    public IEnumerator VignetteTransition(float startIntensity, float endIntensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float intensity = Mathf.Lerp(startIntensity, endIntensity, t);
            SetVignette(intensity);
            yield return null;
        }
        SetVignette(endIntensity);
    }

    // ============ BLOOM CONTROL ============

    public void SetBloom(float intensity, float threshold = 0.9f)
    {
        if (_bloom != null)
        {
            _bloom.intensity.value = intensity;
            _bloom.threshold.value = threshold;
        }
    }

    public IEnumerator BloomPulse(float duration, float maxIntensity = 2f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float intensity = Mathf.Sin(t * Mathf.PI) * maxIntensity;
            SetBloom(intensity);
            yield return null;
        }
        SetBloom(0f);
    }

    // ============ FILM GRAIN CONTROL ============

    public void SetFilmGrain(float intensity)
    {
        if (_filmGrain != null)
        {
            _filmGrain.intensity.value = Mathf.Clamp01(intensity);
        }
    }

    public IEnumerator FilmGrainTransition(float startIntensity, float endIntensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float intensity = Mathf.Lerp(startIntensity, endIntensity, t);
            SetFilmGrain(intensity);
            yield return null;
        }
        SetFilmGrain(endIntensity);
    }

    // ============ CHROMATIC ABERRATION CONTROL ============

    public void SetChromaticAberration(float intensity)
    {
        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.value = Mathf.Clamp01(intensity);
        }
    }

    public IEnumerator ChromaticAbberrationBurst(float duration, float maxIntensity = 0.5f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float intensity = Mathf.Sin(t * Mathf.PI) * maxIntensity;
            SetChromaticAberration(intensity);
            yield return null;
        }
        SetChromaticAberration(0f);
    }

    // ============ LENS DISTORTION CONTROL ============

    public void SetLensDistortion(float intensity)
    {
        if (_lensDistortion != null)
        {
            _lensDistortion.intensity.value = Mathf.Clamp(intensity, -100f, 100f);
        }
    }

    // ============ COMBINED EFFECTS ============

    /// <summary>
    /// Chaos effect - dùng cho moment transformation peak
    /// </summary>
    public IEnumerator ChaosEffect(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Vignette tăng rồi giảm
            float vignetteIntensity = Mathf.Sin(t * Mathf.PI * 2f) * 0.3f + 0.2f;
            SetVignette(vignetteIntensity);

            // Bloom pulsing
            float bloomIntensity = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4f)) * 3f;
            SetBloom(bloomIntensity);

            // Grain increasing
            SetFilmGrain(t * 0.5f);

            // Chromatic aberration burst
            float abberationIntensity = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * 0.4f;
            SetChromaticAberration(abberationIntensity);

            yield return null;
        }

        // Fade back to normal
        yield return StartCoroutine(VignetteTransition(_vignette.intensity.value, 0f, 0.5f));
        SetBloom(0f);
        SetFilmGrain(0f);
        SetChromaticAberration(0f);
    }

    /// <summary>
    /// Dread effect - build tension slowly
    /// </summary>
    public IEnumerator DreadEffect(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Slow vignette increase
            SetVignette(t * 0.25f);

            // Grain increase
            SetFilmGrain(t * 0.15f);

            // Subtle bloom
            SetBloom(t * 0.5f);

            yield return null;
        }
    }

    /// <summary>
    /// Reset all effects to default
    /// </summary>
    public void ResetAllEffects()
    {
        SetVignette(0f);
        SetBloom(0f);
        SetFilmGrain(0f);
        SetChromaticAberration(0f);
        SetLensDistortion(0f);
    }
}