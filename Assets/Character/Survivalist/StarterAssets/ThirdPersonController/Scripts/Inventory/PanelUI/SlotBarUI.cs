using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotBarUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotBarItem
    {
        public GameObject root;
        public Image background;
        public Image iconImage;
        public TextMeshProUGUI keyLabel;
    }

    [Header("4 Slots: Rifle, Pistol, Melee, Item")]
    public SlotBarItem[] slots = new SlotBarItem[4];

    [Header("Panel Layout")]
    [SerializeField] private float panelWidth = 230f;
    [SerializeField] private float slotHeight = 58f;
    [SerializeField] private float slotSpacing = 5f;

    [Header("Key Layout")]
    [SerializeField] private float keyLeftPadding = 8f;
    [SerializeField] private float keyWidth = 24f;

    [Header("Icon Layout")]
    [Tooltip("Chiều rộng tối đa của vùng icon")]
    [SerializeField] private float iconAreaWidth = 170f;

    [Tooltip("Chiều cao tối đa của vùng icon")]
    [SerializeField] private float iconAreaHeight = 46f;

    [Tooltip("Khoảng cách icon với cạnh phải")]
    [SerializeField] private float iconRightPadding = 10f;

    [Range(0.1f, 1f)]
    [Tooltip("Mức độ icon lấp đầy vùng chứa")]
    [SerializeField] private float iconFill = 0.92f;

    [Header("Colors")]
    [SerializeField]
    private Color colorActiveBg =
        new Color(1f, 1f, 1f, 0.10f);

    [SerializeField]
    private Color colorInactiveBg =
        new Color(0f, 0f, 0f, 0f);

    [SerializeField]
    private Color colorActiveIcon =
        new Color(1f, 1f, 1f, 1f);

    [SerializeField]
    private Color colorInactiveIcon =
        new Color(1f, 1f, 1f, 0.30f);

    [SerializeField]
    private Color colorActiveText =
        new Color(1f, 0.85f, 0.25f, 1f);

    [SerializeField]
    private Color colorInactiveText =
        new Color(1f, 1f, 1f, 0.28f);

    private static readonly string[] KeyLabels =
    {
        "1", "2", "3", "4"
    };

    private void Start()
    {
        InitLabels();
        ApplyLayout();

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnActiveSlotChanged
                += HandleActiveSlotChanged;

            InventorySystem.Instance.OnInventoryChanged
                += RefreshAll;
        }

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (InventorySystem.Instance == null)
            return;

        InventorySystem.Instance.OnActiveSlotChanged
            -= HandleActiveSlotChanged;

        InventorySystem.Instance.OnInventoryChanged
            -= RefreshAll;
    }

    private void HandleActiveSlotChanged(int slotIndex)
    {
        RefreshAll();
    }

    private void InitLabels()
    {
        if (slots == null)
            return;

        int count = Mathf.Min(slots.Length, KeyLabels.Length);

        for (int i = 0; i < count; i++)
        {
            SlotBarItem slot = slots[i];

            if (slot != null && slot.keyLabel != null)
                slot.keyLabel.text = KeyLabels[i];
        }
    }

    private void ApplyLayout()
    {
        RectTransform panelRect = GetComponent<RectTransform>();

        if (panelRect == null || slots == null)
            return;

        /*
         * SlotBarUI bám vào giữa cạnh phải màn hình.
         * Pos X và Pos Y vẫn có thể chỉnh trong Inspector.
         */
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);

        float currentY = 0f;

        for (int i = 0; i < slots.Length; i++)
        {
            SlotBarItem slot = slots[i];

            if (slot == null || slot.root == null)
                continue;

            RectTransform slotRect =
                slot.root.GetComponent<RectTransform>();

            if (slotRect == null)
                continue;

            // Slot kéo giãn theo chiều ngang của SlotBarUI.
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(1f, 1f);
            slotRect.pivot = new Vector2(0.5f, 1f);

            slotRect.offsetMin =
                new Vector2(0f, -(currentY + slotHeight));

            slotRect.offsetMax =
                new Vector2(0f, -currentY);

            LayoutKey(slot);
            LayoutIconArea(slot);

            currentY += slotHeight;

            if (i < slots.Length - 1)
                currentY += slotSpacing;
        }

        panelRect.sizeDelta =
            new Vector2(panelWidth, currentY);
    }

    private void LayoutKey(SlotBarItem slot)
    {
        if (slot.keyLabel == null)
            return;

        RectTransform keyRect = slot.keyLabel.rectTransform;

        keyRect.anchorMin = new Vector2(0f, 0f);
        keyRect.anchorMax = new Vector2(0f, 1f);
        keyRect.pivot = new Vector2(0f, 0.5f);

        keyRect.offsetMin =
            new Vector2(keyLeftPadding, 0f);

        keyRect.offsetMax =
            new Vector2(keyLeftPadding + keyWidth, 0f);

        slot.keyLabel.alignment = TextAlignmentOptions.Center;
        slot.keyLabel.enableAutoSizing = true;
        slot.keyLabel.fontSizeMin = 14f;
        slot.keyLabel.fontSizeMax = 19f;
        slot.keyLabel.raycastTarget = false;
    }

    private void LayoutIconArea(SlotBarItem slot)
    {
        if (slot.iconImage == null)
            return;

        RectTransform iconRect = slot.iconImage.rectTransform;

        /*
         * Icon sử dụng anchor Middle Right.
         * Tất cả icon dùng chung một vùng hiển thị.
         */
        iconRect.anchorMin = new Vector2(1f, 0.5f);
        iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);

        iconRect.anchoredPosition =
            new Vector2(-iconRightPadding, 0f);

        iconRect.sizeDelta =
            new Vector2(iconAreaWidth, iconAreaHeight);

        slot.iconImage.type = Image.Type.Simple;
        slot.iconImage.preserveAspect = true;
        slot.iconImage.raycastTarget = false;
    }

    public void RefreshAll()
    {
        InventorySystem inventory = InventorySystem.Instance;

        if (inventory == null || slots == null)
            return;

        int activeIndex = inventory.activeSlot;

        for (int i = 0; i < slots.Length; i++)
        {
            RefreshSlot(
                i,
                activeIndex,
                inventory
            );
        }
    }

    private void RefreshSlot(
        int index,
        int activeIndex,
        InventorySystem inventory)
    {
        if (index < 0 || index >= slots.Length)
            return;

        SlotBarItem slot = slots[index];

        if (slot == null || slot.root == null)
            return;

        bool isActive = index == activeIndex;

        if (slot.background != null)
        {
            slot.background.color =
                isActive
                    ? colorActiveBg
                    : colorInactiveBg;

            slot.background.raycastTarget = false;
        }

        if (slot.keyLabel != null)
        {
            slot.keyLabel.color =
                isActive
                    ? colorActiveText
                    : colorInactiveText;
        }

        if (index < 3)
        {
            RefreshWeaponSlot(
                slot,
                index,
                isActive,
                inventory
            );
        }
        else
        {
            RefreshItemSlot(
                slot,
                isActive,
                inventory
            );
        }
    }

    private void RefreshWeaponSlot(
        SlotBarItem slot,
        int index,
        bool isActive,
        InventorySystem inventory)
    {
        if (inventory.weaponSlots == null ||
            index < 0 ||
            index >= inventory.weaponSlots.Length)
        {
            ApplyIcon(slot, null, isActive);
            return;
        }

        var weaponSlot = inventory.weaponSlots[index];

        bool hasIcon =
            !weaponSlot.IsEmpty &&
            weaponSlot.item != null &&
            weaponSlot.item.icon != null;

        ApplyIcon(
            slot,
            hasIcon ? weaponSlot.item.icon : null,
            isActive
        );
    }

    private void RefreshItemSlot(
        SlotBarItem slot,
        bool isActive,
        InventorySystem inventory)
    {
        bool hasIcon =
            !inventory.heldItemSlot.IsEmpty &&
            inventory.heldItemSlot.item != null &&
            inventory.heldItemSlot.item.icon != null;

        ApplyIcon(
            slot,
            hasIcon
                ? inventory.heldItemSlot.item.icon
                : null,
            isActive
        );
    }

    private void ApplyIcon(
        SlotBarItem slot,
        Sprite sprite,
        bool isActive)
    {
        if (slot.iconImage == null)
            return;

        bool hasIcon = sprite != null;

        slot.iconImage.enabled = hasIcon;

        if (!hasIcon)
            return;

        slot.iconImage.sprite = sprite;
        slot.iconImage.color =
            isActive
                ? colorActiveIcon
                : colorInactiveIcon;

        FitIconToArea(slot.iconImage, sprite);
    }

    private void FitIconToArea(
        Image image,
        Sprite sprite)
    {
        if (image == null || sprite == null)
            return;

        RectTransform iconRect = image.rectTransform;

        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;

        if (spriteWidth <= 0f || spriteHeight <= 0f)
            return;

        float spriteAspect = spriteWidth / spriteHeight;

        float maxWidth = iconAreaWidth * iconFill;
        float maxHeight = iconAreaHeight * iconFill;

        float finalWidth = maxWidth;
        float finalHeight = finalWidth / spriteAspect;

        /*
         * Nếu chiều cao vượt vùng chứa thì giới hạn theo chiều cao.
         * Sprite vẫn giữ đúng tỷ lệ và không bị bóp méo.
         */
        if (finalHeight > maxHeight)
        {
            finalHeight = maxHeight;
            finalWidth = finalHeight * spriteAspect;
        }

        iconRect.sizeDelta =
            new Vector2(finalWidth, finalHeight);
    }
}