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

    [Header("5 Slots")]
    public SlotBarItem[] slots = new SlotBarItem[5];

    [Header("Layout")]
    public float slotHeight = 36f;
    public float slotSpacing = 4f;
    public float dividerGap = 8f; // khoảng cách thêm trước slot 5

    [Header("Colors")]
    public Color colorActiveBg = new Color(1f, 1f, 1f, 0.08f);
    public Color colorInactiveBg = new Color(1f, 1f, 1f, 0f);
    public Color colorActiveIcon = Color.white;
    public Color colorInactiveIcon = new Color(1f, 1f, 1f, 0.35f);
    public Color colorActiveText = new Color(1f, 0.85f, 0.25f, 1f);
    public Color colorInactiveText = new Color(1f, 1f, 1f, 0.25f);
    public Color colorActiveItem = new Color(0.4f, 0.75f, 1f, 1f);

    static readonly string[] KeyLabels = { "1", "2", "3", "4", "5" };
    static readonly string[] SlotNames =
    {
        "Súng trường",
        "Súng lục",
        "Cận chiến",
        "Lựu đạn",
        "Item"
    };

    void Start()
    {
        InitLabels();
        PositionSlots();

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnActiveSlotChanged += _ => RefreshAll();
            InventorySystem.Instance.OnInventoryChanged += RefreshAll;
        }
        RefreshAll();
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance == null) return;
        InventorySystem.Instance.OnActiveSlotChanged -= _ => RefreshAll();
        InventorySystem.Instance.OnInventoryChanged -= RefreshAll;
    }

    void InitLabels()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].keyLabel != null) slots[i].keyLabel.text = KeyLabels[i];
        }
    }

    // Tự đặt vị trí từng slot bằng code
    void PositionSlots()
    {
        float y = 0f;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].root == null) continue;

            // Thêm khoảng cách trước slot 5
            if (i == 4) y -= dividerGap;

            var rt = slots[i].root.GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0, -(y + slotHeight));
            rt.offsetMax = new Vector2(0, -y);

            y += slotHeight + slotSpacing;
        }

        // Set chiều cao SlotBarUI = tổng slot + divider
        var selfRt = GetComponent<RectTransform>();
        selfRt.sizeDelta = new Vector2(200, y + dividerGap);
    }

    public void RefreshAll()
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;
        int active = inv.activeSlot;
        for (int i = 0; i < slots.Length; i++)
            RefreshSlot(i, active, inv);
    }

    void RefreshSlot(int i, int activeIndex, InventorySystem inv)
    {
        var s = slots[i];
        if (s == null || s.root == null) return;
        bool isActive = (i == activeIndex);

        if (s.background != null)
            s.background.color = isActive ? colorActiveBg : colorInactiveBg;

        if (s.iconImage != null)
            s.iconImage.color = isActive ? colorActiveIcon : colorInactiveIcon;

        if (s.keyLabel != null)
            s.keyLabel.color = isActive
                ? new Color(1f, 1f, 1f, 0.7f)
                : new Color(1f, 1f, 1f, 0.25f);

        if (i < 4) RefreshWeaponSlot(s, i, isActive, inv);
        else RefreshItemSlot(s, isActive, inv);
    }

    void RefreshWeaponSlot(SlotBarItem s, int i, bool isActive, InventorySystem inv)
    {
        var weapSlot = inv.weaponSlots[i];

        if (s.iconImage != null)
        {
            bool hasIcon = !weapSlot.IsEmpty && weapSlot.item.icon != null;
            s.iconImage.enabled = hasIcon;
            if (hasIcon) s.iconImage.sprite = weapSlot.item.icon;
        }
    }

    void RefreshItemSlot(SlotBarItem s, bool isActive, InventorySystem inv)
    {
        bool empty = inv.heldItemSlot.IsEmpty;

        if (s.iconImage != null)
        {
            bool hasIcon = !empty && inv.heldItemSlot.item.icon != null;
            s.iconImage.enabled = hasIcon;
            if (hasIcon) s.iconImage.sprite = inv.heldItemSlot.item.icon;
        }
    }
}