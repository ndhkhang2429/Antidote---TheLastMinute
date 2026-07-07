using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bản đồ tầng dạng ảnh tĩnh (giống bản đồ giấy RE). KHÔNG tự động có sẵn —
/// player phải tìm được item "Sơ đồ tầng" (DocumentDataSO/ItemDataSO đánh dấu là bản đồ)
/// thì mới bấm M mở được (xem MapUnlockListener.cs).
/// Bản đồ chỉ hiện các phòng ĐÃ ĐI QUA (do RoomZone báo lên), KHÔNG tự tô màu phòng mục tiêu —
/// player tự suy luận cần đi đâu dựa trên ghi chú/tài liệu đã đọc, không có gợi ý trực tiếp trên bản đồ.
/// </summary>
public class FloorMapController : MonoBehaviour
{
    public static FloorMapController Instance { get; private set; }

    [SerializeField] private GameObject floorMapPanel;
    [SerializeField] private List<RoomMapIcon> roomIcons;

    private readonly HashSet<string> discoveredRooms = new HashSet<string>();
    private bool mapUnlocked = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (floorMapPanel != null) floorMapPanel.SetActive(false);
    }

    private void Update()
    {
        if (mapUnlocked && Input.GetKeyDown(KeyCode.M))
            ToggleMap();
    }

    // Gọi hàm này khi player nhặt được item bản đồ (xem MapUnlockListener.cs)
    public void UnlockMap()
    {
        mapUnlocked = true;
    }

    public bool IsMapUnlocked() => mapUnlocked;

    public void ToggleMap()
    {
        if (floorMapPanel == null || !mapUnlocked) return;
        bool nowOpen = !floorMapPanel.activeSelf;
        floorMapPanel.SetActive(nowOpen);

        Time.timeScale = nowOpen ? 0f : 1f;
        Cursor.lockState = nowOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = nowOpen;
    }

    public void MarkRoomDiscovered(string roomID)
    {
        if (discoveredRooms.Contains(roomID)) return;
        discoveredRooms.Add(roomID);

        foreach (var icon in roomIcons)
        {
            if (icon.roomID == roomID)
                icon.SetDiscovered(true);
        }
    }
}
