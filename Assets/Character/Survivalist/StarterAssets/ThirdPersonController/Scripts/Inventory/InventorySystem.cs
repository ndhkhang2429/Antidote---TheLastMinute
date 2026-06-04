using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Equipment Slots")]
    public ItemDataSO equippedHelmet;
    public ItemDataSO equippedVest;
    public BackpackDataSO equippedBackpack;

    [Header("Weapon Slots (index 0-3 = ô 1-4)")]
    public InventorySlot[] weaponSlots = new InventorySlot[4];

    [Header("Item Grid (balo)")]
    [SerializeField] private List<InventorySlot> itemSlots = new();
    [SerializeField] private int maxItemSlots = 32;

    [Header("Hotbar")]
    public InventorySlot[] hotbarSlots = new InventorySlot[3]; // Q, E, R

    // ── Events ──────────────────────────────────────────────
    public event Action OnInventoryChanged;
    public event Action<ItemDataSO> OnWeaponEquipped;
    public event Action<int> OnHotbarUsed; // index hotbar

    // Thêm method public này để các class khác trigger event
    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    // ── Capacity ─────────────────────────────────────────────
    public int MaxCapacity => equippedBackpack != null ? equippedBackpack.capacity : 0;

    public int UsedCapacity
    {
        get
        {
            int used = 0;
            foreach (var slot in itemSlots)
                if (!slot.IsEmpty)
                    used += slot.item.weightPerUnit * slot.quantity;
            // Lựu đạn trong weapon slot 4 cũng tính
            var grenadeSlot = weaponSlots[3];
            if (!grenadeSlot.IsEmpty)
                used += grenadeSlot.item.weightPerUnit * grenadeSlot.quantity;
            return used;
        }
    }

    // ────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < weaponSlots.Length; i++) weaponSlots[i] = new InventorySlot();
        for (int i = 0; i < hotbarSlots.Length; i++) hotbarSlots[i] = new InventorySlot();
    }

    // ── Nhặt item ────────────────────────────────────────────
    /// <returns>true nếu nhặt được ít nhất 1</returns>
    public bool PickupItem(ItemDataSO item, int amount = 1)
    {
        switch (item.category)
        {
            case ItemCategory.Equipment:
                return TryEquipEquipment(item);

            case ItemCategory.Weapon:
                return TryEquipWeapon(item as WeaponDataSO);

            case ItemCategory.Grenade:
                // Lựu đạn vào item grid trước, người chơi tự kéo vào ô 4
                return TryAddToGrid(item, amount);

            default: // Consumable, Ammo, QuestItem
                return TryAddToGrid(item, amount);
        }
    }

    // ── Equipment ────────────────────────────────────────────
    bool TryEquipEquipment(ItemDataSO item)
    {
        if (item is BackpackDataSO bp) { equippedBackpack = bp; OnInventoryChanged?.Invoke(); return true; }
        // phân loại nón/giáp qua itemName hoặc thêm enum con
        // ở đây dùng category phụ đơn giản:
        if (item.itemName.ToLower().Contains("nón") || item.itemName.ToLower().Contains("helmet"))
        { equippedHelmet = item; OnInventoryChanged?.Invoke(); return true; }
        equippedVest = item;
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ── Weapon ───────────────────────────────────────────────
    public bool TryEquipWeapon(WeaponDataSO weapon)
    {
        if (weapon == null) return false;
        int slotIndex = WeaponSlotIndex(weapon.weaponSlotType);
        if (slotIndex < 0) return false;

        weaponSlots[slotIndex].Set(weapon, 1);
        OnWeaponEquipped?.Invoke(weapon);
        OnInventoryChanged?.Invoke();
        return true;
    }

    int WeaponSlotIndex(WeaponSlotType type) => type switch
    {
        WeaponSlotType.PistolOrShotgun => 0,
        WeaponSlotType.Rifle => 1,
        WeaponSlotType.Melee => 2,
        WeaponSlotType.Grenade => 3,
        _ => -1
    };

    // ── Item Grid ─────────────────────────────────────────────
    bool TryAddToGrid(ItemDataSO item, int amount)
    {
        if (equippedBackpack == null)
        {
            Debug.Log("Không có balo!");
            return false;
        }

        int weightNeeded = item.weightPerUnit * amount;
        if (UsedCapacity + weightNeeded > MaxCapacity)
        {
            // Thêm được 1 phần
            int canFit = (MaxCapacity - UsedCapacity) / item.weightPerUnit;
            if (canFit <= 0) { Debug.Log("Balo đầy!"); return false; }
            amount = canFit;
        }

        int remaining = amount;

        // 1. Stack vào ô đã có cùng loại
        foreach (var slot in itemSlots)
        {
            if (!slot.IsEmpty && slot.item == item && !slot.IsFull)
            {
                remaining = slot.Add(remaining);
                if (remaining == 0) break;
            }
        }

        // 2. Mở ô mới
        while (remaining > 0 && itemSlots.Count < maxItemSlots)
        {
            var newSlot = new InventorySlot();
            newSlot.Set(item, 0);
            remaining = newSlot.Add(remaining);
            itemSlots.Add(newSlot);
        }

        OnInventoryChanged?.Invoke();
        return remaining < amount; // true nếu ít nhất 1 cái được thêm
    }

    // ── Remove ───────────────────────────────────────────────
    public bool RemoveItem(ItemDataSO item, int amount = 1)
    {
        int toRemove = amount;
        foreach (var slot in itemSlots)
        {
            if (slot.IsEmpty || slot.item != item) continue;
            int take = Mathf.Min(slot.quantity, toRemove);
            slot.quantity -= take;
            toRemove -= take;
            if (slot.quantity <= 0) slot.Clear();
            if (toRemove <= 0) break;
        }
        if (toRemove < amount) { OnInventoryChanged?.Invoke(); return true; }
        return false;
    }

    // ── Hotbar ────────────────────────────────────────────────
    /// Gán item vào hotbar (gọi từ UI khi drag)
    public void AssignHotbar(int index, ItemDataSO item, int qty)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;
        hotbarSlots[index].Set(item, qty);
        OnInventoryChanged?.Invoke();
    }

    /// Sử dụng hotbar (nhấn Q/E/R)
    public void UseHotbar(int index)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;
        var slot = hotbarSlots[index];
        if (slot.IsEmpty) return;

        // Gọi logic dùng item – delegate sang ItemUser
        ItemUser.Use(slot.item, gameObject);

        slot.quantity--;
        if (slot.quantity <= 0) slot.Clear();

        OnHotbarUsed?.Invoke(index);
        OnInventoryChanged?.Invoke();
    }

    // ── Lựu đạn vào ô 4 (drag từ grid sang weapon slot) ─────
    public bool MoveGrenadeToWeaponSlot(InventorySlot fromGridSlot)
    {
        if (fromGridSlot.IsEmpty || fromGridSlot.item.category != ItemCategory.Grenade) return false;

        var grenadeSlot = weaponSlots[3];
        if (!grenadeSlot.IsEmpty && grenadeSlot.item != fromGridSlot.item)
        {
            // swap về grid
            itemSlots.Add(new InventorySlot());
            itemSlots[^1].Set(grenadeSlot.item, grenadeSlot.quantity);
        }

        grenadeSlot.Set(fromGridSlot.item, fromGridSlot.quantity);
        fromGridSlot.Clear();
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────
    public List<InventorySlot> GetItemSlots() => itemSlots;

    public bool HasItem(ItemDataSO item, int amount = 1)
    {
        int count = 0;
        foreach (var slot in itemSlots)
            if (!slot.IsEmpty && slot.item == item) count += slot.quantity;
        return count >= amount;
    }

    public int CountItem(ItemDataSO item)
    {
        int count = 0;
        foreach (var slot in itemSlots)
            if (!slot.IsEmpty && slot.item == item) count += slot.quantity;
        return count;
    }
}           