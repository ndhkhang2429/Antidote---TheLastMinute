using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    public const int RifleSlotIndex = 0;
    public const int PistolSlotIndex = 1;
    public const int MeleeSlotIndex = 2;
    public const int ItemSlotIndex = 3;

    public const int WeaponSlotCount = 3;

    [Header("Backpack")]
    [SerializeField] private float _defaultCapacity = 150f;

    [Header("Weapon Slots (0=Rifle, 1=Pistol, 2=Melee)")]
    public InventorySlot[] weaponSlots =
        new InventorySlot[WeaponSlotCount];

    [Header("Item Grid")]
    [SerializeField] private List<InventorySlot> itemSlots = new();
    [SerializeField] private int maxItemSlots = 32;

    [Header("Slot 4 - Held Quest Item")]
    public InventorySlot heldItemSlot = new InventorySlot();

    [Header("Active Slot")]
    public int activeSlot = -1;
    public int activeWeaponSlot = -1;

    [Header("Number Key Input")]
    [Tooltip("Disable this if another script already handles slot input.")]
    [SerializeField] private bool handleNumberKeyInput = true;

    public float MaxCapacity => _defaultCapacity;

    public float UsedCapacity
    {
        get
        {
            float used = 0f;

            foreach (InventorySlot slot in itemSlots)
            {
                if (!slot.IsEmpty)
                    used += slot.item.weightPerUnit * slot.quantity;
            }

            return used;
        }
    }

    public event Action OnInventoryChanged;
    public event Action<ItemDataSO> OnWeaponEquipped;
    public event Action<int> OnActiveSlotChanged;
    public event Action<ItemDataSO> OnHeldItemChanged;

    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Start with three empty weapon slots.
        weaponSlots =
            new InventorySlot[WeaponSlotCount];

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            weaponSlots[i] =
                new InventorySlot();
        }

        // Start with an empty quest-item slot.
        heldItemSlot =
            new InventorySlot();
        itemSlots.Clear();

        activeSlot = -1;
        activeWeaponSlot = -1;
    }

    private void Start()
    {
        Debug.Log(
            $"[Inventory] Ready | Capacity: {MaxCapacity}"
        );

        /*
         * Tự chọn vũ khí đầu tiên đang có.
         */
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (!weaponSlots[i].IsEmpty)
            {
                SelectWeaponSlot(i);
                break;
            }
        }

        OnInventoryChanged?.Invoke();
    }

    private void Update()
    {
        if (!handleNumberKeyInput)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectWeaponSlot(RifleSlotIndex);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectWeaponSlot(PistolSlotIndex);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectWeaponSlot(MeleeSlotIndex);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectItemSlot();
        }
    }

    public int PickupItem(
        ItemDataSO item,
        int amount = 1)
    {
        if (item == null)
            return amount;

        switch (item.category)
        {
            case ItemCategory.Equipment:
                return amount;

            case ItemCategory.Weapon:
                {
                    bool equipped =
                        TryEquipWeapon(item as WeaponDataSO);

                    return equipped
                        ? amount - 1
                        : amount;
                }

            default:
                return TryAddToGrid(item, amount);
        }
    }

    public bool TryEquipWeapon(WeaponDataSO weapon)
    {
        if (weapon == null)
            return false;

        int slotIndex =
            WeaponSlotIndex(weapon.weaponSlotType);

        /*
         * Grenade và QuestItem không còn được đưa vào
         * mảng weaponSlots.
         */
        if (slotIndex < 0 ||
            slotIndex >= WeaponSlotCount)
        {
            return false;
        }

        weaponSlots[slotIndex].Set(weapon, 1);

        OnWeaponEquipped?.Invoke(weapon);
        OnInventoryChanged?.Invoke();

        return true;
    }

    private int WeaponSlotIndex(WeaponSlotType type)
    {
        return type switch
        {
            WeaponSlotType.Rifle =>
                RifleSlotIndex,

            WeaponSlotType.PistolOrShotgun =>
                PistolSlotIndex,

            WeaponSlotType.Melee =>
                MeleeSlotIndex,

            WeaponSlotType.QuestItem => -1,

            _ => -1
        };
    }

    public int TryAddToGrid(
        ItemDataSO item,
        int amount)
    {
        if (item == null || amount <= 0)
            return amount;

        if (item.weightPerUnit <= 0f)
            return AddToSlots(item, amount);

        float freeCapacity =
            MaxCapacity - UsedCapacity;

        if (freeCapacity <= 0f)
            return amount;

        int canFit = Mathf.FloorToInt(
            freeCapacity / item.weightPerUnit
        );

        if (canFit <= 0)
            return amount;

        int amountToTry =
            Mathf.Min(amount, canFit);

        int leftoverFromWeight =
            amount - amountToTry;

        int leftoverFromSlots =
            AddToSlots(item, amountToTry);

        return leftoverFromWeight +
               leftoverFromSlots;
    }

    private int AddToSlots(
        ItemDataSO item,
        int amount)
    {
        int remaining = amount;

        foreach (InventorySlot slot in itemSlots)
        {
            if (slot.IsEmpty ||
                slot.item != item ||
                slot.IsFull)
            {
                continue;
            }

            remaining = slot.Add(remaining);

            if (remaining == 0)
                break;
        }

        while (remaining > 0 &&
               itemSlots.Count < maxItemSlots)
        {
            InventorySlot newSlot =
                new InventorySlot();

            newSlot.Set(item, 0);
            remaining = newSlot.Add(remaining);

            itemSlots.Add(newSlot);
        }

        if (remaining < amount)
        {
            if (!heldItemSlot.IsEmpty &&
                heldItemSlot.item == item)
            {
                heldItemSlot.quantity =
                    CountItem(item);
            }

            OnInventoryChanged?.Invoke();

            if (item.category != ItemCategory.Document)
            {
                QuestManager.Instance?.ReportEvent(
                    QuestCompletionType.PickupItem,
                    item.itemName
                );
            }
        }

        return remaining;
    }

    public bool RemoveItem(
        ItemDataSO item,
        int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int toRemove = amount;

        foreach (InventorySlot slot in itemSlots)
        {
            if (slot.IsEmpty || slot.item != item)
                continue;

            int take =
                Mathf.Min(slot.quantity, toRemove);

            slot.quantity -= take;
            toRemove -= take;

            if (slot.quantity <= 0)
                slot.Clear();

            if (toRemove <= 0)
                break;
        }

        bool success = toRemove < amount;

        if (!success)
            return false;

        if (!heldItemSlot.IsEmpty &&
            heldItemSlot.item == item)
        {
            int remaining = CountItem(item);

            if (remaining <= 0)
            {
                heldItemSlot.Clear();

                if (activeSlot == ItemSlotIndex)
                {
                    activeSlot = -1;
                    activeWeaponSlot = -1;

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

    /*
     * Slot 4 mới dành cho QuestItem.
     */
    public bool MoveQuestItemToSlot4(
        InventorySlot fromGridSlot)
    {
        if (fromGridSlot == null ||
            fromGridSlot.IsEmpty ||
            fromGridSlot.item.category !=
            ItemCategory.QuestItem)
        {
            return false;
        }

        ItemDataSO draggedItem =
            fromGridSlot.item;

        if (heldItemSlot.IsEmpty)
        {
            fromGridSlot.quantity--;

            if (fromGridSlot.quantity <= 0)
                fromGridSlot.Clear();

            heldItemSlot.Set(draggedItem, 1);

            OnHeldItemChanged?.Invoke(
                heldItemSlot.item
            );

            OnInventoryChanged?.Invoke();
            return true;
        }

        if (heldItemSlot.item == draggedItem)
            return false;

        ItemDataSO oldItem =
            heldItemSlot.item;

        int oldQuantity =
            heldItemSlot.quantity;

        fromGridSlot.quantity--;

        bool wasCleared = false;

        if (fromGridSlot.quantity <= 0)
        {
            fromGridSlot.Clear();
            wasCleared = true;
        }

        int leftover =
            TryAddToGrid(oldItem, oldQuantity);

        if (leftover == 0)
        {
            heldItemSlot.Set(draggedItem, 1);

            OnHeldItemChanged?.Invoke(
                heldItemSlot.item
            );

            OnInventoryChanged?.Invoke();
            return true;
        }

        /*
         * Rollback nếu không thể đưa item cũ
         * trở lại inventory.
         */
        if (wasCleared)
            fromGridSlot.Set(draggedItem, 1);
        else
            fromGridSlot.quantity++;

        if (leftover < oldQuantity)
        {
            int accidentallyAdded =
                oldQuantity - leftover;

            RemoveItem(
                oldItem,
                accidentallyAdded
            );
        }

        return false;
    }

    public void SelectWeaponSlot(int index)
    {
        if (index < 0 ||
            index >= WeaponSlotCount)
        {
            return;
        }

        activeSlot = index;
        activeWeaponSlot = index;

        OnActiveSlotChanged?.Invoke(index);

        OnHeldItemChanged?.Invoke(
            weaponSlots[index].item
        );

        OnInventoryChanged?.Invoke();
    }

    public void SelectItemSlot()
    {
        activeSlot = ItemSlotIndex;
        activeWeaponSlot = -1;

        OnActiveSlotChanged?.Invoke(
            ItemSlotIndex
        );

        OnHeldItemChanged?.Invoke(
            heldItemSlot.item
        );

        OnInventoryChanged?.Invoke();
    }

    public bool AssignItemSlot(ItemDataSO item)
    {
        if (item == null ||
            item.category != ItemCategory.QuestItem)
        {
            return false;
        }

        heldItemSlot.Set(
            item,
            CountItem(item)
        );

        OnHeldItemChanged?.Invoke(item);
        OnInventoryChanged?.Invoke();

        return true;
    }

    public void ClearItemSlot()
    {
        heldItemSlot.Clear();

        if (activeSlot == ItemSlotIndex)
        {
            activeSlot = -1;
            activeWeaponSlot = -1;

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

    public bool IsHoldingQuestItem()
    {
        return activeSlot == ItemSlotIndex &&
               !heldItemSlot.IsEmpty;
    }

    public bool IsHoldingItem(ItemDataSO item)
    {
        return activeSlot == ItemSlotIndex &&
               !heldItemSlot.IsEmpty &&
               heldItemSlot.item == item;
    }

    public ItemDataSO GetHeldItem()
    {
        if (activeSlot == ItemSlotIndex)
            return heldItemSlot.item;

        if (activeSlot >= 0 &&
            activeSlot < WeaponSlotCount)
        {
            return weaponSlots[activeSlot].item;
        }

        return null;
    }

    public List<InventorySlot> GetItemSlots()
    {
        return itemSlots;
    }

    public bool HasItem(
        ItemDataSO item,
        int amount = 1)
    {
        int count = 0;

        foreach (InventorySlot slot in itemSlots)
        {
            if (!slot.IsEmpty &&
                slot.item == item)
            {
                count += slot.quantity;
            }
        }

        return count >= amount;
    }

    public int CountItem(ItemDataSO item)
    {
        int count = 0;

        foreach (InventorySlot slot in itemSlots)
        {
            if (!slot.IsEmpty &&
                slot.item == item)
            {
                count += slot.quantity;
            }
        }

        return count;
    }

    public void UseItem(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty)
            return;

        if (slot.item.category !=
            ItemCategory.Consumable)
        {
            return;
        }

        ConsumableDataSO consumable =
            slot.item as ConsumableDataSO;

        if (consumable == null ||
            PlayerState.Instance == null)
        {
            return;
        }

        HealthSystem playerHealth =
            PlayerState.Instance
                .GetComponent<HealthSystem>();

        PlayerStamina playerStamina =
            PlayerState.Instance
                .GetComponent<PlayerStamina>();

        bool needsHealth =
            playerHealth != null &&
            consumable.healthRestore > 0 &&
            playerHealth.CurrentHP <
            playerHealth.MaxHP;

        bool needsStamina =
            playerStamina != null &&
            consumable.thirstRestore > 0 &&
            playerStamina.currentStamina <
            playerStamina.maxStamina;

        if (!needsHealth && !needsStamina)
        {
            InventoryUI.Instance?.CloseInventory();

            NotificationUI.Instance?.ShowNotification(
                "Your vitals are already stable."
            );

            return;
        }

        InventoryUI.Instance?.CloseInventory();

        float useTime = consumable.useTime;

        ActionTimerManager.Instance.StartAction(
            $"Using {consumable.itemName}...",
            useTime,
            () =>
            {
                if (needsHealth)
                {
                    playerHealth.Heal(
                        consumable.healthRestore
                    );
                }

                if (needsStamina)
                {
                    playerStamina.RestoreStamina(
                        consumable.thirstRestore
                    );
                }

                NotificationUI.Instance?.ShowNotification(
                    $"{consumable.itemName} used."
                );

                slot.quantity--;

                if (slot.quantity <= 0)
                    slot.Clear();

                NotifyInventoryChanged();
            }
        );
    }
}