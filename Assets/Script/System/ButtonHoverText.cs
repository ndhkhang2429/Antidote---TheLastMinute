using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class ButtonHoverText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Image highlightBar;
    public TextMeshProUGUI buttonText;

    [Header("Settings")]
    public float targetAlpha = 0.15f;
    public float fadeDuration = 0.2f;
    public Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color hoverColor = Color.white;

    private Coroutine _fadeCoroutine;

    void Start()
    {
        CreateGradientSprite();
        SetBarAlpha(0f);
        if (buttonText) buttonText.color = normalColor;
    }

    private void CreateGradientSprite()
    {
        int width = 256, height = 32;
        Texture2D tex = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
        {
            float t = (float)x / width;
            float alpha = Mathf.Sin(t * Mathf.PI);

            for (int y = 0; y < height; y++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }

        tex.Apply();
        highlightBar.sprite = Sprite.Create(tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeBar(targetAlpha));
        if (buttonText) buttonText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeBar(0f));
        if (buttonText) buttonText.color = normalColor;
    }

    private IEnumerator FadeBar(float targetA)
    {
        float startA = highlightBar.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            t = 1f - (1f - t) * (1f - t); // ease out
            SetBarAlpha(Mathf.Lerp(startA, targetA, t));
            yield return null;
        }

        SetBarAlpha(targetA);
    }

    private void SetBarAlpha(float alpha)
    {
        if (!highlightBar) return;
        Color c = highlightBar.color;
        c.a = alpha;
        highlightBar.color = c;
    }
}