using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Inventory weapon slot UI.
/// Slot 0 = Rifle, Slot 1 = Pistol/Shotgun, Slot 2 = Melee.
/// </summary>
public class WeaponSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI slotKeyText;

    [Header("Status")]
    [SerializeField] private GameObject equippedBadge;
    [SerializeField] private TextMeshProUGUI equippedBadgeText;
    [SerializeField] private GameObject emptyHint;
    [SerializeField] private TextMeshProUGUI emptyHintText;

    private static readonly string[] SlotLabels =
    {
        "[1] RIFLE",
        "[2] PISTOL / SHOTGUN",
        "[3] MELEE"
    };

    private InventorySlot _slot;
    private int _slotIndex = -1;
    private bool _subscribed;

    private void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    private void Start()
    {
        // InventorySystem may initialize after this UI object's OnEnable.
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        TooltipUI.Hide();
    }

    public void Bind(InventorySlot slot, int index)
    {
        if (index < 0 || index >= InventorySystem.WeaponSlotCount)
        {
            Debug.LogError($"[WeaponSlotUI] Invalid weapon slot index: {index}", this);
            return;
        }

        _slot = slot;
        _slotIndex = index;

        if (slotKeyText != null)
            slotKeyText.text = SlotLabels[index];

        if (equippedBadgeText != null)
            equippedBadgeText.text = "EQUIPPED";

        if (emptyHintText != null)
            emptyHintText.text = "EMPTY — DRAG A WEAPON HERE";

        TrySubscribe();
        Refresh();
    }

    private void TrySubscribe()
    {
        if (_subscribed || InventorySystem.Instance == null)
            return;

        InventorySystem.Instance.OnInventoryChanged += Refresh;
        InventorySystem.Instance.OnActiveSlotChanged += HandleActiveSlotChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
            InventorySystem.Instance.OnActiveSlotChanged -= HandleActiveSlotChanged;
        }

        _subscribed = false;
    }

    private void HandleActiveSlotChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        bool isEmpty = _slot == null || _slot.IsEmpty || _slot.item == null;
        bool isEquipped = !isEmpty &&
                          InventorySystem.Instance != null &&
                          InventorySystem.Instance.activeWeaponSlot == _slotIndex;

        if (emptyHint != null)
            emptyHint.SetActive(isEmpty);

        if (equippedBadge != null)
            equippedBadge.SetActive(isEquipped);

        if (isEmpty)
        {
            ClearDisplay();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = _slot.item.icon;
            iconImage.enabled = _slot.item.icon != null;
            iconImage.preserveAspect = true;
        }

        if (weaponNameText != null)
            weaponNameText.text = _slot.item.itemName;

        RefreshAmmoText();
    }

    private void ClearDisplay()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (weaponNameText != null)
            weaponNameText.text = string.Empty;

        if (ammoText != null)
            ammoText.text = string.Empty;
    }

    private void RefreshAmmoText()
    {
        if (ammoText == null)
            return;

        if (_slotIndex == InventorySystem.MeleeSlotIndex)
        {
            ammoText.text = string.Empty;
            return;
        }

        if (_slot.item is not WeaponDataSO weapon)
        {
            ammoText.text = string.Empty;
            return;
        }

        ammoText.text = weapon.magazineSize > 0
            ? $"{weapon.magazineSize} / {weapon.magazineSize}"
            : "NO AMMO";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_slot != null && !_slot.IsEmpty && _slot.item != null)
            TooltipUI.Show(_slot.item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Hide();
    }
}