using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Starting Equipment")]
    [SerializeField] private BackpackDataSO _startingBackpack;

    [Header("Equipment Slots")]
    public HelmetDataSO equippedHelmet;
    public VestDataSO equippedVest;
    public BackpackDataSO equippedBackpack;

    [Header("Weapon Slots (index 0-3 = ô 1-4)")]
    public InventorySlot[] weaponSlots = new InventorySlot[4];

    [Header("Item Grid (balo)")]
    [SerializeField] private List<InventorySlot> itemSlots = new();
    [SerializeField] private int maxItemSlots = 32;

    [Header("Hotbar")]
    public InventorySlot[] hotbarSlots = new InventorySlot[3];

    // ── Events ───────────────────────────────────────────────
    public event Action OnInventoryChanged;
    public event Action<ItemDataSO> OnWeaponEquipped;
    public event Action<int> OnHotbarUsed;

    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();

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

            var grenadeSlot = weaponSlots[3];
            if (!grenadeSlot.IsEmpty)
                used += grenadeSlot.item.weightPerUnit * grenadeSlot.quantity;
            return used;
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < weaponSlots.Length; i++) weaponSlots[i] = new InventorySlot();
        for (int i = 0; i < hotbarSlots.Length; i++) hotbarSlots[i] = new InventorySlot();
    }

    void Start()
    {
        if (equippedBackpack == null && _startingBackpack != null)
            equippedBackpack = _startingBackpack;

        // Pre-allocate toàn bộ slot ngay từ đầu
        itemSlots.Clear();
        for (int i = 0; i < maxItemSlots; i++)
            itemSlots.Add(new InventorySlot());
    }

    // ── Pickup ────────────────────────────────────────────────
    public bool PickupItem(ItemDataSO item, int amount = 1)
    {
        if (item == null) return false;

        switch (item.category)
        {
            case ItemCategory.Equipment:
                return TryEquipEquipment(item);
            case ItemCategory.Weapon:
                return TryEquipWeapon(item as WeaponDataSO);
            case ItemCategory.Grenade:
                return TryAddToGrid(item, amount);
            default:
                return TryAddToGrid(item, amount);
        }
    }

    // ── Equipment ─────────────────────────────────────────────
    bool TryEquipEquipment(ItemDataSO item)
    {
        if (item is BackpackDataSO bp)
        {
            equippedBackpack = bp;
            OnInventoryChanged?.Invoke();
            return true;
        }

        if (item is HelmetDataSO helmet)
        {
            equippedHelmet = helmet;
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Đội nón: {helmet.itemName} | Giảm {helmet.damageReduction * 100}% sát thương");
            return true;
        }

        if (item is VestDataSO vest)
        {
            equippedVest = vest;
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Mặc áo giáp: {vest.itemName} | Level {vest.vestLevel}");
            return true;
        }

        return false;
    }

    // ── Weapon ────────────────────────────────────────────────
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
    public bool TryAddToGrid(ItemDataSO item, int amount)
    {
        Debug.Log($"[DEBUG] TryAddToGrid called: {item.itemName} x{amount}");
        if (equippedBackpack == null)
        {
            Debug.Log("[Inventory] Không có balo!");
            return false;
        }

        // ── Fix: item không có weight thì không giới hạn theo weight ──
        if (item.weightPerUnit <= 0)
        {
            // Chỉ giới hạn số slot
            return AddToSlots(item, amount);
        }

        // Kiểm tra sức chứa
        int remaining = amount;
        int freeCapacity = MaxCapacity - UsedCapacity;

        if (freeCapacity <= 0)
        {
            Debug.Log("[Inventory] Balo đầy!");
            return false;
        }

        // Tính số lượng thực sự có thể nhặt
        int canFit = freeCapacity / item.weightPerUnit;
        if (canFit <= 0)
        {
            Debug.Log("[Inventory] Balo không đủ chỗ!");
            return false;
        }

        remaining = Mathf.Min(amount, canFit);
        return AddToSlots(item, remaining);
    }

    bool AddToSlots(ItemDataSO item, int amount)
    {
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

        // 2. Tìm ô trống đầu tiên — KHÔNG tạo slot mới
        if (remaining > 0)
        {
            foreach (var slot in itemSlots)
            {
                if (slot.IsEmpty)
                {
                    slot.Set(item, 0);
                    remaining = slot.Add(remaining);
                    if (remaining == 0) break;
                }
            }
        }

        if (remaining < amount)
        {
            OnInventoryChanged?.Invoke();
            return true;
        }

        Debug.Log("[Inventory] Balo đầy, không còn ô trống!");
        return false;
    }

    // ── Remove ────────────────────────────────────────────────
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
    public void AssignHotbar(int index, ItemDataSO item, int qty)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;
        hotbarSlots[index].Set(item, qty);
        OnInventoryChanged?.Invoke();
    }

    public void UseHotbar(int index)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;
        var slot = hotbarSlots[index];
        if (slot.IsEmpty) return;

        ItemUser.Use(slot.item, gameObject);
        slot.quantity--;
        if (slot.quantity <= 0) slot.Clear();

        OnHotbarUsed?.Invoke(index);
        OnInventoryChanged?.Invoke();
    }

    // ── Grenade → Weapon Slot 4 ───────────────────────────────
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

    // ── Helpers ───────────────────────────────────────────────
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