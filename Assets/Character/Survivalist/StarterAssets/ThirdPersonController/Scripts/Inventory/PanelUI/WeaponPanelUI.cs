using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WeaponPanelUI : MonoBehaviour
{
    [Header("Weapon Slots (index 0=Pistol, 1=Rifle, 2=Melee, 3=Grenade)")]
    public WeaponSlotUI[] weaponSlotUIs = new WeaponSlotUI[4];

    public void Refresh()
    {
        var inv = InventorySystem.Instance;
        for (int i = 0; i < weaponSlotUIs.Length; i++)
        {
            if (weaponSlotUIs[i] != null)
                weaponSlotUIs[i].Bind(inv.weaponSlots[i], i);
        }
    }
}