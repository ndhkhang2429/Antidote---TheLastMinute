using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Starting Equipment")]
    [SerializeField] private BackpackDataSO _startingBackpack;

    [Header("Equipment Slots")]
    public ItemDataSO equippedHelmet;
    public ItemDataSO equippedVest;
    public BackpackDataSO equippedBackpack;

    [Header("Weapon Slots (0=Pistol, 1=Rifle, 2=Melee, 3=Grenade)")]
    public InventorySlot[] weaponSlots = new InventorySlot[4];

    [Header("Item Grid")]
    [SerializeField] private List<InventorySlot> itemSlots = new();
    [SerializeField] private int maxItemSlots = 32;

    [Header("Slot 5 — QuestItem được gán thủ công")]
    public InventorySlot heldItemSlot = new InventorySlot();

    [Header("Active Slot (0-3=vũ khí, 4=item, -1=không cầm)")]
    public int activeSlot = -1;
    public int activeWeaponSlot = -1;

    // ── Events ───────────────────────────────────────────
    public event Action OnInventoryChanged;
    public event Action<ItemDataSO> OnWeaponEquipped;
    public event Action<int> OnActiveSlotChanged;  // 0-4
    public event Action<ItemDataSO> OnHeldItemChanged;

    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();

    // ── Capacity ─────────────────────────────────────────
    public int MaxCapacity => equippedBackpack != null ? equippedBackpack.capacity : 0;

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
        if (equippedBackpack == null && _startingBackpack != null)
        {
            equippedBackpack = _startingBackpack;
            Debug.Log($"[Inventory] Balo mặc định: {equippedBackpack.itemName} | Sức chứa: {equippedBackpack.capacity}");
        }
    }

    // ── Pickup ────────────────────────────────────────────
    public bool PickupItem(ItemDataSO item, int amount = 1)
    {
        if (item == null) return false;

        switch (item.category)
        {
            case ItemCategory.Equipment:
                return TryEquipEquipment(item);
            case ItemCategory.Weapon:
                return TryEquipWeapon(item as WeaponDataSO);
            default:
                return TryAddToGrid(item, amount);
        }
    }

    // ── Equipment ─────────────────────────────────────────
    bool TryEquipEquipment(ItemDataSO item)
    {
        if (item is BackpackDataSO bp)
        {
            equippedBackpack = bp;
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Trang bị balo: {bp.itemName}");
            return true;
        }

        string name = item.itemName.ToLower();
        if (name.Contains("nón") || name.Contains("helmet") || name.Contains("cap"))
        {
            equippedHelmet = item;
            OnInventoryChanged?.Invoke();
            return true;
        }

        equippedVest = item;
        OnInventoryChanged?.Invoke();
        return true;
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
        WeaponSlotType.PistolOrShotgun => 0,
        WeaponSlotType.Rifle => 1,
        WeaponSlotType.Melee => 2,
        WeaponSlotType.Grenade => 3,
        _ => -1
    };

    // ── Item Grid ─────────────────────────────────────────
    public bool TryAddToGrid(ItemDataSO item, int amount)
    {
        if (equippedBackpack == null)
        {
            Debug.Log("[Inventory] Không có balo!");
            return false;
        }

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

        // Stack vào ô đã có cùng loại
        foreach (var slot in itemSlots)
        {
            if (!slot.IsEmpty && slot.item == item && !slot.IsFull)
            {
                remaining = slot.Add(remaining);
                if (remaining == 0) break;
            }
        }

        // Mở ô mới
        while (remaining > 0 && itemSlots.Count < maxItemSlots)
        {
            var newSlot = new InventorySlot();
            newSlot.Set(item, 0);
            remaining = newSlot.Add(remaining);
            itemSlots.Add(newSlot);
        }

        if (remaining < amount)
        {
            // Cập nhật số lượng heldItemSlot nếu đang cầm item này
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
            // Cập nhật heldItemSlot nếu đang cầm item này
            if (!heldItemSlot.IsEmpty && heldItemSlot.item == item)
            {
                int remaining = CountItem(item);
                if (remaining <= 0)
                {
                    // Hết item → xóa slot 5
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

        // Swap về grid nếu slot 4 đang có item khác
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

    // ── Active Slot Selection ─────────────────────────────

    /// <summary>Nhấn phím 1-4</summary>
    public void SelectWeaponSlot(int index)
    {
        if (index < 0 || index > 3) return;

        activeSlot = index;
        activeWeaponSlot = index;

        OnActiveSlotChanged?.Invoke(index);
        OnHeldItemChanged?.Invoke(weaponSlots[index].item);
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Inventory] Active slot {index + 1}: {weaponSlots[index].item?.itemName ?? "Trống"}");
    }

    /// <summary>Nhấn phím 5 — chỉ active slot 5, không cycle</summary>
    public void SelectItemSlot()
    {
        activeSlot = 4;
        activeWeaponSlot = -1;

        OnActiveSlotChanged?.Invoke(4);
        OnHeldItemChanged?.Invoke(heldItemSlot.item);
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Inventory] Active slot 5 | Item: {heldItemSlot.item?.itemName ?? "Trống"}");
    }

    /// <summary>
    /// Gán QuestItem vào slot 5 — gọi từ ItemSlot5UI khi drag
    /// </summary>
    public bool AssignItemSlot(ItemDataSO item)
    {
        if (item == null)
        {
            Debug.Log("[Inventory] Item null!");
            return false;
        }

        if (item.category != ItemCategory.QuestItem)
        {
            Debug.Log("[Inventory] Chỉ QuestItem mới vào được slot 5!");
            return false;
        }

        heldItemSlot.Set(item, CountItem(item));

        OnHeldItemChanged?.Invoke(item);
        OnInventoryChanged?.Invoke();

        Debug.Log($"[Inventory] Gán slot 5: {item.itemName}");
        return true;
    }

    /// <summary>Xóa slot 5</summary>
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

        Debug.Log("[Inventory] Xóa slot 5");
    }

    /// <summary>Bỏ chọn tất cả</summary>
    public void DeselectAll()
    {
        activeSlot = -1;
        activeWeaponSlot = -1;

        OnActiveSlotChanged?.Invoke(-1);
        OnHeldItemChanged?.Invoke(null);
        OnInventoryChanged?.Invoke();
    }

    // ── Query Helpers ─────────────────────────────────────

    /// <summary>Đang active slot 5 và có item</summary>
    public bool IsHoldingQuestItem()
        => activeSlot == 4 && !heldItemSlot.IsEmpty;

    /// <summary>Đang cầm đúng item này</summary>
    public bool IsHoldingItem(ItemDataSO item)
        => activeSlot == 4
        && !heldItemSlot.IsEmpty
        && heldItemSlot.item == item;

    /// <summary>Đang cầm fuse đúng ID</summary>
    public bool IsHoldingFuse(string fuseID)
        => activeSlot == 4
        && !heldItemSlot.IsEmpty
        && heldItemSlot.item is FuseItemDataSO f
        && f.fuseID == fuseID;

    /// <summary>Item gì đang được active</summary>
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