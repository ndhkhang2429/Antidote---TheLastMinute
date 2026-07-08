using UnityEngine;

// Loại điều kiện hoàn thành 1 bước nhiệm vụ
public enum QuestCompletionType
{
    EnterRoom,          // Player bước vào 1 RoomZone cụ thể
    PickupItem,         // Nhặt 1 item cụ thể (dùng itemName làm ID)
    InteractObject,     // Tương tác 1 object cụ thể (đòn bẩy, cửa, máy phát điện...)
    ReadDocument,       // Đọc 1 DocumentDataSO cụ thể (documentID)
    CustomEvent         // Bắn tay bằng code (VD: giết boss, hoàn thành cutscene)
}

/// <summary>
/// Đại diện 1 bước tiến trình NGẦM của game (không hiện HUD báo cho player).
/// stepTitle/stepDescription chỉ dùng để BẠN dễ đọc trong Inspector khi thiết kế,
/// không hiển thị ra UI. Việc "gợi ý" player đến từ Document/môi trường, không phải từ đây.
/// </summary>
[CreateAssetMenu(fileName = "QuestStep_", menuName = "DeadRoof/Quest/Quest Step")]
public class QuestStepSO : ScriptableObject
{
    [Header("Chỉ để BẠN dễ quản lý trong Inspector (không hiện cho player)")]
    public string stepTitle;
    [TextArea(2, 4)]
    public string stepDescription;

    [Header("Điều kiện hoàn thành")]
    public QuestCompletionType completionType;
    public string targetID;                  // roomID / itemName / objectID / documentID tương ứng completionType

    [Header("Phần thưởng / mở khóa (tùy chọn)")]
    public GameEventSO onStepCompletedEvent; // Bắn event khi bước này hoàn thành (mở cửa, spawn zombie, v.v.)
}