using System.Collections;
using UnityEngine;

public class HospitalAmbienceController :
    MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField]
    private AudioSource baseAmbienceSource;

    [SerializeField]
    private AudioSource electricalHumSource;

    [Header("Volume When Power Is Off")]
    [Range(0f, 1f)]
    [SerializeField]
    private float basePowerOffVolume = 0.22f;

    [Header("Volume When Power Is On")]
    [Range(0f, 1f)]
    [SerializeField]
    private float basePowerOnVolume = 0.16f;

    [Range(0f, 1f)]
    [SerializeField]
    private float electricalHumVolume = 0.12f;

    [Header("Transition")]
    [Min(0.1f)]
    [SerializeField]
    private float fadeDuration = 2.5f;

    private bool _lastPowerState;
    private bool _hasInitialState;
    private Coroutine _fadeCoroutine;

    private void Start()
    {
        PrepareSource(baseAmbienceSource);
        PrepareSource(electricalHumSource);

        if (baseAmbienceSource != null)
        {
            baseAmbienceSource.volume = 0f;
            baseAmbienceSource.Play();
        }

        if (electricalHumSource != null)
        {
            electricalHumSource.volume = 0f;
            electricalHumSource.Play();
        }
    }

    private void Update()
    {
        LightingManager lightingManager =
            LightingManager.Instance;

        bool powerIsOn =
            lightingManager != null &&
            lightingManager.IsPowerOn;

        if (!_hasInitialState)
        {
            _hasInitialState = true;
            _lastPowerState = powerIsOn;

            StartAmbienceTransition(
                powerIsOn,
                1.5f
            );

            return;
        }

        if (powerIsOn == _lastPowerState)
        {
            return;
        }

        _lastPowerState = powerIsOn;

        StartAmbienceTransition(
            powerIsOn,
            fadeDuration
        );
    }

    private void PrepareSource(
        AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
    }

    private void StartAmbienceTransition(
        bool powerIsOn,
        float duration)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(
            FadeAmbience(
                powerIsOn,
                duration
            )
        );
    }

    private IEnumerator FadeAmbience(
        bool powerIsOn,
        float duration)
    {
        float startBaseVolume =
            baseAmbienceSource != null
                ? baseAmbienceSource.volume
                : 0f;

        float startHumVolume =
            electricalHumSource != null
                ? electricalHumSource.volume
                : 0f;

        float targetBaseVolume =
            powerIsOn
                ? basePowerOnVolume
                : basePowerOffVolume;

        float targetHumVolume =
            powerIsOn
                ? electricalHumVolume
                : 0f;

        float elapsed = 0f;
        float safeDuration =
            Mathf.Max(0.1f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / safeDuration
            );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            if (baseAmbienceSource != null)
            {
                baseAmbienceSource.volume =
                    Mathf.Lerp(
                        startBaseVolume,
                        targetBaseVolume,
                        smoothProgress
                    );
            }

            if (electricalHumSource != null)
            {
                electricalHumSource.volume =
                    Mathf.Lerp(
                        startHumVolume,
                        targetHumVolume,
                        smoothProgress
                    );
            }

            yield return null;
        }

        if (baseAmbienceSource != null)
        {
            baseAmbienceSource.volume =
                targetBaseVolume;
        }

        if (electricalHumSource != null)
        {
            electricalHumSource.volume =
                targetHumVolume;
        }

        _fadeCoroutine = null;
    }
}