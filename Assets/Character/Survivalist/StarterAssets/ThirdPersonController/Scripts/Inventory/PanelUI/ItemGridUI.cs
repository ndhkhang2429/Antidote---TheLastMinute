using System.Collections.Generic;
using UnityEngine;

public class ItemGridUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject itemSlotPrefab; // prefab 1 ô item có gắn ItemSlotUI
    public Transform gridParent;     // parent có Grid Layout Group

    [Header("Empty slots to always show")]
    public int minVisibleSlots = 16;     // số ô tối thiểu hiển thị dù trống

    readonly List<ItemSlotUI> _slotUIs = new();

    public void Refresh()
    {
        if (InventorySystem.Instance == null) return;

        var slots = InventorySystem.Instance.GetItemSlots();
        int needed = Mathf.Max(slots.Count, minVisibleSlots);

        // Tạo thêm ô UI nếu thiếu
        while (_slotUIs.Count < needed)
        {
            var go = Instantiate(itemSlotPrefab, gridParent);
            var sui = go.GetComponent<ItemSlotUI>();
            if (sui != null) _slotUIs.Add(sui);
        }

        // Ẩn bớt nếu grid thu nhỏ (hiếm xảy ra)
        for (int i = 0; i < _slotUIs.Count; i++)
            _slotUIs[i].gameObject.SetActive(i < needed);

        // Bind dữ liệu
        for (int i = 0; i < _slotUIs.Count; i++)
        {
            if (i < slots.Count)
                _slotUIs[i].Bind(slots[i]);
            else
                _slotUIs[i].Bind(null); // ô trống
        }
    }
}