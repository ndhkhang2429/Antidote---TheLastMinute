using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentPanelUI : MonoBehaviour
{
    [Header("Helmet Slot")]
    public Image helmetIcon;
    public TextMeshProUGUI helmetName;
    public GameObject helmetEmptyHint;

    [Header("Vest Slot")]
    public Image vestIcon;
    public TextMeshProUGUI vestName;
    public GameObject vestEmptyHint;

    [Header("Backpack Slot")]
    public Image backpackIcon;
    public TextMeshProUGUI backpackName;
    public GameObject backpackEmptyHint;

    [Header("Capacity Bar")]
    public Slider capacityBar;
    public TextMeshProUGUI capacityText;

    public void Refresh()
    {
        var inv = InventorySystem.Instance;

        // Helmet
        RefreshSlot(inv.equippedHelmet, helmetIcon, helmetName, helmetEmptyHint);

        // Vest
        RefreshSlot(inv.equippedVest, vestIcon, vestName, vestEmptyHint);

        // Backpack
        if (inv.equippedBackpack != null)
        {
            backpackIcon.sprite = inv.equippedBackpack.icon;
            backpackIcon.enabled = true;
            backpackName.text = inv.equippedBackpack.itemName;
            backpackEmptyHint?.SetActive(false);
        }
        else
        {
            backpackIcon.enabled = false;
            backpackName.text = "";
            backpackEmptyHint?.SetActive(true);
        }

        // Capacity bar
        int max = inv.MaxCapacity;
        int used = inv.UsedCapacity;

        if (capacityBar != null)
        {
            capacityBar.maxValue = max > 0 ? max : 1;
            capacityBar.value = used;
        }

        if (capacityText != null)
            capacityText.text = max > 0 ? $"{used} / {max}" : "Không có balo";
    }

    void RefreshSlot(ItemDataSO item, Image icon, TextMeshProUGUI label, GameObject emptyHint)
    {
        if (item != null)
        {
            icon.sprite = item.icon;
            icon.enabled = true;
            label.text = item.itemName;
            emptyHint?.SetActive(false);
        }
        else
        {
            icon.enabled = false;
            label.text = "";
            emptyHint?.SetActive(true);
        }
    }
}