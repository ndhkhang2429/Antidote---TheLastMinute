using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeadRoofIntroSlideshow : MonoBehaviour
{
    [System.Serializable]
    public class StorySlide
    {
        [Header("Nội dung slide")]
        public Sprite image;

        [TextArea(2, 5)]
        public string storyText;

        [Min(0.5f)]
        public float displayDuration = 3.5f;
    }

    [Header("Story")]
    [SerializeField] private StorySlide[] slides;

    [Header("UI")]
    [SerializeField] private CanvasGroup introCanvasGroup;
    [SerializeField] private Image storyImage;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private GameObject skipText;
    [SerializeField] private TypewriterText storyTypewriter;

    [Header("Skip / Next Slide")]
    [SerializeField] private KeyCode nextSlideKey = KeyCode.Space;
    [SerializeField] private string nextSlideMessage = "Press SPACE for next";

    [Header("Timing")]
    [SerializeField] private float imageFadeDuration = 0.8f;
    [SerializeField] private float delayBetweenSlides = 0.2f;
    [SerializeField] private float finalFadeDuration = 1f;

    public bool IsFinished { get; private set; }
    public bool IsPlaying { get; private set; }

    // Chỉ yêu cầu chuyển slide hiện tại, không kết thúc toàn bộ slideshow.
    private bool nextSlideRequested;

    private void Awake()
    {
        IsFinished = false;
        IsPlaying = false;
        nextSlideRequested = false;

        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = 1f;
            introCanvasGroup.gameObject.SetActive(true);
        }

        ConfigureNextSlideText();
        SetSkipTextVisible(false);
        SetSlideAlpha(0f);
    }

    private void Update()
    {
        if (!IsPlaying || IsFinished)
            return;

        if (Input.GetKeyDown(nextSlideKey))
        {
            nextSlideRequested = true;

            // Dừng typewriter để coroutine của slide có thể chuyển tiếp ngay.
            if (storyTypewriter != null)
                storyTypewriter.CancelTyping(false);
        }
    }

    public IEnumerator PlaySlideshow()
    {
        IsFinished = false;
        IsPlaying = true;
        nextSlideRequested = false;

        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = 1f;
            introCanvasGroup.gameObject.SetActive(true);
        }

        ConfigureNextSlideText();
        SetSkipTextVisible(true);

        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("Chưa có slide nào trong DeadRoofIntroSlideshow.");
            FinishImmediately();
            yield break;
        }

        foreach (StorySlide slide in slides)
        {
            nextSlideRequested = false;

            if (slide == null || slide.image == null)
            {
                Debug.LogWarning("Có một slide chưa được gán ảnh.");
                continue;
            }

            storyImage.sprite = slide.image;

            if (storyTypewriter != null)
                storyTypewriter.ClearImmediately();
            else if (storyText != null)
                storyText.text = string.Empty;

            yield return FadeSlide(0f, 1f);

            if (nextSlideRequested)
            {
                SetSlideAlpha(0f);
                continue;
            }

            if (storyTypewriter != null)
            {
                yield return StartCoroutine(
                    storyTypewriter.TypeText(slide.storyText)
                );
            }
            else if (storyText != null)
            {
                storyText.text = slide.storyText;
            }

            if (nextSlideRequested)
            {
                SetSlideAlpha(0f);
                continue;
            }

            float timer = 0f;
            while (timer < slide.displayDuration && !nextSlideRequested)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (nextSlideRequested)
            {
                SetSlideAlpha(0f);
                continue;
            }

            yield return FadeSlide(1f, 0f);

            if (nextSlideRequested)
            {
                SetSlideAlpha(0f);
                continue;
            }

            float delayTimer = 0f;
            while (delayTimer < delayBetweenSlides && !nextSlideRequested)
            {
                delayTimer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        IsPlaying = false;
        SetSlideAlpha(0f);
        SetSkipTextVisible(false);

        yield return FadeCanvasToTransparent();

        if (introCanvasGroup != null)
            introCanvasGroup.gameObject.SetActive(false);

        IsFinished = true;
    }

    private IEnumerator FadeSlide(float from, float to)
    {
        if (imageFadeDuration <= 0f)
        {
            SetSlideAlpha(to);
            yield break;
        }

        float timer = 0f;
        while (timer < imageFadeDuration)
        {
            if (nextSlideRequested)
            {
                SetSlideAlpha(0f);
                yield break;
            }

            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / imageFadeDuration);
            SetSlideAlpha(Mathf.Lerp(from, to, progress));
            yield return null;
        }

        SetSlideAlpha(to);
    }

    private IEnumerator FadeCanvasToTransparent()
    {
        if (introCanvasGroup == null)
            yield break;

        if (finalFadeDuration <= 0f)
        {
            introCanvasGroup.alpha = 0f;
            yield break;
        }

        float startAlpha = introCanvasGroup.alpha;
        float timer = 0f;

        while (timer < finalFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / finalFadeDuration);
            introCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            yield return null;
        }

        introCanvasGroup.alpha = 0f;
    }

    private void ConfigureNextSlideText()
    {
        if (skipText == null)
            return;

        TextMeshProUGUI label = skipText.GetComponent<TextMeshProUGUI>();
        if (label == null)
            label = skipText.GetComponentInChildren<TextMeshProUGUI>(true);

        if (label != null)
            label.text = nextSlideMessage;
    }

    private void SetSkipTextVisible(bool visible)
    {
        if (skipText != null)
            skipText.SetActive(visible);
    }

    private void SetSlideAlpha(float alpha)
    {
        if (storyImage != null)
        {
            Color imageColor = storyImage.color;
            imageColor.a = alpha;
            storyImage.color = imageColor;
        }

        if (storyText != null)
            storyText.alpha = alpha;
    }

    private void FinishImmediately()
    {
        IsPlaying = false;
        SetSlideAlpha(0f);
        SetSkipTextVisible(false);

        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = 0f;
            introCanvasGroup.gameObject.SetActive(false);
        }

        IsFinished = true;
    }
}