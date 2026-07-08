using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton UI controller cho ExamineSystem.
/// Gắn vào Canvas có sẵn trong scene.
/// Hiển thị nội dung tờ giấy/vật phẩm khi player đọc — dùng CHUNG cho 2 nguồn:
///   1) ExaminableObject  → vật thể đọc tại chỗ, không nhặt (giữ nguyên hành vi cũ)
///   2) DocumentDataSO    → item trong inventory, đọc lại được bất cứ lúc nào (MỚI)
/// </summary>
public class ExamineUIController : MonoBehaviour
{
    public static ExamineUIController Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Panel nền mờ phủ màn hình khi đang đọc")]
    public GameObject examinePanel;
    [Tooltip("Image hiển thị sprite tờ giấy/hình ảnh")]
    public Image contentImage;
    [Tooltip("Text hiển thị nội dung chữ")]
    public TextMeshProUGUI contentText;
    [Tooltip("Text nhắc nhở thoát")]
    public TextMeshProUGUI exitHintText;
    [Tooltip("Text tên vật phẩm")]
    public TextMeshProUGUI titleText;

    // ── State ──────────────────────────────────────────────
    public bool IsExamining { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (examinePanel != null) examinePanel.SetActive(false);
    }

    /// <summary>
    /// Mở UI xem vật phẩm dạng ExaminableObject (đọc tại chỗ, không nhặt).
    /// Giữ nguyên hành vi cũ, chỉ đổi sang gọi hàm dùng chung bên dưới.
    /// </summary>
    public void OpenExamine(ExaminableObject obj)
    {
        if (obj == null) return;
        ShowContent(obj.objectName, obj.contentText, obj.contentSprite, obj.openSound);
    }

    /// <summary>
    /// MỚI — Mở UI xem 1 DocumentDataSO (item trong inventory).
    /// Gọi từ ItemGridUI khi player double-click / dùng 1 slot có category = Document.
    /// Đồng thời báo cho DocumentReadTracker để hệ thống quest ngầm biết đã đọc.
    /// </summary>
    public void OpenExamine(DocumentDataSO doc)
    {
        if (doc == null) return;
        ShowContent(doc.itemName, doc.contentText, doc.contentSprite, doc.openSound);

        // Báo ngầm cho quest system — KHÔNG hiện thông báo nào cho player
        DocumentReadTracker.Instance?.MarkRead(doc);
    }

    /// <summary>
    /// Logic hiển thị dùng chung cho cả 2 nguồn ở trên — tránh lặp code.
    /// </summary>
    private void ShowContent(string title, string text, Sprite sprite, AudioClip sound)
    {
        IsExamining = true;
        examinePanel.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (sprite != null)
        {
            contentImage.gameObject.SetActive(true);
            contentImage.sprite = sprite;
            if (contentText != null)
                contentText.gameObject.SetActive(false);
        }
        else if (!string.IsNullOrEmpty(text))
        {
            if (contentText != null)
            {
                contentText.gameObject.SetActive(true);
                contentText.text = text;
            }
            contentImage.gameObject.SetActive(false);
        }

        if (exitHintText != null)
            exitHintText.text = "[F] hoặc [ESC] để đóng";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (sound != null)
            AudioSource.PlayClipAtPoint(sound, Camera.main.transform.position);

        Debug.Log($"[ExamineUI] Đang xem: {title}");
    }

    /// <summary>
    /// Đóng UI. Gọi khi nhấn F hoặc ESC (logic bấm phím vẫn nằm ở PlayerInteraction như cũ).
    /// </summary>
    public void CloseExamine()
    {
        IsExamining = false;
        examinePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[ExamineUI] Đóng examine.");
    }
}