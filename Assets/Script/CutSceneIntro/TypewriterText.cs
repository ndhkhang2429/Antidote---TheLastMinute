using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterText : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI targetText;

    [Header("Typing")]
    [Tooltip("Thời gian giữa mỗi ký tự.")]
    [SerializeField, Min(0.005f)]
    private float characterInterval = 0.03f;

    [Tooltip("Phát âm thanh sau mỗi bao nhiêu ký tự hợp lệ.")]
    [SerializeField, Min(1)]
    private int clickEveryCharacters = 2;

    [Header("Typing Sound")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingClick;

    [SerializeField, Range(0f, 1f)]
    private float clickVolume = 0.22f;

    [SerializeField] private float minimumPitch = 0.96f;
    [SerializeField] private float maximumPitch = 1.04f;

    [Header("Sound Filtering")]
    [Tooltip("Không phát click với dấu cách và ký tự xuống dòng.")]
    [SerializeField] private bool ignoreWhitespace = true;

    [Tooltip("Không phát click với dấu chấm, dấu phẩy và dấu câu.")]
    [SerializeField] private bool ignorePunctuation = true;

    public bool IsTyping { get; private set; }

    private int typingVersion;

    private void Awake()
    {
        ClearImmediately();
    }

    /// <summary>
    /// Hiện nội dung theo hiệu ứng đánh máy.
    /// </summary>
    public IEnumerator TypeText(string content)
    {
        if (targetText == null)
        {
            Debug.LogError(
                "TypewriterText chưa được gán Target Text.",
                this
            );

            yield break;
        }

        typingVersion++;
        int currentVersion = typingVersion;

        IsTyping = true;

        targetText.text = content;
        targetText.maxVisibleCharacters = 0;
        targetText.ForceMeshUpdate();

        int totalCharacters =
            targetText.textInfo.characterCount;

        int validCharacterCounter = 0;

        for (int i = 0; i < totalCharacters; i++)
        {
            // Coroutine đã bị hủy bởi slide mới hoặc Skip.
            if (currentVersion != typingVersion)
            {
                IsTyping = false;
                yield break;
            }

            targetText.maxVisibleCharacters = i + 1;

            TMP_CharacterInfo characterInfo =
                targetText.textInfo.characterInfo[i];

            char currentCharacter =
                characterInfo.character;

            if (ShouldPlayClick(currentCharacter))
            {
                validCharacterCounter++;

                if (validCharacterCounter %
                    clickEveryCharacters == 0)
                {
                    PlayTypingClick();
                }
            }

            yield return new WaitForSecondsRealtime(
                characterInterval
            );
        }

        targetText.maxVisibleCharacters =
            totalCharacters;

        IsTyping = false;
    }

    /// <summary>
    /// Dừng hiệu ứng đang chạy.
    /// </summary>
    public void CancelTyping(bool showCompleteText)
    {
        typingVersion++;
        IsTyping = false;

        if (targetText == null)
        {
            return;
        }

        if (showCompleteText)
        {
            targetText.maxVisibleCharacters =
                int.MaxValue;
        }
        else
        {
            ClearImmediately();
        }
    }

    /// <summary>
    /// Hiện toàn bộ nội dung ngay lập tức.
    /// </summary>
    public void ShowImmediately(string content)
    {
        typingVersion++;
        IsTyping = false;

        if (targetText == null)
        {
            return;
        }

        targetText.text = content;
        targetText.maxVisibleCharacters =
            int.MaxValue;
    }

    public void ClearImmediately()
    {
        typingVersion++;
        IsTyping = false;

        if (targetText == null)
        {
            return;
        }

        targetText.text = string.Empty;
        targetText.maxVisibleCharacters = 0;
    }

    private bool ShouldPlayClick(char character)
    {
        if (ignoreWhitespace &&
            char.IsWhiteSpace(character))
        {
            return false;
        }

        if (ignorePunctuation &&
            char.IsPunctuation(character))
        {
            return false;
        }

        return true;
    }

    private void PlayTypingClick()
    {
        if (typingAudioSource == null ||
            typingClick == null)
        {
            return;
        }

        typingAudioSource.pitch = Random.Range(
            minimumPitch,
            maximumPitch
        );

        typingAudioSource.PlayOneShot(
            typingClick,
            clickVolume
        );
    }

    private void OnDisable()
    {
        CancelTyping(false);
    }
}