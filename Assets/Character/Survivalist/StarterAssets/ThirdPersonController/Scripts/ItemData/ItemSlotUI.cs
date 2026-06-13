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

    static GameObject _dragGhost;      // Ghost icon bay theo chuột
    static ItemSlotUI _dragSource;     // Ô gốc đang bị kéo

    public void Bind(InventorySlot slot)
    {
        BoundSlot = slot;
        Refresh();
    }

    public void Refresh()
    {
        if (BoundSlot == null || BoundSlot.IsEmpty)
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
            if (quantityText != null) quantityText.text = "";
            if (weightText != null) weightText.text = "";
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = BoundSlot.item.icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
        }

        if (quantityText != null)
            quantityText.text = BoundSlot.quantity > 1 ? $"{BoundSlot.quantity}" : "";

        if (weightText != null)
            weightText.text = BoundSlot.item.weightPerUnit > 0 ? $"{BoundSlot.item.weightPerUnit * BoundSlot.quantity}" : "";
    }

    // ── Drag (Bắt đầu kéo thả ô Balo) ────────────────────────
    public void OnBeginDrag(PointerEventData e)
    {
        if (BoundSlot == null || BoundSlot.IsEmpty) return;
        _dragSource = this;

        _dragGhost = new GameObject("DragGhost_Grid");
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            _dragGhost.transform.SetParent(canvas.rootCanvas.transform, false);
            _dragGhost.transform.SetAsLastSibling(); // Ép icon nổi lên trên cùng
        }

        var img = _dragGhost.AddComponent<Image>();
        img.sprite = BoundSlot.item.icon;
        img.raycastTarget = false; // QUAN TRỌNG: Để chuột xuyên qua icon ảo
        img.color = new Color(1f, 1f, 1f, 0.7f);
        img.preserveAspect = true;

        var rt = _dragGhost.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 60);

        MoveGhostToPointer(e);

        // Làm mờ icon ở ô gốc đi một chút để báo hiệu đang cầm nó
        if (iconImage != null) iconImage.color = new Color(1f, 1f, 1f, 0.3f);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_dragGhost != null) MoveGhostToPointer(e);
    }

    void MoveGhostToPointer(PointerEventData e)
    {
        var rt = _dragGhost.GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Canvas rootCanvas = canvas.rootCanvas;
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

        // Trả lại độ sáng cho ô gốc nếu nó chưa bị xóa
        if (iconImage != null && BoundSlot != null && !BoundSlot.IsEmpty)
            iconImage.color = Color.white;
    }

    // ── Drop (Xử lý khi thả đồ vào ô) ────────────────────────
    public void OnDrop(PointerEventData e)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;

        // --- TRƯỜNG HỢP 1: Nhận đồ từ Slot 5 (tay nhân vật) kéo vào Balo ---
        var slot5 = e.pointerDrag?.GetComponent<ItemSlot5UI>();
        if (slot5 != null)
        {
            if (!inv.heldItemSlot.IsEmpty)
            {
                ItemDataSO itemFromSlot5 = inv.heldItemSlot.item;
                int qtyFromSlot5 = inv.heldItemSlot.quantity;

                if (BoundSlot.IsEmpty)
                {
                    // Ô đang trống -> Cất thẳng vào đây
                    BoundSlot.Set(itemFromSlot5, qtyFromSlot5);
                    inv.ClearItemSlot();
                }
                else if (BoundSlot.item == itemFromSlot5 && !BoundSlot.IsFull)
                {
                    // Cùng loại -> Gộp (Stack) lại
                    int remain = BoundSlot.Add(qtyFromSlot5);
                    if (remain > 0) inv.heldItemSlot.quantity = remain;
                    else inv.ClearItemSlot();
                }
                else
                {
                    // Khác loại -> Swap với điều kiện vật cũ PHẢI LÀ QuestItem
                    if (BoundSlot.item.category == ItemCategory.QuestItem)
                    {
                        ItemDataSO tempItem = BoundSlot.item;
                        int tempQty = BoundSlot.quantity;

                        BoundSlot.Set(itemFromSlot5, qtyFromSlot5);
                        inv.heldItemSlot.Set(tempItem, tempQty);
                    }
                    else
                    {
                        // --- ĐÃ FIX: Chuyển sang dùng int leftover ---
                        int leftover = inv.TryAddToGrid(itemFromSlot5, qtyFromSlot5);

                        if (leftover == 0)
                        {
                            inv.ClearItemSlot(); // Đã cất hết vào balo
                        }
                        else
                        {
                            // Cập nhật lại số lượng còn dư trên tay (Slot 5)
                            inv.heldItemSlot.quantity = leftover;
                        }
                    }
                }
                inv.NotifyInventoryChanged();
            }
            return; // Xong việc với Slot 5 thì ngắt hàm
        }

        // --- TRƯỜNG HỢP 2: Kéo thả giữa các ô trong Balo với nhau ---
        if (_dragSource == null || _dragSource == this) return;
        if (BoundSlot == null || _dragSource.BoundSlot == null) return;

        // Cùng loại -> Gộp đồ
        if (!BoundSlot.IsEmpty && BoundSlot.item == _dragSource.BoundSlot.item && !BoundSlot.IsFull)
        {
            int remain = BoundSlot.Add(_dragSource.BoundSlot.quantity);
            if (remain > 0) _dragSource.BoundSlot.quantity = remain;
            else _dragSource.BoundSlot.Clear();
        }
        else
        {
            // Khác loại hoặc ô trống -> Tráo đổi
            var tmpItem = BoundSlot.item;
            var tmpQty = BoundSlot.quantity;

            BoundSlot.Set(_dragSource.BoundSlot.item, _dragSource.BoundSlot.quantity);
            _dragSource.BoundSlot.Set(tmpItem, tmpQty);
        }

        inv.NotifyInventoryChanged();
    }

    // ── Tooltip ───────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        if (BoundSlot != null && !BoundSlot.IsEmpty)
            TooltipUI.Show(BoundSlot.item);
    }

    public void OnPointerExit(PointerEventData e) => TooltipUI.Hide();

    // Hàm phụ trợ dọn dẹp ô
    public void ClearSlot()
    {
        if (BoundSlot != null) BoundSlot.Clear();
        Refresh();
    }
}