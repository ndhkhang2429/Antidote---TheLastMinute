using UnityEngine;

public class PlayerEquipmentManager : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Kéo thả object Xương Bàn Tay của nhân vật vào đây")]
    public Transform handSocket;

    // Biến lưu trữ object 3D đang hiển thị trên tay
    private GameObject currentEquippedModel;

    private void Start()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHeldItemChanged += HandleItemChange;
    }

    private void HandleItemChange(ItemDataSO heldItem)
    {
        // 1. Xóa object cũ đi (nếu có) khi đổi đồ hoặc cất đồ
        if (currentEquippedModel != null)
        {
            // Gọi hàm OnUnequip nếu object đó có interface
            var oldEquippable = currentEquippedModel.GetComponent<IEquippable>();
            oldEquippable?.OnUnequip();

            Destroy(currentEquippedModel);
        }

        // 2. Nếu đang không cầm gì, hoặc đồ không có model trên tay -> Dừng lại
        if (heldItem == null || heldItem.equipPrefab == null)
            return;

        // 3. Sinh ra (Instantiate) prefab mới và gắn nó làm con của Xương Bàn Tay
        currentEquippedModel = Instantiate(heldItem.equipPrefab, handSocket);
        currentEquippedModel.transform.localPosition = Vector3.zero;
        currentEquippedModel.transform.localRotation = Quaternion.identity;

        // 4. Tìm kiếm interface IEquippable và kích hoạt món đồ
        var newEquippable = currentEquippedModel.GetComponent<IEquippable>();
        newEquippable?.OnEquip();
    }
}