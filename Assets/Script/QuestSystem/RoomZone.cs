using UnityEngine;

/// <summary>
/// Đặt component này trên 1 BoxCollider (isTrigger = true) phủ toàn bộ diện tích 1 phòng,
/// hoặc đặt ngay khung cửa nếu phòng nhỏ. Dùng để QuestManager biết ngầm khi player
/// đã vào 1 phòng cụ thể (VD: trigger mở đèn, spawn zombie, tiến quest ngầm...).
/// Việc DẪN ĐƯỜNG cho player giờ dựa vào bảng chỉ dẫn vật lý (Navigation Sign) đặt
/// trong level, không còn phụ thuộc vào script này nữa.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomZone : MonoBehaviour
{
    [Tooltip("Phải khớp với targetID trong QuestStepSO nếu bước quest yêu cầu vào phòng này")]
    public string roomID;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        QuestManager.Instance?.ReportEvent(QuestCompletionType.EnterRoom, roomID);
    }
}