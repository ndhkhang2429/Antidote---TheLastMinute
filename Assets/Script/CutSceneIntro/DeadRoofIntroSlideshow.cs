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

    [Header("Timing")]
    [SerializeField] private float imageFadeDuration = 0.8f;
    [SerializeField] private float delayBetweenSlides = 0.2f;
    [SerializeField] private float finalFadeDuration = 1f;

    public bool IsFinished { get; private set; }

    private bool skipRequested;

    private void Awake()
    {
        IsFinished = false;
        skipRequested = false;

        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = 1f;
            introCanvasGroup.gameObject.SetActive(true);
        }

        if (skipText != null)
        {
            skipText.SetActive(true);
        }

        SetSlideAlpha(0f);
    }

    private void Update()
    {
        if (!IsFinished &&
            Input.GetKeyDown(KeyCode.Space))
        {
            skipRequested = true;

            if (storyTypewriter != null)
            {
                storyTypewriter.CancelTyping(false);
            }
        }
    }

    public IEnumerator PlaySlideshow()
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("Chưa có slide nào trong DeadRoofIntroSlideshow.");
            FinishImmediately();
            yield break;
        }

        foreach (StorySlide slide in slides)
        {
            if (skipRequested)
            {
                break;
            }

            if (slide.image == null)
            {
                Debug.LogWarning("Có một slide chưa được gán ảnh.");
                continue;
            }

            storyImage.sprite = slide.image;

            if (storyTypewriter != null)
            {
                storyTypewriter.ClearImmediately();
            }
            else
            {
                storyText.text = string.Empty;
            }

            yield return FadeSlide(0f, 1f);

            if (storyTypewriter != null)
            {
                yield return StartCoroutine(
                    storyTypewriter.TypeText(slide.storyText)
                );
            }
            else
            {
                storyText.text = slide.storyText;
            }

            float timer = 0f;

            while (timer < slide.displayDuration)
            {
                if (skipRequested)
                {
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            if (skipRequested)
            {
                break;
            }

            yield return FadeSlide(1f, 0f);

            if (delayBetweenSlides > 0f)
            {
                yield return new WaitForSeconds(delayBetweenSlides);
            }
        }

        SetSlideAlpha(0f);

        if (skipText != null)
        {
            skipText.SetActive(false);
        }

        yield return FadeCanvasToBlackOrTransparent();

        if (introCanvasGroup != null)
        {
            introCanvasGroup.gameObject.SetActive(false);
        }

        IsFinished = true;
    }

    private IEnumerator FadeSlide(float from, float to)
    {
        float timer = 0f;

        while (timer < imageFadeDuration)
        {
            if (skipRequested)
            {
                SetSlideAlpha(0f);
                yield break;
            }

            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                timer / imageFadeDuration
            );

            float alpha = Mathf.Lerp(from, to, progress);

            SetSlideAlpha(alpha);

            yield return null;
        }

        SetSlideAlpha(to);
    }

    private IEnumerator FadeCanvasToBlackOrTransparent()
    {
        if (introCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = introCanvasGroup.alpha;
        float timer = 0f;

        while (timer < finalFadeDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                timer / finalFadeDuration
            );

            introCanvasGroup.alpha =
                Mathf.Lerp(startAlpha, 0f, progress);

            yield return null;
        }

        introCanvasGroup.alpha = 0f;
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
        {
            storyText.alpha = alpha;
        }
    }

    private void FinishImmediately()
    {
        SetSlideAlpha(0f);

        if (skipText != null)
        {
            skipText.SetActive(false);
        }

        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = 0f;
            introCanvasGroup.gameObject.SetActive(false);
        }

        IsFinished = true;
    }
}