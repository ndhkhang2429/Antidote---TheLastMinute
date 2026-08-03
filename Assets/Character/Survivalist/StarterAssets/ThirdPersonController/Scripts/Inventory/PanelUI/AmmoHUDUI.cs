using UnityEngine;
using TMPro;

public class AmmoHUDUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Object con được bật/tắt khi có hoặc không có súng.")]
    [SerializeField] private GameObject _ammoDisplay;

    [SerializeField] private TextMeshProUGUI _magazineText;
    [SerializeField] private TextMeshProUGUI _reserveText;
    [SerializeField] private TextMeshProUGUI _dividerText;

    [Header("Equipment")]
    [SerializeField]
    private PlayerEquipmentManager _equipmentManager;

    [Header("Colors")]
    [SerializeField]
    private Color _normalAmmoColor =
        Color.white;

    [SerializeField]
    private Color _lowAmmoColor =
        new Color(1f, 0.75f, 0.15f, 1f);

    [SerializeField]
    private Color _emptyAmmoColor =
        new Color(1f, 0.20f, 0.15f, 1f);

    [SerializeField]
    private Color _reserveAmmoColor =
        new Color(1f, 1f, 1f, 0.65f);

    [Range(0.05f, 0.5f)]
    [SerializeField]
    private float _lowAmmoThreshold = 0.25f;

    private WeaponInstance _activeWeapon;
    private bool _inventorySubscribed;
    private bool _equipmentSubscribed;

    private void Awake()
    {
        FindReferences();

        if (_dividerText != null)
        {
            _dividerText.text = "|";
        }

        HideAmmoDisplay();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        SynchronizeCurrentWeapon();
        Refresh();
    }

    private void Start()
    {
        /*
         * Kiểm tra thêm trong Start vì InventorySystem
         * có thể được tạo sau OnEnable.
         */
        FindReferences();
        SubscribeEvents();
        SynchronizeCurrentWeapon();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void FindReferences()
    {
        if (_equipmentManager == null)
        {
            _equipmentManager =
                FindObjectOfType<PlayerEquipmentManager>();
        }
    }

    private void SubscribeEvents()
    {
        if (!_inventorySubscribed &&
            InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged
                += Refresh;

            InventorySystem.Instance.OnActiveSlotChanged
                += HandleActiveSlotChanged;

            _inventorySubscribed = true;
        }

        if (!_equipmentSubscribed &&
            _equipmentManager != null)
        {
            _equipmentManager.OnWeaponEquipped
                += HandleWeaponEquipped;

            _equipmentSubscribed = true;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_inventorySubscribed &&
            InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged
                -= Refresh;

            InventorySystem.Instance.OnActiveSlotChanged
                -= HandleActiveSlotChanged;
        }

        if (_equipmentSubscribed &&
            _equipmentManager != null)
        {
            _equipmentManager.OnWeaponEquipped
                -= HandleWeaponEquipped;
        }

        _inventorySubscribed = false;
        _equipmentSubscribed = false;
    }

    private void SynchronizeCurrentWeapon()
    {
        if (_equipmentManager == null)
        {
            _activeWeapon = null;
            return;
        }

        _activeWeapon =
            _equipmentManager.CurrentWeaponInstance;
    }

    private void HandleWeaponEquipped(
        WeaponInstance weapon)
    {
        _activeWeapon = weapon;
        Refresh();
    }

    private void HandleActiveSlotChanged(
        int slotIndex)
    {
        /*
         * PlayerEquipmentManager sẽ gửi WeaponInstance
         * mới sau khi tạo xong prefab. Trước lúc đó có thể
         * ẩn HUD để tránh hiện đạn của súng cũ.
         */
        InventorySystem inventory =
            InventorySystem.Instance;

        if (inventory == null ||
            inventory.activeWeaponSlot < 0)
        {
            _activeWeapon = null;
            HideAmmoDisplay();
            return;
        }

        Refresh();
    }

    public void Refresh()
    {
        InventorySystem inventory =
            InventorySystem.Instance;

        if (inventory == null)
        {
            HideAmmoDisplay();
            return;
        }

        /*
         * Khi cầm Quest Item, melee hoặc tay không,
         * activeWeaponSlot sẽ không phải firearm hợp lệ.
         */
        int activeSlotIndex =
            inventory.activeWeaponSlot;

        if (activeSlotIndex < 0 ||
            inventory.weaponSlots == null ||
            activeSlotIndex >=
            inventory.weaponSlots.Length)
        {
            HideAmmoDisplay();
            return;
        }

        InventorySlot activeSlot =
            inventory.weaponSlots[
                activeSlotIndex
            ];

        if (activeSlot == null ||
            activeSlot.IsEmpty ||
            activeSlot.item == null)
        {
            HideAmmoDisplay();
            return;
        }

        if (activeSlot.item is not
            WeaponDataSO weaponData)
        {
            HideAmmoDisplay();
            return;
        }

        /*
         * Melee không có HUD đạn.
         */
        if (weaponData.combatType !=
            CombatType.Firearm)
        {
            HideAmmoDisplay();
            return;
        }

        activeSlot.InitializeAmmoIfNeeded();

        int magazineAmmo =
            Mathf.Clamp(
                activeSlot.currentAmmo,
                0,
                weaponData.magazineSize
            );

        int reserveAmmo =
            GetReserveAmmo(
                inventory,
                weaponData
            );

        ShowAmmoDisplay();

        if (_magazineText != null)
        {
            _magazineText.text =
                magazineAmmo.ToString();

            _magazineText.color =
                GetMagazineColor(
                    magazineAmmo,
                    weaponData.magazineSize
                );
        }

        if (_reserveText != null)
        {
            _reserveText.text =
                reserveAmmo.ToString();

            _reserveText.color =
                _reserveAmmoColor;
        }

        if (_dividerText != null)
        {
            _dividerText.text = "|";
            _dividerText.color =
                _reserveAmmoColor;
        }
    }

    private int GetReserveAmmo(
        InventorySystem inventory,
        WeaponDataSO weaponData)
    {
        if (inventory == null ||
            weaponData == null ||
            weaponData.compatibleAmmo == null)
        {
            return 0;
        }

        return inventory.CountItem(
            weaponData.compatibleAmmo
        );
    }

    private Color GetMagazineColor(
        int magazineAmmo,
        int magazineSize)
    {
        if (magazineAmmo <= 0)
        {
            return _emptyAmmoColor;
        }

        if (magazineSize <= 0)
        {
            return _normalAmmoColor;
        }

        float ammoRatio =
            (float)magazineAmmo /
            magazineSize;

        if (ammoRatio <= _lowAmmoThreshold)
        {
            return _lowAmmoColor;
        }

        return _normalAmmoColor;
    }

    private void ShowAmmoDisplay()
    {
        if (_ammoDisplay != null &&
            !_ammoDisplay.activeSelf)
        {
            _ammoDisplay.SetActive(true);
        }
    }

    private void HideAmmoDisplay()
    {
        if (_ammoDisplay != null &&
            _ammoDisplay.activeSelf)
        {
            _ammoDisplay.SetActive(false);
        }
    }
}