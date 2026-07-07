using UnityEngine;

/// <summary>
/// Đặt component này trên 1 BoxCollider (isTrigger = true) phủ toàn bộ diện tích 1 phòng,
/// hoặc đặt ngay khung cửa nếu phòng nhỏ. Mỗi phòng trong game (Lobby, Corridor_A,
/// SupplyRoom, ElectricalRoom, StairwellToFloor2...) đều có 1 RoomZone với roomID riêng.
/// FloorMapController dùng roomID này để tô icon "đã khám phá" trên bản đồ.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomZone : MonoBehaviour
{
    [Tooltip("Phải khớp với targetID trong QuestStepSO (nếu bước quest yêu cầu vào phòng này) " +
             "và mapRoomID để hiển thị đúng icon trên Floor Map")]
    public string roomID;

    [Tooltip("Tên hiển thị trên bản đồ, VD: 'Phòng Vật tư Y tế'")]
    public string displayName;

    private bool hasBeenDiscovered = false;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Báo cho hệ thống bản đồ (luôn chạy, dù quest hiện tại không cần phòng này)
        if (!hasBeenDiscovered)
        {
            hasBeenDiscovered = true;
            FloorMapController.Instance?.MarkRoomDiscovered(roomID);
        }

        // Báo cho quest, QuestManager tự lọc xem có đang cần EnterRoom này không
        QuestManager.Instance?.ReportEvent(QuestCompletionType.EnterRoom, roomID);
    }
}
