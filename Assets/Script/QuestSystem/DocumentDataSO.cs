using UnityEngine;

/// <summary>
/// Item dạng "đọc được" — ghi chú, thư, hồ sơ bệnh nhân, mảnh bản đồ viết tay...
/// Kế thừa ItemDataSO nên vẫn nhặt/lưu trong InventorySystem như mọi item khác
/// (category = Document, nên rơi vào nhánh TryAddToGrid mặc định, không cần sửa InventorySystem).
/// Khác Consumable ở chỗ: đọc xong KHÔNG bị tiêu hao, player giữ để xem lại bất cứ lúc nào (giống File trong RE).
/// </summary>
[CreateAssetMenu(fileName = "Document_", menuName = "Inventory/Document Item")]
public class DocumentDataSO : ItemDataSO
{
    [Header("Nội dung hiển thị khi đọc")]
    [TextArea(4, 12)]
    public string contentText;

    [Tooltip("Dùng nếu tài liệu có hình vẽ tay (VD: mảnh bản đồ) thay vì chỉ chữ")]
    public Sprite contentSprite;

    public AudioClip openSound;

    [Header("Liên kết Quest (tùy chọn, âm thầm — không hiện HUD)")]
    [Tooltip("Nếu QuestStepSO nào đó có completionType = ReadDocument, targetID phải trùng chuỗi này")]
    public string documentID;

    [Tooltip("Bắn thêm 1 GameEventSO khi đọc lần đầu, VD: unlock cửa, spawn zombie, bật đèn khu vực")]
    public GameEventSO onFirstReadEvent;
}