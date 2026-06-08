using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlot5UI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Refs")]
    public Image iconImage;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI hintLabel;

    [Header("Colors")]
    public Color colorHasItem = new Color(0.4f, 0.75f, 1f, 1f);
    public Color colorEmpty = new Color(1f, 1f, 1f, 0.25f);

    private GameObject dragIcon;

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

        if (!empty)
        {
            var item = inv.heldItemSlot.item;
            if (iconImage != null && item.icon != null)
                iconImage.sprite = item.icon;

            if (nameLabel != null)
            {
                int qty = inv.heldItemSlot.quantity;
                nameLabel.text = qty > 1 ? $"{item.itemName} x{qty}" : item.itemName;
                nameLabel.color = colorHasItem;
            }

            if (hintLabel != null)
            {
                hintLabel.gameObject.SetActive(true);
                hintLabel.text = "Nhấn [5] để cầm";
            }
        }
        else
        {
            if (nameLabel != null)
            {
                nameLabel.text = "Trống";
                nameLabel.color = colorEmpty;
            }

            if (hintLabel != null)
            {
                hintLabel.gameObject.SetActive(true);
                hintLabel.text = "Kéo QuestItem vào đây";
            }
        }
    }

    // Nhận đồ từ balo kéo vào
    public void OnDrop(PointerEventData e)
    {
        var source = e.pointerDrag?.GetComponent<ItemSlotUI>();
        if (source == null || source.BoundSlot == null || source.BoundSlot.IsEmpty)
            return;

        var item = source.BoundSlot.item;
        if (item.category != ItemCategory.QuestItem) return;

        InventorySystem.Instance.MoveQuestItemToSlot5(source.BoundSlot);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        var inv = InventorySystem.Instance;
        if (inv != null && !inv.heldItemSlot.IsEmpty)
            TooltipUI.Show(inv.heldItemSlot.item);
    }

    public void OnPointerExit(PointerEventData e)
    {
        TooltipUI.Hide();
    }

    // ── XỬ LÝ KÉO ITEM ĐI ĐỂ CẤT ──────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        var inv = InventorySystem.Instance;
        if (inv == null || inv.heldItemSlot.IsEmpty) return;

        // Tạo icon ma bay theo chuột
        dragIcon = new GameObject("DragGhost_Slot5");
        Canvas canvas = GetComponentInParent<Canvas>();
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        Image img = dragIcon.AddComponent<Image>();
        img.sprite = inv.heldItemSlot.item.icon;

        // Tắt raycast để chuột xuyên qua hình này, bấm trúng ô lưới bên dưới
        img.raycastTarget = false;
        img.preserveAspect = true;
        img.rectTransform.sizeDelta = new Vector2(70, 70);

        if (iconImage != null) iconImage.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null) Destroy(dragIcon);
        if (iconImage != null) iconImage.color = new Color(1, 1, 1, 1f);
    }
}