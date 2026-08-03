using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropItemZoneUI : MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private CanvasGroup _visualGroup;
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _hintText;
    [SerializeField] private ItemDropManager _dropManager;

    [Header("Colors")]
    [SerializeField]
    private Color _normalColor =
        new Color(0.15f, 0.15f, 0.15f, 0.72f);
    [SerializeField]
    private Color _hoverColor =
        new Color(0.60f, 0.16f, 0.12f, 0.90f);

    private bool _draggingItem;

    private void Awake()
    {
        if (_visualGroup == null)
            _visualGroup = GetComponent<CanvasGroup>();

        if (_background == null)
            _background = GetComponent<Image>();

        if (_dropManager == null)
            _dropManager = FindObjectOfType<ItemDropManager>();

        SetVisible(false);
        SetNormalVisual();
    }

    private void OnEnable()
    {
        ItemSlotUI.ItemDragStarted += HandleDragStarted;
        ItemSlotUI.ItemDragEnded += HandleDragEnded;
    }

    private void OnDisable()
    {
        ItemSlotUI.ItemDragStarted -= HandleDragStarted;
        ItemSlotUI.ItemDragEnded -= HandleDragEnded;
    }

    private void HandleDragStarted(ItemSlotUI source)
    {
        _draggingItem = source != null &&
                        source.BoundSlot != null &&
                        !source.BoundSlot.IsEmpty;

        SetVisible(_draggingItem);
        SetNormalVisual();
    }

    private void HandleDragEnded()
    {
        _draggingItem = false;
        SetNormalVisual();
        SetVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_draggingItem) return;

        if (_background != null)
            _background.color = _hoverColor;

        if (_titleText != null)
            _titleText.text = "DROP TO DISCARD";

        if (_hintText != null)
            _hintText.text = "The item will be dropped on the ground.";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_draggingItem)
            SetNormalVisual();
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemSlotUI source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<ItemSlotUI>()
            : null;

        if (source == null || source.BoundSlot == null || source.BoundSlot.IsEmpty)
            return;

        if (_dropManager == null)
            _dropManager = FindObjectOfType<ItemDropManager>();

        if (_dropManager == null)
        {
            Debug.LogError("[DropItemZoneUI] ItemDropManager was not found.", this);
            return;
        }

        _dropManager.TryDropSlot(source.BoundSlot);
        source.Refresh();
    }

    private void SetNormalVisual()
    {
        if (_background != null)
            _background.color = _normalColor;

        if (_titleText != null)
            _titleText.text = "DROP ITEM";

        if (_hintText != null)
            _hintText.text = "Drag an item here to discard it.";
    }

    private void SetVisible(bool visible)
    {
        if (_visualGroup == null) return;

        _visualGroup.alpha = visible ? 1f : 0f;
        _visualGroup.interactable = visible;

        // Chỉ chặn raycast khi vùng drop đang hiện.
        _visualGroup.blocksRaycasts = visible;
    }
}