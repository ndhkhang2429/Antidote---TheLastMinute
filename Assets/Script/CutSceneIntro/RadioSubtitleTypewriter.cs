using System.Collections;
using TMPro;
using UnityEngine;

public class RadioSubtitleTypewriter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Typewriter")]
    [Tooltip("Thời gian giữa mỗi ký tự. Nhỏ hơn sẽ chạy nhanh hơn.")]
    [SerializeField, Min(0.005f)]
    private float characterInterval = 0.025f;

    [Tooltip("Thời gian subtitle fade vào.")]
    [SerializeField, Min(0f)]
    private float fadeInDuration = 0.12f;

    [Tooltip("Thời gian subtitle fade ra.")]
    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.15f;

    [Header("Radio Lines")]
    [TextArea(2, 4)]
    [SerializeField]
    private string line1 =
        "Attention... This is Coldstone Medical Center Emergency Broadcast.";

    [TextArea(2, 4)]
    [SerializeField]
    private string line2 =
        "If anyone can hear this... The evacuation helicopter will arrive at the hospital rooftop.";

    [TextArea(2, 4)]
    [SerializeField]
    private string line3 =
        "Estimated arrival time... Fifteen minutes.";

    [TextArea(2, 4)]
    [SerializeField]
    private string line4 =
        "All remaining survivors... Proceed to the rooftop immediately.";

    [TextArea(2, 4)]
    [SerializeField]
    private string line5 =
        "This is your final evacuation opportunity. May God be with you. End of transmission.";

    private Coroutine currentCoroutine;

    private void Awake()
    {
        HideImmediately();
    }

    public void ShowLine1()
    {
        StartLine(line1);
    }

    public void ShowLine2()
    {
        StartLine(line2);
    }

    public void ShowLine3()
    {
        StartLine(line3);
    }

    public void ShowLine4()
    {
        StartLine(line4);
    }

    public void ShowLine5()
    {
        StartLine(line5);
    }

    public void ClearSubtitle()
    {
        StopCurrentCoroutine();
        currentCoroutine = StartCoroutine(ClearRoutine());
    }

    private void StartLine(string line)
    {
        StopCurrentCoroutine();
        currentCoroutine = StartCoroutine(TypeLineRoutine(line));
    }

    private IEnumerator TypeLineRoutine(string line)
    {
        subtitleText.text = line;
        subtitleText.maxVisibleCharacters = 0;

        // Buộc TMP tính chính xác số ký tự hiển thị.
        subtitleText.ForceMeshUpdate();

        int visibleCharacterCount =
            subtitleText.textInfo.characterCount;

        yield return FadeCanvasGroup(1f, fadeInDuration);

        for (int i = 1; i <= visibleCharacterCount; i++)
        {
            subtitleText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(characterInterval);
        }

        subtitleText.maxVisibleCharacters =
            visibleCharacterCount;

        currentCoroutine = null;
    }

    private IEnumerator ClearRoutine()
    {
        yield return FadeCanvasGroup(0f, fadeOutDuration);

        subtitleText.text = string.Empty;
        subtitleText.maxVisibleCharacters = 0;

        currentCoroutine = null;
    }

    private IEnumerator FadeCanvasGroup(
        float targetAlpha,
        float duration)
    {
        float startAlpha = subtitleCanvasGroup.alpha;

        if (duration <= 0f)
        {
            subtitleCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                timer / duration
            );

            subtitleCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                progress
            );

            yield return null;
        }

        subtitleCanvasGroup.alpha = targetAlpha;
    }

    private void StopCurrentCoroutine()
    {
        if (currentCoroutine == null)
        {
            return;
        }

        StopCoroutine(currentCoroutine);
        currentCoroutine = null;
    }

    private void HideImmediately()
    {
        if (subtitleCanvasGroup != null)
        {
            subtitleCanvasGroup.alpha = 0f;
        }

        if (subtitleText != null)
        {
            subtitleText.text = string.Empty;
            subtitleText.maxVisibleCharacters = 0;
        }
    }

    private void OnDisable()
    {
        StopCurrentCoroutine();
        HideImmediately();
    }
}