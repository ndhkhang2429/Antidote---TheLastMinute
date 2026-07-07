using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Backpack — mặc định có sẵn, không cần loot")]
    [SerializeField] private float _defaultCapacity = 150f;

    [Header("Weapon Slots (0=Rifle, 1=Pistol, 2=Melee, 3=Grenade)")]
    public InventorySlot[] weaponSlots = new InventorySlot[4];

    [Header("Item Grid")]
    [SerializeField] private List<InventorySlot> itemSlots = new();
    [SerializeField] private int maxItemSlots = 32;

    [Header("Slot 5 — QuestItem đang cầm")]
    public InventorySlot heldItemSlot = new InventorySlot();

    [Header("Active Slot")]
    public int activeSlot = -1;
    public int activeWeaponSlot = -1;

    public float MaxCapacity => _defaultCapacity;
    public float UsedCapacity
    {
        get
        {
            float used = 0f;
            foreach (var slot in itemSlots)
                if (!slot.IsEmpty)
                    used += slot.item.weightPerUnit * slot.quantity;
            return used;
        }
    }

    public event Action OnInventoryChanged;
    public event Action<ItemDataSO> OnWeaponEquipped;
    public event Action<int> OnActiveSlotChanged;
    public event Action<ItemDataSO> OnHeldItemChanged;

    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();

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

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null && !weaponSlots[i].IsEmpty)
            {
                SelectWeaponSlot(i);
                break;
            }
        }

        OnInventoryChanged?.Invoke();
    }

    // Trả về số lượng còn dư (nếu nhặt xong dư 0 là nhặt sạch)
    public int PickupItem(ItemDataSO item, int amount = 1)
    {
        if (item == null) return amount;
        switch (item.category)
        {
            case ItemCategory.Equipment: return amount;
            case ItemCategory.Weapon:
                bool equipped = TryEquipWeapon(item as WeaponDataSO);
                return equipped ? (amount - 1) : amount;
            default: return TryAddToGrid(item, amount);
        }
    }

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
        WeaponSlotType.Rifle => 0,
        WeaponSlotType.PistolOrShotgun => 1,
        WeaponSlotType.Melee => 2,
        WeaponSlotType.Grenade => 3,
        WeaponSlotType.QuestItem => 4,
        _ => -1
    };

    public int TryAddToGrid(ItemDataSO item, int amount)
    {
        if (item.weightPerUnit <= 0f) return AddToSlots(item, amount);

        float freeCapacity = MaxCapacity - UsedCapacity;

        if (freeCapacity <= 0f) return amount;

        int canFit = Mathf.FloorToInt(freeCapacity / item.weightPerUnit);
        if (canFit <= 0) return amount;

        int amountToTry = Mathf.Min(amount, canFit);
        int leftoverFromWeight = amount - amountToTry;

        int leftoverFromSlots = AddToSlots(item, amountToTry);

        return leftoverFromWeight + leftoverFromSlots;
    }

    int AddToSlots(ItemDataSO item, int amount)
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

            if (item.category != ItemCategory.Document)
                QuestManager.Instance?.ReportEvent(QuestCompletionType.PickupItem, item.itemName);
        }

        return remaining;
    }

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
                else heldItemSlot.quantity = remaining;
            }
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool MoveGrenadeToWeaponSlot(InventorySlot fromGridSlot)
    {
        if (fromGridSlot.IsEmpty || fromGridSlot.item.category != ItemCategory.Grenade) return false;
        ItemDataSO draggedItem = fromGridSlot.item;
        var grenadeSlot = weaponSlots[3];

        if (grenadeSlot.IsEmpty)
        {
            fromGridSlot.quantity -= 1;
            if (fromGridSlot.quantity <= 0) fromGridSlot.Clear();
            grenadeSlot.Set(draggedItem, 1);
            OnInventoryChanged?.Invoke();
            return true;
        }
        if (grenadeSlot.item == draggedItem) return false;

        ItemDataSO oldItem = grenadeSlot.item;
        int oldQty = grenadeSlot.quantity;
        fromGridSlot.quantity -= 1;
        bool wasCleared = false;
        if (fromGridSlot.quantity <= 0) { fromGridSlot.Clear(); wasCleared = true; }

        // --- ĐÃ FIX: Chuyển int sang điều kiện so sánh ---
        int leftover = TryAddToGrid(oldItem, oldQty);
        if (leftover == 0)
        {
            grenadeSlot.Set(draggedItem, 1);
            OnInventoryChanged?.Invoke();
            return true;
        }
        else
        {
            // Trả lại phần dư nếu không thể hoán đổi
            if (wasCleared) fromGridSlot.Set(draggedItem, 1);
            else fromGridSlot.quantity += 1;

            // Xóa bớt phần đã lỡ thêm vào Grid (Rollback) để tránh nhân bản item
            if (leftover < oldQty)
            {
                int addedAccidentally = oldQty - leftover;
                RemoveItem(oldItem, addedAccidentally);
            }
            return false;
        }
    }

    public bool MoveQuestItemToSlot5(InventorySlot fromGridSlot)
    {
        if (fromGridSlot.IsEmpty || fromGridSlot.item.category != ItemCategory.QuestItem) return false;
        ItemDataSO draggedItem = fromGridSlot.item;

        if (heldItemSlot.IsEmpty)
        {
            fromGridSlot.quantity -= 1;
            if (fromGridSlot.quantity <= 0) fromGridSlot.Clear();
            heldItemSlot.Set(draggedItem, 1);
            OnHeldItemChanged?.Invoke(heldItemSlot.item);
            OnInventoryChanged?.Invoke();
            return true;
        }
        if (heldItemSlot.item == draggedItem) return false;

        ItemDataSO oldItem = heldItemSlot.item;
        int oldQty = heldItemSlot.quantity;
        fromGridSlot.quantity -= 1;
        bool wasCleared = false;
        if (fromGridSlot.quantity <= 0) { fromGridSlot.Clear(); wasCleared = true; }

        // --- ĐÃ FIX: Chuyển int sang điều kiện so sánh ---
        int leftover = TryAddToGrid(oldItem, oldQty);
        if (leftover == 0)
        {
            heldItemSlot.Set(draggedItem, 1);
            OnHeldItemChanged?.Invoke(heldItemSlot.item);
            OnInventoryChanged?.Invoke();
            return true;
        }
        else
        {
            if (wasCleared) fromGridSlot.Set(draggedItem, 1);
            else fromGridSlot.quantity += 1;

            if (leftover < oldQty)
            {
                int addedAccidentally = oldQty - leftover;
                RemoveItem(oldItem, addedAccidentally);
            }
            return false;
        }
    }

    public void SelectWeaponSlot(int index)
    {
        if (index < 0 || index > 3) return;
        activeSlot = index;
        activeWeaponSlot = index;
        OnActiveSlotChanged?.Invoke(index);
        OnHeldItemChanged?.Invoke(weaponSlots[index].item);
        OnInventoryChanged?.Invoke();
    }

    public void SelectItemSlot()
    {
        activeSlot = 4;
        activeWeaponSlot = -1;
        OnActiveSlotChanged?.Invoke(4);
        OnHeldItemChanged?.Invoke(heldItemSlot.item);
        OnInventoryChanged?.Invoke();
    }

    public bool AssignItemSlot(ItemDataSO item)
    {
        if (item == null || item.category != ItemCategory.QuestItem) return false;
        heldItemSlot.Set(item, CountItem(item));
        OnHeldItemChanged?.Invoke(item);
        OnInventoryChanged?.Invoke();
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

    public bool IsHoldingQuestItem() => activeSlot == 4 && !heldItemSlot.IsEmpty;
    public bool IsHoldingItem(ItemDataSO item) => activeSlot == 4 && !heldItemSlot.IsEmpty && heldItemSlot.item == item;
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
    // --- CƠ CHẾ SỬ DỤNG VẬT PHẨM (MÁU, NƯỚC) ---
    // --- CƠ CHẾ SỬ DỤNG VẬT PHẨM (MÁU, NƯỚC) ---
    public void UseItem(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty) return;

        // Chỉ cho phép dùng nếu nó thuộc Category Consumable
        if (slot.item.category != ItemCategory.Consumable) return;

        ConsumableDataSO consumable = slot.item as ConsumableDataSO;
        if (consumable != null)
        {
            if (PlayerState.Instance != null)
            {
                HealthSystem playerHealth = PlayerState.Instance.GetComponent<HealthSystem>();
                PlayerStamina playerStamina = PlayerState.Instance.GetComponent<PlayerStamina>();

                // Kiểm tra xem người chơi có THỰC SỰ cần dùng vật phẩm này không
                bool needHealth = playerHealth != null && consumable.healthRestore > 0 && playerHealth.CurrentHP < playerHealth.MaxHP;
                bool needStamina = playerStamina != null && consumable.thirstRestore > 0 && playerStamina.currentStamina < playerStamina.maxStamina;

                // Nếu cả máu và thể lực đều không cần hồi (hoặc vật phẩm không cung cấp)
                if (!needHealth && !needStamina)
                {
                    InventoryUI.Instance.CloseInventory();
                    if (NotificationUI.Instance != null)
                        NotificationUI.Instance.ShowNotification("Chỉ số đang đầy, không cần dùng!");
                    return; // Ngắt hàm, không trừ item
                }

                InventoryUI.Instance.CloseInventory();
                float useTime = consumable.useTime;

                ActionTimerManager.Instance.StartAction($"Đang dùng {consumable.itemName}...", useTime, () =>
                {
                    // Hồi máu nếu vật phẩm có thông số hồi máu và người chơi đang mất máu
                    if (needHealth)
                    {
                        playerHealth.Heal(consumable.healthRestore);
                    }

                    // Hồi thể lực nếu vật phẩm có thông số hồi nước và người chơi đang mất sức
                    if (needStamina)
                    {
                        playerStamina.RestoreStamina(consumable.thirstRestore);
                    }

                    if (NotificationUI.Instance != null)
                    {
                        // Tạo thông báo linh hoạt dựa trên tác dụng của vật phẩm
                        string notifMsg = $"Đã dùng {consumable.itemName}";
                        NotificationUI.Instance.ShowNotification(notifMsg);
                    }

                    slot.quantity--;
                    if (slot.quantity <= 0) slot.Clear();
                    NotifyInventoryChanged();
                });
            }
        }
    }
}