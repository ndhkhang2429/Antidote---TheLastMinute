using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    [Header("Equipment Slots")]
    public EquipmentSlot helmetSlot;
    public EquipmentSlot armorSlot;
    public BackpackData equippedBackpack;

    [Header("Weapon Slots (4 cố định)")]
    public WeaponSlot[] weaponSlots = new WeaponSlot[4];

    [Header("Item Bag")]
    public List<InventorySlot> bagSlots = new List<InventorySlot>();

    public int CurrentWeight => bagSlots.Sum(s => s.item.weight * s.amount);
    public int MaxWeight => equippedBackpack != null ? equippedBackpack.maxCapacity : 30;

    public bool TryPickupItem(ItemData item, int amount = 1)
    {
        if (item is WeaponData weapon)
            return TryEquipWeapon(weapon);

        if (item.category == ItemCategory.Equipment)
            return TryEquipGear(item);

        if (CurrentWeight + item.weight * amount > MaxWeight)
        {
            // SỬA LỖI: Tạm thời dùng Debug.Log thay cho UIManager chưa tồn tại
            Debug.LogWarning("Balo đầy! Không thể nhặt thêm: " + item.itemName);
            // Nếu bạn dùng InteractionUIManager từ script trước, có thể đổi thành:
            // InteractionUIManager.Instance.ShowPrompt("Balo đầy!");
            return false;
        }

        AddToBag(item, amount);
        return true;
    }

    private bool TryEquipWeapon(WeaponData weapon)
    {
        int idx = (int)weapon.slotType;
        if (weaponSlots[idx].weapon != null)
        {
            // SỬA LỖI: Tạm thời dùng Debug.Log thay cho WorldItemSpawner chưa tồn tại
            Debug.Log($"Đã vứt vũ khí cũ [{weaponSlots[idx].weapon.itemName}] ra đất!");
        }
        weaponSlots[idx].weapon = weapon;
        weaponSlots[idx].currentAmmo = weapon.magazineSize;
        OnInventoryChanged?.Invoke();
        return true;
    }

    private bool TryEquipGear(ItemData item)
    {
        Debug.Log($"Trang bị gear: {item.itemName}");
        return true;
    }

    private void AddToBag(ItemData item, int amount)
    {
        var existingSlot = bagSlots.FirstOrDefault(s => s.item == item);

        if (existingSlot != null)
        {
            existingSlot.amount += amount;
        }
        else
        {
            bagSlots.Add(new InventorySlot { item = item, amount = amount });
        }

        OnInventoryChanged?.Invoke();
    }

    public event Action OnInventoryChanged;
}

[System.Serializable]
public class EquipmentSlot
{
    public ItemData equipmentItem;
}

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;
}

[System.Serializable]
public class WeaponSlot
{
    public WeaponData weapon;
    public int currentAmmo;
}