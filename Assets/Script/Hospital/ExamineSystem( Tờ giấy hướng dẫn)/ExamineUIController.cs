using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI dùng chung để đọc ExaminableObject và DocumentDataSO.
/// Hỗ trợ ảnh, chữ hoặc ảnh và chữ cùng lúc.
/// Có thể tạm ẩn các HUD object trong lúc đọc.
/// </summary>
public class ExamineUIController : MonoBehaviour
{
    public static ExamineUIController Instance
    {
        get;
        private set;
    }

    [Header("UI References")]
    [Tooltip("Panel nền khi đang đọc tài liệu.")]
    public GameObject examinePanel;

    [Tooltip("Ảnh hoặc nền giấy của tài liệu.")]
    public Image contentImage;

    [Tooltip("Nội dung chữ hiển thị trên tài liệu.")]
    public TextMeshProUGUI contentText;

    [Tooltip("Hướng dẫn đóng tài liệu.")]
    public TextMeshProUGUI exitHintText;

    [Header("Hide While Examining")]
    [Tooltip(
        "Những object sẽ tạm bị ẩn khi đọc tài liệu. " +
        "Ví dụ: HUD, crosshair, ammo, health, objective panel."
    )]
    [SerializeField]
    private List<GameObject> objectsToHideWhileExamining =
        new List<GameObject>();

    public bool IsExamining
    {
        get;
        private set;
    }

    private ExaminableObject currentExaminableObject;

    /*
     * Ghi nhớ trạng thái Active trước khi ẩn.
     * Nếu object vốn đã tắt thì khi đóng tài liệu,
     * nó vẫn tiếp tục tắt.
     */
    private readonly Dictionary<GameObject, bool>
        previousObjectStates =
            new Dictionary<GameObject, bool>();

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (examinePanel != null)
        {
            examinePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Mở tài liệu đặt trong thế giới.
    /// </summary>
    public void OpenExamine(
        ExaminableObject obj)
    {
        if (obj == null || IsExamining)
        {
            return;
        }

        currentExaminableObject = obj;

        ShowContent(
            obj.objectName,
            obj.contentText,
            obj.contentSprite,
            obj.openSound
        );
    }

    /// <summary>
    /// Mở tài liệu trong inventory.
    /// </summary>
    public void OpenExamine(
        DocumentDataSO doc)
    {
        if (doc == null || IsExamining)
        {
            return;
        }

        currentExaminableObject = null;

        ShowContent(
            doc.itemName,
            doc.contentText,
            doc.contentSprite,
            doc.openSound
        );

        DocumentReadTracker.Instance
            ?.MarkRead(doc);
    }

    private void ShowContent(
        string title,
        string text,
        Sprite sprite,
        AudioClip sound)
    {
        IsExamining = true;

        HideConfiguredObjects();

        if (examinePanel != null)
        {
            examinePanel.SetActive(true);
        }

        bool hasSprite =
            sprite != null;

        bool hasText =
            !string.IsNullOrWhiteSpace(text);

        if (contentImage != null)
        {
            contentImage.gameObject.SetActive(
                hasSprite
            );

            contentImage.sprite =
                hasSprite
                    ? sprite
                    : null;
        }

        if (contentText != null)
        {
            contentText.gameObject.SetActive(
                hasText
            );

            contentText.text =
                hasText
                    ? text
                    : string.Empty;

            if (hasSprite && hasText)
            {
                contentText.transform
                    .SetAsLastSibling();
            }
        }

        if (exitHintText != null)
        {
            exitHintText.text =
                "[F] or [ESC] to close";
        }

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        if (sound != null)
        {
            Camera mainCamera =
                Camera.main;

            Vector3 soundPosition =
                mainCamera != null
                    ? mainCamera.transform.position
                    : transform.position;

            AudioSource.PlayClipAtPoint(
                sound,
                soundPosition
            );
        }

        Debug.Log(
            $"[ExamineUI] Đang xem: {title}"
        );
    }

    public void CloseExamine()
    {
        if (!IsExamining)
        {
            return;
        }

        IsExamining = false;

        if (examinePanel != null)
        {
            examinePanel.SetActive(false);
        }

        RestoreConfiguredObjects();

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        ExaminableObject closedObject =
            currentExaminableObject;

        currentExaminableObject = null;

        /*
         * Tài liệu được xem là đã đọc sau khi
         * người chơi đóng giao diện.
         */
        closedObject?.NotifyExamineClosed();

        Debug.Log(
            "[ExamineUI] Đóng examine."
        );
    }

    /// <summary>
    /// Ghi nhớ trạng thái rồi tạm ẩn HUD.
    /// </summary>
    private void HideConfiguredObjects()
    {
        previousObjectStates.Clear();

        foreach (
            GameObject target
            in objectsToHideWhileExamining)
        {
            if (target == null ||
                target == examinePanel)
            {
                continue;
            }

            previousObjectStates[target] =
                target.activeSelf;

            target.SetActive(false);
        }
    }

    /// <summary>
    /// Khôi phục đúng trạng thái trước khi mở tài liệu.
    /// </summary>
    private void RestoreConfiguredObjects()
    {
        foreach (
            KeyValuePair<GameObject, bool> pair
            in previousObjectStates)
        {
            if (pair.Key != null)
            {
                pair.Key.SetActive(
                    pair.Value
                );
            }
        }

        previousObjectStates.Clear();
    }

    private void OnDisable()
    {
        /*
         * Tránh HUD bị tắt vĩnh viễn nếu controller
         * bị disable trong lúc đang đọc.
         */
        if (previousObjectStates.Count > 0)
        {
            RestoreConfiguredObjects();
        }
    }
}