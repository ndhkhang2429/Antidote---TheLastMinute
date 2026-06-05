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
        Debug.Log($"[SlotUI] Bind — slot null:{slot == null} | empty:{slot?.IsEmpty} | item:{slot?.item?.itemName ?? "NULL"}");
        Refresh();
    }

    public void Refresh()
    {
        bool empty = BoundSlot == null || BoundSlot.IsEmpty;

        if (!empty)
        {
            iconImage.sprite = BoundSlot.item.icon != null ? BoundSlot.item.icon : null;
            iconImage.enabled = true;
            iconImage.color = Color.white; // ← THÊM DÒNG NÀY
            quantityText.text = BoundSlot.quantity > 1 ? $"{BoundSlot.quantity}" : "";
            weightText.text = BoundSlot.item.weightPerUnit > 0
                                ? $"{BoundSlot.item.weightPerUnit * BoundSlot.quantity}" : "";
        }
        else
        {
            iconImage.enabled = false;
            iconImage.color = Color.white;
            quantityText.text = "";
            weightText.text = "";
        }
    }

    // ── Drag ─────────────────────────────────────────────────
    public void OnBeginDrag(PointerEventData e)
    {
        if (BoundSlot == null || BoundSlot.IsEmpty) return;
        _dragSource = this;

        _dragGhost = new GameObject("DragGhost");

        // Đặt vào Canvas gốc (top-level)
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        _dragGhost.transform.SetParent(rootCanvas.transform, false);

        var img = _dragGhost.AddComponent<Image>();
        img.sprite = BoundSlot.item.icon;
        img.raycastTarget = false; // QUAN TRỌNG: không chặn raycast
        img.color = new Color(1, 1, 1, 0.8f);

        var rt = _dragGhost.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(48, 48);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Dùng position thực thay vì anchoredPosition
        MoveGhostToPointer(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_dragGhost == null) return;
        MoveGhostToPointer(e);
    }

    void MoveGhostToPointer(PointerEventData e)
    {
        var rt = _dragGhost.GetComponent<RectTransform>();
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        // Convert screen position sang local position của root canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            e.position,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : e.pressEventCamera,
            out Vector2 localPoint
        );

        rt.localPosition = localPoint;
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