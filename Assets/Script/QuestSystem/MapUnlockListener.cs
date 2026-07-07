using UnityEngine;

/// <summary>
/// Đặt object này 1 lần trong scene (ngang hàng QuestManager/FloorMapController).
/// Kéo vào field mapItem đúng asset ItemDataSO (hoặc DocumentDataSO) đại diện cho
/// "Sơ đồ tầng trệt" mà player sẽ nhặt được đâu đó trong map (VD: bàn lễ tân, phòng bảo vệ).
/// Không cần sửa InventorySystem — script này tự kiểm tra qua OnInventoryChanged.
/// </summary>
public class MapUnlockListener : MonoBehaviour
{
    [SerializeField] private ItemDataSO mapItem;

    private bool alreadyUnlocked = false;

    private void OnEnable()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged += CheckMapItem;
    }

    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= CheckMapItem;
    }

    private void CheckMapItem()
    {
        if (alreadyUnlocked || mapItem == null) return;
        if (InventorySystem.Instance == null || FloorMapController.Instance == null) return;

        if (InventorySystem.Instance.HasItem(mapItem, 1))
        {
            alreadyUnlocked = true;
            FloorMapController.Instance.UnlockMap();
        }
    }
}
