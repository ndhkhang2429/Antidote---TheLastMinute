using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel toàn màn hình hiện nội dung 1 DocumentDataSO (giống "File" trong Resident Evil).
/// Mở bằng cách gọi DocumentReaderUI.Instance.Open(doc) từ ItemGridUI khi player
/// chọn/dùng 1 slot có item.category == ItemCategory.Document (xem hướng dẫn nối vào ItemGridUI bên dưới).
/// Đóng bằng phím E hoặc ESC.
/// </summary>
public class DocumentReaderUI : MonoBehaviour
{
    public static DocumentReaderUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text titleText;        // đổi TMP_Text nếu dùng TextMeshPro
    [SerializeField] private Text bodyText;
    [SerializeField] private Image contentImage;     // dùng khi document có contentSprite
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            Close();
    }

    public void Open(DocumentDataSO doc)
    {
        if (doc == null || panelRoot == null) return;

        titleText.text = doc.itemName;
        bodyText.text = doc.contentText;

        if (contentImage != null)
        {
            bool hasSprite = doc.contentSprite != null;
            contentImage.gameObject.SetActive(hasSprite);
            if (hasSprite) contentImage.sprite = doc.contentSprite;
        }

        if (doc.openSound != null && audioSource != null)
            audioSource.PlayOneShot(doc.openSound);

        panelRoot.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Đánh dấu đã đọc — chỉ có tác dụng ngầm (event/quest), KHÔNG hiện thông báo nào cho player
        DocumentReadTracker.Instance?.MarkRead(doc);
    }

    public void Close()
    {
        panelRoot.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
