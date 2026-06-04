using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    public Image iconImage;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI weightText;

    public InventorySlot BoundSlot { get; private set; }

    static GameObject _dragGhost;      // ghost icon theo chuột
    static ItemSlotUI _dragSource;

    public void Bind(InventorySlot slot)
    {
        BoundSlot = slot;
        Refresh();
    }

    public void Refresh()
    {
        bool empty = BoundSlot == null || BoundSlot.IsEmpty;
        iconImage.enabled = !empty;
        quantityText.text = empty ? "" : (BoundSlot.quantity > 1 ? $"x{BoundSlot.quantity}" : "");
        weightText.text = empty ? "" : $"{BoundSlot.item.weightPerUnit * BoundSlot.quantity}";
        if (!empty) iconImage.sprite = BoundSlot.item.icon;
    }

    // ── Drag ─────────────────────────────────────────────────
    public void OnBeginDrag(PointerEventData e)
    {
        if (BoundSlot == null || BoundSlot.IsEmpty) return;
        _dragSource = this;

        _dragGhost = new GameObject("DragGhost");
        _dragGhost.transform.SetParent(transform.root, false); // top canvas
        var img = _dragGhost.AddComponent<Image>();
        img.sprite = BoundSlot.item.icon;
        img.raycastTarget = false;
        var rt = _dragGhost.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(48, 48);
        rt.anchoredPosition = e.position;
    }

    public void OnDrag(PointerEventData e)
    {
        if (_dragGhost == null) return;
        _dragGhost.GetComponent<RectTransform>().anchoredPosition += e.delta;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_dragGhost != null) Destroy(_dragGhost);
        _dragSource = null;
    }

    // ── Drop ─────────────────────────────────────────────────
    public void OnDrop(PointerEventData e)
    {
        if (_dragSource == null || _dragSource == this) return;

        // Không swap nếu 1 trong 2 slot null
        if (BoundSlot == null || _dragSource.BoundSlot == null) return;

        var inv = InventorySystem.Instance;

        // Swap
        var tmpItem = BoundSlot.item;
        var tmpQty = BoundSlot.quantity;

        BoundSlot.Set(_dragSource.BoundSlot.item, _dragSource.BoundSlot.quantity);
        _dragSource.BoundSlot.Set(tmpItem, tmpQty);

        // Dùng method public thay vì gọi event trực tiếp
        inv?.NotifyInventoryChanged();

        Refresh();
        _dragSource.Refresh();
    }

    // ── Tooltip ───────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        if (BoundSlot == null || BoundSlot.IsEmpty) return;
        TooltipUI.Show(BoundSlot.item);
    }

    public void OnPointerExit(PointerEventData e) => TooltipUI.Hide();
}