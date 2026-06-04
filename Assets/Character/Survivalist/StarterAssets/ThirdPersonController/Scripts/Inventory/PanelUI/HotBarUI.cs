using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarUI : MonoBehaviour
{
    [Header("Hotbar Slots (Q, E, R)")]
    public HotbarSlotUI[] slots = new HotbarSlotUI[3];

    [Header("Highlight — viền sáng khi dùng")]
    public Color activeColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.25f);

    void Start()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged += Refresh;

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHotbarUsed += OnHotbarUsed;

        Refresh();
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance == null) return;
        InventorySystem.Instance.OnInventoryChanged -= Refresh;
        InventorySystem.Instance.OnHotbarUsed -= OnHotbarUsed;
    }

    public void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i]?.Refresh();
    }

    // Flash highlight khi dùng
    void OnHotbarUsed(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        StartCoroutine(FlashSlot(index));
    }

    System.Collections.IEnumerator FlashSlot(int index)
    {
        var bg = slots[index].GetComponent<Image>();
        if (bg == null) yield break;

        Color original = bg.color;
        bg.color = activeColor;
        yield return new WaitForSeconds(0.12f);
        bg.color = original;
    }
}   