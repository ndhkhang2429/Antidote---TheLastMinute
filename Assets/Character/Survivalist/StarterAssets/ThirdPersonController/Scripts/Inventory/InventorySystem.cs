using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Backpack — mặc định có sẵn, không cần loot")]
    [SerializeField] private int _defaultCapacity = 150; // Balo Cấp 2

    [Header("Weapon Slots (0=Pistol, 1=Rifle, 2=Melee, 3=Grenade)")]
    public InventorySlot[] weaponSlots = new InventorySlot[4];

    [Header("Item Grid")]
    [SerializeField] private List<InventorySlot> itemSlots = new();
    [SerializeField] private int maxItemSlots = 32;

    [Header("Slot 5 — QuestItem đang cầm")]
    public InventorySlot heldItemSlot = new InventorySlot();

    [Header("Active Slot")]
    public int activeSlot = -1;
    public int activeWeaponSlot = -1;

    // ── Capacity ─────────────────────────────────────────
    public int MaxCapacity => _defaultCapacity;
    public int UsedCapacity
    {
        get
        {
            int used = 0;
            foreach (var slot in itemSlots)
                if (!slot.IsEmpty)
                    used += slot.item.weightPerUnit * slot.quantity;
            var grenadeSlot = weaponSlots[3];
            if (!grenadeSlot.IsEmpty)
                used += grenadeSlot.item.weightPerUnit * grenadeSlot.quantity;
            return used;
        }
    }

    // ── Events ───────────────────────────────────────────
    public event Action OnInventoryChanged;
    public event Action<ItemDataSO> OnWeaponEquipped;
    public event Action<int> OnActiveSlotChanged;
    public event Action<ItemDataSO> OnHeldItemChanged;

    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();

    // ── Lifecycle ─────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < weaponSlots.Length; i++)
            weaponSlots[i] = new InventorySlot();
    }

    void Start()
    {
        Debug.Log($"[Inventory] Sẵn sàng | Sức chứa: {MaxCapacity}");
        OnInventoryChanged?.Invoke();
    }

    // ── Pickup ────────────────────────────────────────────
    public bool PickupItem(ItemDataSO item, int amount = 1)
    {
        if (item == null) return false;

        switch (item.category)
        {
            case ItemCategory.Equipment:
                // Bỏ qua hoàn toàn — balo đã có sẵn
                Debug.Log($"[Inventory] {item.itemName} không cần nhặt");
                return false;

            case ItemCategory.Weapon:
                return TryEquipWeapon(item as WeaponDataSO);

            default:
                return TryAddToGrid(item, amount);
        }
    }

    // ── Weapon ────────────────────────────────────────────
    public bool TryEquipWeapon(WeaponDataSO weapon)
    {
        if (weapon == null) return false;
        int slotIndex = WeaponSlotIndex(weapon.weaponSlotType);
        if (slotIndex < 0) return false;

        weaponSlots[slotIndex].Set(weapon, 1);
        OnWeaponEquipped?.Invoke(weapon);
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory] Trang bị: {weapon.itemName} → slot {slotIndex + 1}");
        return true;
    }

    int WeaponSlotIndex(WeaponSlotType type) => type switch
    {
        WeaponSlotType.PistolOrShotgun => 1,
        WeaponSlotType.Rifle => 0,
        WeaponSlotType.Melee => 2,
        WeaponSlotType.Grenade => 3,
        WeaponSlotType.QuestItem => 4,
        _ => -1
    };

    // ── Item Grid ─────────────────────────────────────────
    public bool TryAddToGrid(ItemDataSO item, int amount)
    {
        if (item.weightPerUnit <= 0)
            return AddToSlots(item, amount);

        int freeCapacity = MaxCapacity - UsedCapacity;
        if (freeCapacity <= 0) { Debug.Log("[Inventory] Balo đầy!"); return false; }

        int canFit = freeCapacity / item.weightPerUnit;
        if (canFit <= 0) { Debug.Log("[Inventory] Không đủ chỗ!"); return false; }

        return AddToSlots(item, Mathf.Min(amount, canFit));
    }

    bool AddToSlots(ItemDataSO item, int amount)
    {
        int remaining = amount;

        foreach (var slot in itemSlots)
        {
            if (!slot.IsEmpty && slot.item == item && !slot.IsFull)
            {
                remaining = slot.Add(remaining);
                if (remaining == 0) break;
            }
        }

        while (remaining > 0 && itemSlots.Count < maxItemSlots)
        {
            var newSlot = new InventorySlot();
            newSlot.Set(item, 0);
            remaining = newSlot.Add(remaining);
            itemSlots.Add(newSlot);
        }

        if (remaining < amount)
        {
            if (!heldItemSlot.IsEmpty && heldItemSlot.item == item)
                heldItemSlot.quantity = CountItem(item);
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Thêm: {item.itemName} x{amount - remaining}");
            return true;
        }
        return false;
    }

    // ── Remove ────────────────────────────────────────────
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

        bool success = toRemove < amount;
        if (success)
        {
            if (!heldItemSlot.IsEmpty && heldItemSlot.item == item)
            {
                int remaining = CountItem(item);
                if (remaining <= 0)
                {
                    heldItemSlot.Clear();
                    if (activeSlot == 4)
                    {
                        activeSlot = -1;
                        OnActiveSlotChanged?.Invoke(-1);
                        OnHeldItemChanged?.Invoke(null);
                    }
                }
                else
                {
                    heldItemSlot.quantity = remaining;
                }
            }
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    // ── Grenade → Weapon Slot 4 ───────────────────────────
    public bool MoveGrenadeToWeaponSlot(InventorySlot fromGridSlot)
    {
        if (fromGridSlot.IsEmpty || fromGridSlot.item.category != ItemCategory.Grenade)
            return false;

        var grenadeSlot = weaponSlots[3];
        if (!grenadeSlot.IsEmpty && grenadeSlot.item != fromGridSlot.item)
        {
            var swapSlot = new InventorySlot();
            swapSlot.Set(grenadeSlot.item, grenadeSlot.quantity);
            itemSlots.Add(swapSlot);
        }

        grenadeSlot.Set(fromGridSlot.item, fromGridSlot.quantity);
        fromGridSlot.Clear();
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ── Active Slot ───────────────────────────────────────
    public void SelectWeaponSlot(int index)
    {
        if (index < 0 || index > 3) return;

        activeSlot = index;
        activeWeaponSlot = index;

        OnActiveSlotChanged?.Invoke(index);
        OnHeldItemChanged?.Invoke(weaponSlots[index].item);
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Inventory] Slot {index + 1}: {weaponSlots[index].item?.itemName ?? "Trống"}");
    }

    public void SelectItemSlot()
    {
        activeSlot = 4;
        activeWeaponSlot = -1;

        OnActiveSlotChanged?.Invoke(4);
        OnHeldItemChanged?.Invoke(heldItemSlot.item);
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Inventory] Slot 5 | Item: {heldItemSlot.item?.itemName ?? "Trống"}");
    }

    public bool AssignItemSlot(ItemDataSO item)
    {
        if (item == null || item.category != ItemCategory.QuestItem)
        {
            Debug.Log("[Inventory] Chỉ QuestItem mới vào slot 5!");
            return false;
        }

        heldItemSlot.Set(item, CountItem(item));
        OnHeldItemChanged?.Invoke(item);
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory] Slot 5: {item.itemName}");
        return true;
    }

    public void ClearItemSlot()
    {
        heldItemSlot.Clear();
        if (activeSlot == 4)
        {
            activeSlot = -1;
            OnActiveSlotChanged?.Invoke(-1);
        }
        OnHeldItemChanged?.Invoke(null);
        OnInventoryChanged?.Invoke();
    }

    public void DeselectAll()
    {
        activeSlot = -1;
        activeWeaponSlot = -1;
        OnActiveSlotChanged?.Invoke(-1);
        OnHeldItemChanged?.Invoke(null);
        OnInventoryChanged?.Invoke();
    }

    // ── Query ─────────────────────────────────────────────
    public bool IsHoldingQuestItem()
        => activeSlot == 4 && !heldItemSlot.IsEmpty;

    public bool IsHoldingItem(ItemDataSO item)
        => activeSlot == 4
        && !heldItemSlot.IsEmpty
        && heldItemSlot.item == item;

    public bool IsHoldingFuse(string fuseID)
        => activeSlot == 4
        && !heldItemSlot.IsEmpty
        && heldItemSlot.item is FuseItemDataSO f
        && f.fuseID == fuseID;

    public ItemDataSO GetHeldItem()
    {
        if (activeSlot == 4) return heldItemSlot.item;
        if (activeSlot >= 0 && activeSlot <= 3) return weaponSlots[activeSlot].item;
        return null;
    }

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