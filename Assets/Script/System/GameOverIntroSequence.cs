using System.Collections;
using UnityEngine;

public class GameOverIntroSequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup[] buttonGroups; // Retry, MainMenu, Exit theo thứ tự
    [SerializeField] private float silenceBeforeTitle = 0.5f;
    [SerializeField] private float titleFadeDuration = 1.2f;
    [SerializeField] private float delayBetweenButtons = 0.25f;
    [SerializeField] private float buttonFadeDuration = 0.4f;
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private void Start()
    {
        titleGroup.alpha = 0f;
        foreach (var g in buttonGroups) g.alpha = 0f;
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(silenceBeforeTitle);

        yield return Fade(titleGroup, 0f, 1f, titleFadeDuration, scalePunch: true);

        yield return new WaitForSeconds(0.4f);

        foreach (var g in buttonGroups)
        {
            yield return Fade(g, 0f, 1f, buttonFadeDuration);
            yield return new WaitForSeconds(delayBetweenButtons);
        }
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration, bool scalePunch = false)
    {
        float t = 0f;
        Vector3 startScale = scalePunch ? Vector3.one * 1.1f : Vector3.one;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            group.alpha = Mathf.Lerp(from, to, p);
            if (scalePunch)
                group.transform.localScale = Vector3.Lerp(startScale, Vector3.one, p);
            yield return null;
        }
        group.alpha = to;
        group.transform.localScale = Vector3.one;
    }
}