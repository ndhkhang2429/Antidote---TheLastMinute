using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlot5UI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Refs")]
    public Image iconImage;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI hintLabel;  // "Kéo QuestItem vào đây"
    public Image borderImage;

    [Header("Colors")]
    public Color colorHasItem = new Color(0.4f, 0.75f, 1f, 1f);
    public Color colorEmpty = new Color(1f, 1f, 1f, 0.25f);
    public Color borderNormal = new Color(1f, 1f, 1f, 0.1f);
    public Color borderHover = new Color(0.4f, 0.75f, 1f, 0.6f);
    public Color borderInvalid = new Color(1f, 0.3f, 0.3f, 0.6f);

    void Start()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;

        bool empty = inv.heldItemSlot.IsEmpty;

        if (iconImage != null) iconImage.enabled = !empty;
        if (hintLabel != null) hintLabel.gameObject.SetActive(empty);

        if (!empty)
        {
            var item = inv.heldItemSlot.item;
            if (iconImage != null && item.icon != null)
                iconImage.sprite = item.icon;
            if (nameLabel != null)
            {
                nameLabel.text = item.itemName;
                nameLabel.color = colorHasItem;
            }
        }
        else
        {
            if (nameLabel != null)
            {
                nameLabel.text = "Trống";
                nameLabel.color = colorEmpty;
            }
        }

        if (borderImage != null) borderImage.color = borderNormal;
    }

    // ── Drop ─────────────────────────────────────────────
    public void OnDrop(PointerEventData e)
    {
        var source = e.pointerDrag?.GetComponent<ItemSlotUI>();
        if (source == null || source.BoundSlot == null || source.BoundSlot.IsEmpty)
            return;

        var item = source.BoundSlot.item;

        // Chỉ nhận QuestItem
        if (item.category != ItemCategory.QuestItem)
        {
            if (borderImage != null) borderImage.color = borderInvalid;
            Debug.Log("[Slot5] Chỉ QuestItem mới kéo vào được!");
            return;
        }

        InventorySystem.Instance.AssignItemSlot(item);
        Refresh();
    }

    // ── Hover feedback ────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        if (borderImage == null) return;

        // Kiểm tra đang drag item gì
        var dragging = e.pointerDrag?.GetComponent<ItemSlotUI>();
        if (dragging != null && dragging.BoundSlot != null && !dragging.BoundSlot.IsEmpty)
        {
            bool valid = dragging.BoundSlot.item.category == ItemCategory.QuestItem;
            borderImage.color = valid ? borderHover : borderInvalid;
        }
        else
        {
            borderImage.color = borderHover;
        }

        TooltipUI.Show(InventorySystem.Instance?.heldItemSlot.item
                       ?? ScriptableObject.CreateInstance<ItemDataSO>());
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (borderImage != null) borderImage.color = borderNormal;
        TooltipUI.Hide();
    }
}