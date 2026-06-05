using System.Collections.Generic;
using UnityEngine;

public class ItemGridUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject itemSlotPrefab;
    public Transform gridParent;
    public int totalSlots = 36;

    readonly List<ItemSlotUI> _slotUIs = new();

    // Dùng Awake thay vì Start để subscribe sớm hơn
    void Awake()
    {
        CreateAllSlots();
    }

    void OnEnable()
    {
        // Subscribe mỗi khi panel được bật
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged += Refresh;

        Refresh();
    }

    void OnDisable()
    {
        // Unsubscribe khi panel tắt
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
    }

    void CreateAllSlots()
    {
        foreach (var s in _slotUIs)
            if (s != null) Destroy(s.gameObject);
        _slotUIs.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            var go = Instantiate(itemSlotPrefab, gridParent);
            var sui = go.GetComponent<ItemSlotUI>();
            _slotUIs.Add(sui);
        }
    }

    public void Refresh()
    {
        if (InventorySystem.Instance == null) return;

        var slots = InventorySystem.Instance.GetItemSlots();

        for (int i = 0; i < _slotUIs.Count; i++)
        {
            if (i < slots.Count)
                _slotUIs[i].Bind(slots[i]);
            else
                _slotUIs[i].Bind(null);
        }
    }
}