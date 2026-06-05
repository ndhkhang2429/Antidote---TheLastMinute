using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HotbarSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Index: 0=Q, 1=E, 2=R")]
    public int slotIndex;

    [Header("UI Refs")]
    public Image iconImage;
    public TextMeshProUGUI keyLabel;    // chữ Q / E / R
    public TextMeshProUGUI itemLabel;   // tên item
    public TextMeshProUGUI qtyLabel;    // số lượng
    public Image borderImage; // viền (đổi màu khi active)

    static readonly string[] KeyNames = { "Q", "E", "R" };

    void Awake()
    {
        if (keyLabel != null)
            keyLabel.text = slotIndex < KeyNames.Length ? KeyNames[slotIndex] : "";
    }



    // ── Refresh UI từ data ────────────────────────────────
    public void Refresh()
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;

        var slot = inv.hotbarSlots[slotIndex];
        bool empty = slot.IsEmpty;

        if (iconImage != null) iconImage.enabled = !empty;
        if (itemLabel != null) itemLabel.text = empty ? "Trống" : slot.item.itemName;
        if (qtyLabel != null) qtyLabel.text = (!empty && slot.quantity > 1) ? $"x{slot.quantity}" : "";

        if (!empty && iconImage != null)
            iconImage.sprite = slot.item.icon;
    }

    // ── Nhận drag từ ItemSlotUI ───────────────────────────
    public void OnDrop(PointerEventData e)
    {
        Debug.Log($"[HotbarSlot {slotIndex}] OnDrop fired! drag object: {e.pointerDrag?.name ?? "NULL"}");

        var source = e.pointerDrag?.GetComponent<ItemSlotUI>();
        if (source == null)
        {
            Debug.Log("[HotbarSlot] Không tìm thấy ItemSlotUI trên drag object!");
            return;
        }
        if (source.BoundSlot == null || source.BoundSlot.IsEmpty)
        {
            Debug.Log("[HotbarSlot] BoundSlot trống!");
            return;
        }

        var item = source.BoundSlot.item;
        if (item.category == ItemCategory.Equipment || item.category == ItemCategory.Weapon)
        {
            Debug.Log("[HotbarSlot] Không cho vũ khí/trang bị vào hotbar");
            return;
        }

        InventorySystem.Instance.AssignHotbar(slotIndex, item, source.BoundSlot.quantity);
        Refresh();
    }

    // ── Tooltip ───────────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        var slot = InventorySystem.Instance?.hotbarSlots[slotIndex];
        if (slot == null || slot.IsEmpty) return;
        TooltipUI.Show(slot.item);
    }

    public void OnPointerExit(PointerEventData e) => TooltipUI.Hide();
}