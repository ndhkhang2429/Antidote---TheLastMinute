using System.Collections;
using TMPro;
using UnityEngine;

public class ObjectiveUIItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI checkmarkText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField]
    private Color completedColor =
        new Color(0.45f, 0.85f, 0.45f, 1f);

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.3f;

    public string ObjectiveID { get; private set; }

    private Coroutine fadeCoroutine;

    public void Initialize(
    string objectiveID,
    string description)
    {
        ObjectiveID = objectiveID;

        objectiveText.text = description;
        objectiveText.color = activeColor;
        objectiveText.fontStyle = FontStyles.Normal;
        objectiveText.alignment =
            TextAlignmentOptions.Right;

        if (checkmarkText != null)
        {
            checkmarkText.text = "[DONE]";
            checkmarkText.alignment =
                TextAlignmentOptions.Right;
            checkmarkText.gameObject.SetActive(false);
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);

        StartFade(1f);
    }

    public void SetDescription(string newDescription)
    {
        objectiveText.text = newDescription;
    }

    public void MarkCompleted()
    {
        if (checkmarkText != null)
            checkmarkText.gameObject.SetActive(true);

        objectiveText.color = completedColor;
        objectiveText.fontStyle = FontStyles.Strikethrough;
    }

    public IEnumerator FadeOutAndDestroy(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                0f,
                timer / fadeDuration
            );

            yield return null;
        }

        canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(
            FadeCanvas(targetAlpha)
        );
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                timer / fadeDuration
            );

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
    }
}