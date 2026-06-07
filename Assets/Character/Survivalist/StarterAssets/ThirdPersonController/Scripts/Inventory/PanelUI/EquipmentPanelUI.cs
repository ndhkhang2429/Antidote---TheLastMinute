using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentPanelUI : MonoBehaviour
{
    [Header("Capacity")]
    public Slider capacityBar;
    public TextMeshProUGUI capacityText;

    public void Refresh()
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;

        int max = inv.MaxCapacity;
        int used = inv.UsedCapacity;

        if (capacityBar != null)
        {
            capacityBar.maxValue = max > 0 ? max : 1;
            capacityBar.value = used;
        }

        if (capacityText != null)
            capacityText.text = $"{used} / {max}";
    }
}