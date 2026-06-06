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
        public Image activeBorder;
        public Image iconImage;
        public TextMeshProUGUI keyLabel;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI ammoLabel;   // chỉ dùng cho slot 1-4
    }

    [Header("5 Slots (0=Pistol, 1=Rifle, 2=Melee, 3=Grenade, 4=Item)")]
    public SlotBarItem[] slots = new SlotBarItem[5];

    [Header("Colors")]
    public Color colorActive = new Color(1f, 0.78f, 0.2f, 1f);
    public Color colorInactive = new Color(1f, 1f, 1f, 0.15f);
    public Color colorBgActive = new Color(0f, 0f, 0f, 0.75f);
    public Color colorBgInactive = new Color(0f, 0f, 0f, 0.4f);
    public Color colorQuestItem = new Color(0.4f, 0.75f, 1f, 1f);   // xanh nhạt
    public Color colorEmpty = new Color(1f, 1f, 1f, 0.25f);

    static readonly string[] KeyLabels = { "1", "2", "3", "4", "5" };
    static readonly string[] SlotNames =
    {
        "Súng lục",
        "Súng trường",
        "Cận chiến",
        "Lựu đạn",
        "Item"
    };

    // ── Lifecycle ─────────────────────────────────────────
    void Start()
    {
        InitLabels();

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnActiveSlotChanged += OnActiveSlotChanged;
            InventorySystem.Instance.OnInventoryChanged += RefreshAll;
        }

        RefreshAll();
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance == null) return;
        InventorySystem.Instance.OnActiveSlotChanged -= OnActiveSlotChanged;
        InventorySystem.Instance.OnInventoryChanged -= RefreshAll;
    }

    void InitLabels()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].keyLabel != null)
                slots[i].keyLabel.text = KeyLabels[i];
        }
    }

    // ── Refresh ───────────────────────────────────────────
    void OnActiveSlotChanged(int activeIndex) => RefreshAll();

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

        // Background + border
        if (s.background != null)
            s.background.color = isActive ? colorBgActive : colorBgInactive;
        if (s.activeBorder != null)
            s.activeBorder.color = isActive ? colorActive : colorInactive;

        if (i < 4)
            RefreshWeaponSlot(s, i, isActive, inv);
        else
            RefreshItemSlot(s, isActive, inv);
    }

    void RefreshWeaponSlot(SlotBarItem s, int i, bool isActive, InventorySystem inv)
    {
        var weapSlot = inv.weaponSlots[i];

        if (weapSlot.IsEmpty)
        {
            // Trống
            if (s.iconImage != null) s.iconImage.enabled = false;
            if (s.nameLabel != null)
            {
                s.nameLabel.text = SlotNames[i];
                s.nameLabel.color = colorEmpty;
            }
            if (s.ammoLabel != null) s.ammoLabel.text = "";
            return;
        }

        // Có vũ khí
        if (s.iconImage != null)
        {
            s.iconImage.enabled = weapSlot.item.icon != null;
            if (weapSlot.item.icon != null)
            {
                s.iconImage.sprite = weapSlot.item.icon;
                s.iconImage.color = Color.white;
            }
        }

        if (s.nameLabel != null)
        {
            s.nameLabel.text = weapSlot.item.itemName;
            s.nameLabel.color = isActive ? colorActive : Color.white;
        }

        // Ammo — chỉ hiện khi là WeaponDataSO
        if (s.ammoLabel != null)
        {
            if (weapSlot.item is WeaponDataSO wd && wd.magazineSize > 0)
                s.ammoLabel.text = $"{wd.magazineSize}";
            else
                s.ammoLabel.text = "";
        }
    }

    void RefreshItemSlot(SlotBarItem s, bool isActive, InventorySystem inv)
    {
        bool empty = inv.heldItemSlot.IsEmpty;

        // Icon
        if (s.iconImage != null)
        {
            if (!empty && inv.heldItemSlot.item.icon != null)
            {
                s.iconImage.sprite = inv.heldItemSlot.item.icon;
                s.iconImage.enabled = true;
                s.iconImage.color = Color.white;
            }
            else
            {
                s.iconImage.enabled = false;
            }
        }

        // Name
        if (s.nameLabel != null)
        {
            s.nameLabel.text = empty ? "Item" : inv.heldItemSlot.item.itemName;
            s.nameLabel.color = isActive && !empty ? colorQuestItem : colorEmpty;
        }

        // Ammo/qty
        if (s.ammoLabel != null)
            s.ammoLabel.text = (!empty && inv.heldItemSlot.quantity > 1)
                               ? $"x{inv.heldItemSlot.quantity}" : "";
    }

    ItemDataSO FindFirstQuestItem(InventorySystem inv)
    {
        foreach (var slot in inv.GetItemSlots())
        {
            if (slot.IsEmpty) continue;
            if (slot.item.category == ItemCategory.QuestItem)
                return slot.item;
        }
        return null;
    }
}