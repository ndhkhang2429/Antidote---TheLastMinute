using System.Collections;
using UnityEngine;

public class EndingFadeController : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float defaultFadeDuration = 1.5f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }
    }

    public void FadeOut()
    {
        StartFade(1f, defaultFadeDuration);
    }

    public void FadeIn()
    {
        StartFade(0f, defaultFadeDuration);
    }

    public void FadeOut(float duration)
    {
        StartFade(1f, duration);
    }

    public void FadeIn(float duration)
    {
        StartFade(0f, duration);
    }

    private void StartFade(float targetAlpha, float duration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        if (targetAlpha > startAlpha)
            fadeCanvasGroup.blocksRaycasts = true;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / duration);

            fadeCanvasGroup.alpha =
                Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
            fadeCanvasGroup.blocksRaycasts = false;

        fadeCoroutine = null;
    }
}