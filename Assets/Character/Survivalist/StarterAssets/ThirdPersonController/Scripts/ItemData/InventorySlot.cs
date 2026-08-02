using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemDataSO item;
    public int quantity;

    [Header("Weapon Runtime State")]
    public int currentAmmo = -1;
    public bool ammoInitialized;

    public bool IsEmpty => item == null;

    public bool IsFull =>
        item != null &&
        quantity >= item.maxStack;

    public int Add(int amount)
    {
        if (item == null)
            return amount;

        int canAdd =
            item.maxStack - quantity;

        int actualAdd =
            Math.Min(canAdd, amount);

        quantity += actualAdd;

        return amount - actualAdd;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;

        currentAmmo = -1;
        ammoInitialized = false;
    }

    public void Set(
        ItemDataSO newItem,
        int qty)
    {
        bool itemChanged =
            item != newItem;

        item = newItem;
        quantity = qty;

        if (newItem is WeaponDataSO weapon &&
            weapon.combatType == CombatType.Firearm)
        {
            /*
             * Chỉ nạp đầy khi khẩu súng được đưa vào
             * slot lần đầu hoặc khi thay bằng súng khác.
             */
            if (itemChanged || !ammoInitialized)
            {
                currentAmmo =
                    weapon.magazineSize;

                ammoInitialized = true;
            }
        }
        else
        {
            // Melee và item thường không có băng đạn.
            currentAmmo = -1;
            ammoInitialized = false;
        }
    }

    public void InitializeAmmoIfNeeded()
    {
        if (ammoInitialized)
            return;

        if (item is not WeaponDataSO weapon ||
            weapon.combatType != CombatType.Firearm)
        {
            currentAmmo = -1;
            ammoInitialized = false;
            return;
        }

        currentAmmo =
            weapon.magazineSize;

        ammoInitialized = true;
    }

    public void SetCurrentAmmo(int amount)
    {
        if (item is not WeaponDataSO weapon ||
            weapon.combatType != CombatType.Firearm)
        {
            currentAmmo = -1;
            ammoInitialized = false;
            return;
        }

        currentAmmo = Mathf.Clamp(
            amount,
            0,
            weapon.magazineSize
        );

        ammoInitialized = true;
    }
}