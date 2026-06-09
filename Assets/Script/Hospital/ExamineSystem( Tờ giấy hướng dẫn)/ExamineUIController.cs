using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton UI controller cho ExamineSystem.
/// Gắn vào Canvas có sẵn trong scene.
/// Hiển thị nội dung tờ giấy/vật phẩm khi player đọc.
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

        // Đảm bảo panel ẩn lúc đầu
        if (examinePanel != null) examinePanel.SetActive(false);
    }

    /// <summary>
    /// Mở UI xem vật phẩm. Gọi từ PlayerInteraction.
    /// </summary>
    public void OpenExamine(ExaminableObject obj)
    {
        if (obj == null) return;

        IsExamining = true;
        examinePanel.SetActive(true);

        // Hiện title
        if (titleText != null)
            titleText.text = obj.objectName;

        // Hiện hình hoặc text
        if (obj.contentSprite != null)
        {
            contentImage.gameObject.SetActive(true);
            contentImage.sprite = obj.contentSprite;

            if (contentText != null)
                contentText.gameObject.SetActive(false);
        }
        else if (!string.IsNullOrEmpty(obj.contentText))
        {
            if (contentText != null)
            {
                contentText.gameObject.SetActive(true);
                contentText.text = obj.contentText;
            }
            contentImage.gameObject.SetActive(false);
        }

        // Hint thoát
        if (exitHintText != null)
            exitHintText.text = "[F] hoặc [ESC] để đóng";

        // Mở cursor để player có thể đọc thoải mái
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Phát sound nếu có
        if (obj.openSound != null)
            AudioSource.PlayClipAtPoint(obj.openSound, Camera.main.transform.position);

        Debug.Log($"[ExamineUI] Đang xem: {obj.objectName}");
    }

    /// <summary>
    /// Đóng UI. Gọi khi nhấn F hoặc ESC.
    /// </summary>
    public void CloseExamine()
    {
        IsExamining = false;
        examinePanel.SetActive(false);

        // Trả cursor về trạng thái game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[ExamineUI] Đóng examine.");
    }
}