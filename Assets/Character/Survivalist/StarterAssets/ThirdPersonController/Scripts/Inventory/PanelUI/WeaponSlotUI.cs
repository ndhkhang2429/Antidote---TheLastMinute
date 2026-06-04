// ─────────────────────────────────────────────
// WeaponSlotUI — gắn trực tiếp lên từng ô vũ khí
// ─────────────────────────────────────────────
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WeaponSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Refs")]
    public Image iconImage;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI slotKeyText;   // "[1] Súng lục"
    public GameObject equippedBadge;   // "Đang dùng"
    public GameObject emptyHint;       // "Trống – kéo vào"

    static readonly string[] SlotLabels =
    {
        "[1] Súng lục / Shotgun",
        "[2] Súng trường",
        "[3] Cận chiến",
        "[4] Lựu đạn"
    };

    InventorySlot _slot;
    int _slotIndex;

    public void Bind(InventorySlot slot, int index)
    {
        _slot = slot;
        _slotIndex = index;

        if (slotKeyText != null)
            slotKeyText.text = SlotLabels[index];

        Refresh();
    }

    void Refresh()
    {
        bool empty = _slot == null || _slot.IsEmpty;

        if (iconImage != null) iconImage.enabled = !empty;
        if (emptyHint != null) emptyHint.SetActive(empty);
        if (equippedBadge != null) equippedBadge.SetActive(false);

        if (empty)
        {
            if (weaponNameText != null) weaponNameText.text = "";
            if (ammoText != null) ammoText.text = "";
            return;
        }

        if (iconImage != null) iconImage.sprite = _slot.item.icon;
        if (weaponNameText != null) weaponNameText.text = _slot.item.itemName;

        // Hiện ammo nếu là WeaponDataSO
        if (_slot.item is WeaponDataSO wd && ammoText != null)
            ammoText.text = wd.magazineSize > 0 ? $"{wd.magazineSize} / {wd.magazineSize}" : "∞";
        else if (ammoText != null)
            ammoText.text = _slotIndex == 3 ? $"x{_slot.quantity}" : "";
    }

    // Nhận lựu đạn kéo từ ItemGrid vào ô [4]
    public void OnDrop(PointerEventData e)
    {
        if (_slotIndex != 3) return; // chỉ ô lựu đạn mới nhận drop

        var sourceSlot = e.pointerDrag?.GetComponent<ItemSlotUI>();
        if (sourceSlot == null || sourceSlot.BoundSlot == null) return;

        bool ok = InventorySystem.Instance.MoveGrenadeToWeaponSlot(sourceSlot.BoundSlot);
        if (!ok) Debug.Log("Chỉ kéo lựu đạn vào ô [4]!");
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (_slot == null || _slot.IsEmpty) return;
        TooltipUI.Show(_slot.item);
    }

    public void OnPointerExit(PointerEventData e) => TooltipUI.Hide();
}