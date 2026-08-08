using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingSceneController : MonoBehaviour
{
    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup victoryGroup;
    [SerializeField] private CanvasGroup messageGroup;
    [SerializeField] private CanvasGroup thankYouGroup;
    [SerializeField] private CanvasGroup continueGroup;

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.7f;
    [SerializeField] private float fadeDuration = 0.8f;

    [SerializeField] private float delayAfterTitle = 0.8f;
    [SerializeField] private float delayAfterVictory = 0.7f;
    [SerializeField] private float delayAfterMessage = 1.0f;
    [SerializeField] private float delayAfterThankYou = 0.8f;

    [Header("Continue Pulse")]
    [SerializeField] private float continueMinAlpha = 0.35f;
    [SerializeField] private float continueMaxAlpha = 1f;
    [SerializeField] private float continuePulseSpeed = 1.4f;

    [Header("Scene Transition")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float inputDelay = 0.5f;

    [Header("Fade Out")]
    [SerializeField] private CanvasGroup fadeOutGroup;
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("Ending Audio")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float audioFadeOutDuration = 1.5f;

    [SerializeField] private float ambienceVolume = 0.12f;
    [SerializeField] private float musicTargetVolume = 0.4f;

    [SerializeField] private float musicStartDelay = 1.2f;
    [SerializeField] private float musicFadeInDuration = 2f;

    private bool canContinue = false;
    private bool isLeaving = false;

    private void Start()
    {
        SetAlpha(titleGroup, 0f);
        SetAlpha(victoryGroup, 0f);
        SetAlpha(messageGroup, 0f);
        SetAlpha(thankYouGroup, 0f);
        SetAlpha(continueGroup, 0f);
        SetAlpha(fadeOutGroup, 0f);

        StartEndingAudio();

        StartCoroutine(EndingSequence());
    }

    private void StartEndingAudio()
    {
        if (ambienceSource != null)
        {
            ambienceSource.volume = ambienceVolume;
            ambienceSource.Play();
        }

        if (musicSource != null)
        {
            musicSource.volume = 0f;
            musicSource.Play();

            StartCoroutine(FadeInMusic());
        }
    }

    private IEnumerator FadeInMusic()
    {
        yield return new WaitForSecondsRealtime(musicStartDelay);

        if (musicSource == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < musicFadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / musicFadeInDuration
            );

            float smoothT = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            musicSource.volume = Mathf.Lerp(
                0f,
                musicTargetVolume,
                smoothT
            );

            yield return null;
        }

        musicSource.volume = musicTargetVolume;
    }

    private void Update()
    {
        if (!canContinue || isLeaving)
            return;

        // Pulse dòng PRESS ANY KEY
        if (continueGroup != null)
        {
            float alpha = Mathf.Lerp(
                continueMinAlpha,
                continueMaxAlpha,
                (Mathf.Sin(Time.unscaledTime * continuePulseSpeed) + 1f) * 0.5f
            );

            continueGroup.alpha = alpha;
        }

        if (Input.anyKeyDown)
        {
            StartCoroutine(ReturnToMainMenu());
        }
    }

    private IEnumerator EndingSequence()
    {
        yield return new WaitForSecondsRealtime(initialDelay);

        yield return FadeCanvasGroup(titleGroup, 0f, 1f, fadeDuration);

        yield return new WaitForSecondsRealtime(delayAfterTitle);

        yield return FadeCanvasGroup(victoryGroup, 0f, 1f, fadeDuration);

        yield return new WaitForSecondsRealtime(delayAfterVictory);

        yield return FadeCanvasGroup(messageGroup, 0f, 1f, fadeDuration);

        yield return new WaitForSecondsRealtime(delayAfterMessage);

        yield return FadeCanvasGroup(thankYouGroup, 0f, 1f, fadeDuration);

        yield return new WaitForSecondsRealtime(delayAfterThankYou);

        yield return FadeCanvasGroup(continueGroup, 0f, 1f, fadeDuration);

        yield return new WaitForSecondsRealtime(inputDelay);

        canContinue = true;

        Debug.Log("[ENDING SCENE] Ready for player input.");
    }

    private IEnumerator ReturnToMainMenu()
    {
        isLeaving = true;
        canContinue = false;

        Debug.Log("[ENDING SCENE] Fading out...");

        StartCoroutine(FadeOutAudio());

        if (fadeOutGroup != null)
        {
            yield return FadeCanvasGroup(
                fadeOutGroup,
                fadeOutGroup.alpha,
                1f,
                fadeOutDuration
            );
        }

        Debug.Log("[ENDING SCENE] Returning to Main Menu...");

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup group,
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;

        group.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            group.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                smoothT
            );

            yield return null;
        }

        group.alpha = targetAlpha;
    }

    private void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
        {
            group.alpha = alpha;
        }
    }

    private IEnumerator FadeOutAudio()
    {
        float startAmbienceVolume =
            ambienceSource != null ? ambienceSource.volume : 0f;

        float startMusicVolume =
            musicSource != null ? musicSource.volume : 0f;

        float elapsed = 0f;

        while (elapsed < audioFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / audioFadeOutDuration
            );

            if (ambienceSource != null)
            {
                ambienceSource.volume =
                    Mathf.Lerp(startAmbienceVolume, 0f, t);
            }

            if (musicSource != null)
            {
                musicSource.volume =
                    Mathf.Lerp(startMusicVolume, 0f, t);
            }

            yield return null;
        }

        if (ambienceSource != null)
            ambienceSource.volume = 0f;

        if (musicSource != null)
            musicSource.volume = 0f;
    }
}