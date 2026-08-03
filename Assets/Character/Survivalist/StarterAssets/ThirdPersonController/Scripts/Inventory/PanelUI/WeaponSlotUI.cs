using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Displays a weapon slot inside the inventory.
///
/// Slot 0 = Rifle
/// Slot 1 = Pistol
/// Slot 2 = Melee
///
/// Firearm ammo format:
/// Magazine ammo / Reserve ammo
/// Example: 27 / 90
/// </summary>
public class WeaponSlotUI :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
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

    [Header("Colors")]
    [SerializeField]
    private Color normalIconColor =
        new Color(1f, 1f, 1f, 0.70f);

    [SerializeField]
    private Color equippedIconColor =
        Color.white;

    [SerializeField]
    private Color normalTextColor =
        new Color(1f, 1f, 1f, 0.70f);

    [SerializeField]
    private Color equippedTextColor =
        Color.white;

    [SerializeField]
    private Color emptyTextColor =
        new Color(1f, 1f, 1f, 0.30f);

    [Header("Ammo Colors")]
    [SerializeField]
    private Color ammoNormalColor =
        Color.white;

    [SerializeField]
    private Color ammoLowColor =
        new Color(1f, 0.75f, 0.20f, 1f);

    [SerializeField]
    private Color ammoEmptyColor =
        new Color(1f, 0.25f, 0.20f, 1f);

    [Range(0.05f, 0.5f)]
    [SerializeField]
    private float lowAmmoThreshold = 0.25f;

    private static readonly string[] SlotLabels =
    {
        "[1] RIFLE",
        "[2] PISTOL",
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
        /*
         * InventorySystem có thể được khởi tạo sau OnEnable,
         * vì vậy kiểm tra đăng ký thêm lần nữa trong Start.
         */
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        TooltipUI.Hide();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void Bind(
        InventorySlot slot,
        int index)
    {
        if (index < 0 ||
            index >= SlotLabels.Length)
        {
            Debug.LogError(
                $"[WeaponSlotUI] Invalid weapon slot index: {index}",
                this
            );

            return;
        }

        _slot = slot;
        _slotIndex = index;

        if (slotKeyText != null)
        {
            slotKeyText.text =
                SlotLabels[index];
        }

        if (equippedBadgeText != null)
        {
            equippedBadgeText.text =
                "EQUIPPED";
        }

        if (emptyHintText != null)
        {
            emptyHintText.text =
                "EMPTY — DRAG A WEAPON HERE";
        }

        TrySubscribe();
        Refresh();
    }

    private void TrySubscribe()
    {
        if (_subscribed ||
            InventorySystem.Instance == null)
        {
            return;
        }

        /*
         * Event này được gọi sau mỗi phát bắn,
         * reload, nhặt đồ hoặc thay đổi inventory.
         */
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

    private void HandleActiveSlotChanged(
        int slotIndex)
    {
        Refresh();
    }

    public void Refresh()
    {
        bool isEmpty =
            _slot == null ||
            _slot.IsEmpty ||
            _slot.item == null;

        bool isEquipped =
            !isEmpty &&
            InventorySystem.Instance != null &&
            InventorySystem.Instance.activeWeaponSlot ==
            _slotIndex;

        RefreshStatus(
            isEmpty,
            isEquipped
        );

        if (isEmpty)
        {
            ShowEmptyState();
            return;
        }

        ShowWeaponState(isEquipped);
    }

    private void RefreshStatus(
        bool isEmpty,
        bool isEquipped)
    {
        if (emptyHint != null)
        {
            emptyHint.SetActive(isEmpty);
        }

        if (equippedBadge != null)
        {
            equippedBadge.SetActive(
                !isEmpty && isEquipped
            );
        }
    }

    private void ShowEmptyState()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (weaponNameText != null)
        {
            weaponNameText.text =
                string.Empty;

            weaponNameText.color =
                emptyTextColor;
        }

        if (ammoText != null)
        {
            ammoText.text =
                string.Empty;
        }
    }

    private void ShowWeaponState(
        bool isEquipped)
    {
        ItemDataSO item =
            _slot.item;

        if (iconImage != null)
        {
            iconImage.sprite =
                item.icon;

            iconImage.enabled =
                item.icon != null;

            iconImage.preserveAspect =
                true;

            iconImage.color =
                isEquipped
                    ? equippedIconColor
                    : normalIconColor;
        }

        if (weaponNameText != null)
        {
            weaponNameText.text =
                item.itemName;

            weaponNameText.color =
                isEquipped
                    ? equippedTextColor
                    : normalTextColor;
        }

        RefreshAmmoText();
    }

    private void RefreshAmmoText()
    {
        if (ammoText == null)
            return;

        if (_slot == null ||
            _slot.IsEmpty ||
            _slot.item == null)
        {
            ammoText.text =
                string.Empty;

            return;
        }

        /*
         * Vũ khí cận chiến không hiển thị đạn.
         */
        if (_slotIndex ==
            InventorySystem.MeleeSlotIndex)
        {
            ammoText.text =
                string.Empty;

            return;
        }

        if (_slot.item is not WeaponDataSO weapon ||
            weapon.combatType !=
            CombatType.Firearm)
        {
            ammoText.text =
                string.Empty;

            return;
        }

        /*
         * Nếu đây là khẩu súng vừa được nhặt,
         * khởi tạo số đạn trong băng một lần.
         */
        _slot.InitializeAmmoIfNeeded();

        int magazineAmmo =
            Mathf.Clamp(
                _slot.currentAmmo,
                0,
                weapon.magazineSize
            );

        int reserveAmmo =
            GetReserveAmmo(weapon);

        /*
         * Kiểu PUBG:
         * Đạn trong băng / Đạn dự trữ
         */
        ammoText.text =
            $"{magazineAmmo} / {reserveAmmo}";

        UpdateAmmoColor(
            magazineAmmo,
            weapon.magazineSize
        );
    }

    private int GetReserveAmmo(
        WeaponDataSO weapon)
    {
        InventorySystem inventory =
            InventorySystem.Instance;

        if (inventory == null ||
            weapon == null ||
            weapon.compatibleAmmo == null)
        {
            return 0;
        }

        return inventory.CountItem(
            weapon.compatibleAmmo
        );
    }

    private void UpdateAmmoColor(
        int magazineAmmo,
        int magazineSize)
    {
        if (ammoText == null)
            return;

        if (magazineAmmo <= 0)
        {
            ammoText.color =
                ammoEmptyColor;

            return;
        }

        if (magazineSize <= 0)
        {
            ammoText.color =
                ammoNormalColor;

            return;
        }

        float ammoRatio =
            (float)magazineAmmo /
            magazineSize;

        ammoText.color =
            ammoRatio <= lowAmmoThreshold
                ? ammoLowColor
                : ammoNormalColor;
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (_slot == null ||
            _slot.IsEmpty ||
            _slot.item == null)
        {
            return;
        }

        TooltipUI.Show(
            _slot.item
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        TooltipUI.Hide();
    }
}