using UnityEngine;

public class HotbarManager : MonoBehaviour
{
    public InventorySlot[] hotbarSlots = new InventorySlot[5];
    private int activeIndex = 0;

    // Gán slot từ Inventory UI (drag & drop)
    public void AssignToHotbar(int hotbarIdx, InventorySlot sourceSlot)
    {
        hotbarSlots[hotbarIdx] = sourceSlot;

        // SỬA LỖI: Comment tạm dòng này cho đến khi bạn code xong HotbarUI
        Debug.Log($"Đã cập nhật UI Hotbar ô số {hotbarIdx}");
        // HotbarUI.Instance.Refresh(hotbarIdx); 
    }

    public void UseActive()
    {
        var slot = hotbarSlots[activeIndex];
        if (slot?.item == null) return;

        if (slot.item is IUsable usable)
        {
            // SỬA LỖI: IUsable.Use() đòi tham số là GameObject, không phải PlayerController
            usable.Use(this.gameObject);

            slot.amount--;
            if (slot.amount <= 0) hotbarSlots[activeIndex] = null;

            // SỬA LỖI: Comment tạm dòng UI
            Debug.Log($"Đã cập nhật lại UI Hotbar ô số {activeIndex} sau khi dùng item");
            // HotbarUI.Instance.Refresh(activeIndex);
        }
    }

    // Phím tắt Q/E/R/F/G
    void Update()
    {
        KeyCode[] keys = { KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.G };
        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
            {
                activeIndex = i;
                UseActive();
            }
        }
    }
}