using UnityEngine;
using UnityEngine.Events;

public class WorldItem : MonoBehaviour
{
    [Header("Item Info")]
    public ItemDataSO itemData;
    public int quantity = 1;

    [Header("Pickup Event")]
    [Tooltip("Bắn ra khi item này thực sự được nhặt thành công. Gọi TriggerPickedUp() từ script pickup/interaction hiện có, TRƯỚC khi Destroy object này.")]
    public UnityEvent onPickedUp;

    private bool _hasBeenPickedUp = false;

    /// <summary>
    /// Gọi hàm này từ script tương tác/pickup hiện có (nơi đang làm Inventory.AddItem() + Destroy(gameObject)),
    /// ngay TRƯỚC khi Destroy object. Ví dụ:
    /// 
    ///   InventorySystem.AddItem(worldItem.itemData, worldItem.quantity);
    ///   worldItem.TriggerPickedUp();   // <-- thêm dòng này
    ///   Destroy(worldItem.gameObject);
    /// </summary>
    public void TriggerPickedUp()
    {
        if (_hasBeenPickedUp) return;
        _hasBeenPickedUp = true;

        onPickedUp?.Invoke();
    }
}