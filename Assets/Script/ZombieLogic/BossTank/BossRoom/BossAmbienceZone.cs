using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossAmbienceZone : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource ambienceSource;

    [Header("Fade")]
    [Range(0f, 1f)]
    [SerializeField] private float targetVolume = 0.45f;

    [Min(0.01f)]
    [SerializeField] private float fadeInDuration = 2f;

    [Min(0.01f)]
    [SerializeField] private float fadeOutDuration = 2f;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    private Coroutine fadeCoroutine;
    private int playerCollidersInside;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;

        if (ambienceSource != null)
        {
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ambienceSource.volume = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerCollidersInside++;

        if (playerCollidersInside == 1)
            FadeIn();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerCollidersInside =
            Mathf.Max(0, playerCollidersInside - 1);

        if (playerCollidersInside == 0)
            FadeOut();
    }

    private void FadeIn()
    {
        if (ambienceSource == null)
            return;

        if (!ambienceSource.isPlaying)
        {
            ambienceSource.volume = 0f;
            ambienceSource.Play();
        }

        StartFade(targetVolume, fadeInDuration, false);
    }

    private void FadeOut()
    {
        if (ambienceSource == null)
            return;

        StartFade(0f, fadeOutDuration, true);
    }

    private void StartFade(
        float destinationVolume,
        float duration,
        bool stopAfterFade)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(
            FadeRoutine(
                destinationVolume,
                duration,
                stopAfterFade));
    }

    private IEnumerator FadeRoutine(
        float destinationVolume,
        float duration,
        bool stopAfterFade)
    {
        float startingVolume = ambienceSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / duration);

            ambienceSource.volume = Mathf.Lerp(
                startingVolume,
                destinationVolume,
                progress);

            yield return null;
        }

        ambienceSource.volume = destinationVolume;

        if (stopAfterFade &&
            Mathf.Approximately(destinationVolume, 0f))
        {
            ambienceSource.Stop();
        }

        fadeCoroutine = null;
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = null;
        playerCollidersInside = 0;

        if (ambienceSource != null)
        {
            ambienceSource.Stop();
            ambienceSource.volume = 0f;
        }
    }
}