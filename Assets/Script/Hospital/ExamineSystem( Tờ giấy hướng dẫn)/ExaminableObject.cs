using UnityEngine;

/// <summary>
/// Gắn vào bất kỳ object nào có thể "đọc/xem":
/// tờ giấy, sách, bảng thông báo, hồ sơ bệnh nhân...
/// Không bị loot - vẫn nằm tại chỗ sau khi đọc xong.
/// </summary>
public class ExaminableObject : MonoBehaviour
{
    [Header("Nội dung hiển thị")]
    [Tooltip("Tên vật phẩm hiện trong prompt")]
    public string objectName = "Tờ hướng dẫn";

    [Tooltip("Nếu dùng Texture/Sprite (tờ giấy có hình)")]
    public Sprite contentSprite;

    [Tooltip("Nếu chỉ dùng text thuần (bảng thông báo, ghi chú ngắn)")]
    [TextArea(3, 10)]
    public string contentText = "";

    [Header("Tuỳ chọn")]
    [Tooltip("Phát âm thanh khi mở xem")]
    public AudioClip openSound;
}