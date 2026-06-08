using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlot5UI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Refs")]
    public Image iconImage;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI hintLabel;

    [Header("Colors")]
    public Color colorHasItem = new Color(0.4f, 0.75f, 1f, 1f);
    public Color colorEmpty = new Color(1f, 1f, 1f, 0.25f);

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
                nameLabel.text = item.itemName;
                nameLabel.color = colorHasItem;
            }

            // Hiển thị hướng dẫn khi đã lắp QuestItem
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

            // Hiển thị hướng dẫn khi ô trống
            if (hintLabel != null)
            {
                hintLabel.gameObject.SetActive(true);
                hintLabel.text = "Kéo QuestItem vào đây";
            }
        }
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
            Debug.Log("[Slot5] Chỉ QuestItem mới kéo vào được!");
            return;
        }

        // Gọi hàm di chuyển trực tiếp từ ô lưới sang Slot 5
        InventorySystem.Instance.MoveQuestItemToSlot5(source.BoundSlot);

        // Không cần gọi Refresh() ở đây nữa vì OnInventoryChanged trong hàm Move đã tự động lo việc đó rồi.
    }

    // ── Hover feedback ────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        var inv = InventorySystem.Instance;
        // Chỉ hiện Tooltip nếu trong ô đang có item
        if (inv != null && !inv.heldItemSlot.IsEmpty)
        {
            TooltipUI.Show(inv.heldItemSlot.item);
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        TooltipUI.Hide();
    }
}