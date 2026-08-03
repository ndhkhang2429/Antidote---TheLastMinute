using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI for the held Quest Item slot.
/// This is display slot 4 and activeSlot index 3.
/// </summary>
public class ItemSlot4UI :
    MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameLabel;

    [Header("Colors")]
    [SerializeField]
    private Color colorHasItem =
        new Color(0.4f, 0.75f, 1f, 1f);

    [SerializeField]
    private Color colorEmpty =
        new Color(1f, 1f, 1f, 0.25f);

    [Header("Drag Icon")]
    [SerializeField]
    private Vector2 dragIconSize =
        new Vector2(70f, 70f);

    private GameObject _dragIcon;
    private bool _subscribed;

    private void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    private void Start()
    {
        /*
         * InventorySystem có thể được khởi tạo sau OnEnable,
         * nên kiểm tra đăng ký lại trong Start.
         */
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        DestroyDragIcon();
        TooltipUI.Hide();

        ResetIconColor();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        DestroyDragIcon();
    }

    private void TrySubscribe()
    {
        if (_subscribed ||
            InventorySystem.Instance == null)
        {
            return;
        }

        InventorySystem.Instance.OnInventoryChanged
            += Refresh;

        InventorySystem.Instance.OnActiveSlotChanged
            += HandleActiveSlotChanged;

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged
                -= Refresh;

            InventorySystem.Instance.OnActiveSlotChanged
                -= HandleActiveSlotChanged;
        }

        _subscribed = false;
    }

    private void HandleActiveSlotChanged(int slotIndex)
    {
        Refresh();
    }

    public void Refresh()
    {
        InventorySystem inventory =
            InventorySystem.Instance;

        if (inventory == null)
            return;

        bool isEmpty =
            inventory.heldItemSlot == null ||
            inventory.heldItemSlot.IsEmpty ||
            inventory.heldItemSlot.item == null;

        bool isActive =
            inventory.activeSlot ==
            InventorySystem.ItemSlotIndex;

        if (isEmpty)
        {
            ShowEmptyState();
            return;
        }

        ShowItemState(inventory, isActive);
    }

    private void ShowItemState(
        InventorySystem inventory,
        bool isActive)
    {
        ItemDataSO item =
            inventory.heldItemSlot.item;

        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;
            iconImage.preserveAspect = true;

            if (_dragIcon == null)
            {
                iconImage.color =
                    isActive
                        ? Color.white
                        : new Color(1f, 1f, 1f, 0.65f);
            }
        }

        if (nameLabel != null)
        {
            int quantity =
                inventory.heldItemSlot.quantity;

            nameLabel.text =
                quantity > 1
                    ? $"{item.itemName} x{quantity}"
                    : item.itemName;

            nameLabel.color = colorHasItem;
        }
    }

    private void ShowEmptyState()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (nameLabel != null)
        {
            nameLabel.text = "EMPTY";
            nameLabel.color = colorEmpty;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySystem inventory =
            InventorySystem.Instance;

        if (inventory == null)
            return;

        ItemSlotUI source =
            eventData.pointerDrag != null
                ? eventData.pointerDrag
                    .GetComponent<ItemSlotUI>()
                : null;

        if (source == null ||
            source.BoundSlot == null ||
            source.BoundSlot.IsEmpty ||
            source.BoundSlot.item == null)
        {
            return;
        }

        ItemDataSO item =
            source.BoundSlot.item;

        if (item.category != ItemCategory.QuestItem)
        {
            NotificationUI.Instance?.ShowNotification(
                "Only quest items can be placed here."
            );

            return;
        }

        bool moved =
            inventory.MoveQuestItemToSlot4(
                source.BoundSlot
            );

        if (!moved)
        {
            NotificationUI.Instance?.ShowNotification(
                "Unable to assign this quest item."
            );
        }

        Refresh();
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        InventorySystem inventory =
            InventorySystem.Instance;

        if (inventory == null ||
            inventory.heldItemSlot == null ||
            inventory.heldItemSlot.IsEmpty ||
            inventory.heldItemSlot.item == null)
        {
            return;
        }

        TooltipUI.Show(
            inventory.heldItemSlot.item
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        TooltipUI.Hide();
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        InventorySystem inventory =
            InventorySystem.Instance;

        if (inventory == null ||
            inventory.heldItemSlot == null ||
            inventory.heldItemSlot.IsEmpty ||
            inventory.heldItemSlot.item == null)
        {
            return;
        }

        Canvas canvas =
            GetComponentInParent<Canvas>();

        if (canvas == null)
            return;

        DestroyDragIcon();

        _dragIcon =
            new GameObject(
                "DragGhost_ItemSlot4",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image)
            );

        _dragIcon.transform.SetParent(
            canvas.transform,
            false
        );

        _dragIcon.transform.SetAsLastSibling();

        Image dragImage =
            _dragIcon.GetComponent<Image>();

        dragImage.sprite =
            inventory.heldItemSlot.item.icon;

        dragImage.raycastTarget = false;
        dragImage.preserveAspect = true;

        dragImage.rectTransform.sizeDelta =
            dragIconSize;

        CanvasGroup canvasGroup =
            _dragIcon.GetComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.9f;

        MoveDragIcon(eventData);

        if (iconImage != null)
        {
            iconImage.color =
                new Color(1f, 1f, 1f, 0.35f);
        }
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        MoveDragIcon(eventData);
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        DestroyDragIcon();
        ResetIconColor();
        Refresh();
    }

    private void MoveDragIcon(
        PointerEventData eventData)
    {
        if (_dragIcon == null)
            return;

        _dragIcon.transform.position =
            eventData.position;
    }

    private void DestroyDragIcon()
    {
        if (_dragIcon == null)
            return;

        Destroy(_dragIcon);
        _dragIcon = null;
    }

    private void ResetIconColor()
    {
        if (iconImage != null)
            iconImage.color = Color.white;
    }
}