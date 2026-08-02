using System;
using UnityEngine;

public class PlayerEquipmentManager : MonoBehaviour
{
    [Header("FPS Weapon Setup")]
    [Tooltip("Assign FPSWeaponSocket under WeaponRecoilRoot.")]
    [SerializeField] private Transform _fpsWeaponSocket;

    [Tooltip("Layer used for first-person arms and weapons.")]
    [SerializeField] private string _fpsLayerName = "FPS_Arms";

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs;

    /// <summary>
    /// Sends the newly equipped WeaponInstance.
    /// Sends null when the weapon is unequipped or
    /// the current item is not a weapon.
    /// </summary>
    public event Action<WeaponInstance> OnWeaponEquipped;

    private GameObject _currentEquippedModel;
    private WeaponInstance _currentWeaponInstance;
    private Renderer[] _currentWeaponRenderers;

    private bool _weaponVisualVisible = true;
    private bool _inventorySubscribed;

    public GameObject CurrentEquippedModel =>
        _currentEquippedModel;

    public WeaponInstance CurrentWeaponInstance =>
        _currentWeaponInstance;

    public bool HasEquippedItem =>
        _currentEquippedModel != null;

    public bool HasEquippedWeapon =>
        _currentWeaponInstance != null;

    public bool IsWeaponVisualVisible =>
        _weaponVisualVisible;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        TrySubscribeInventoryEvents();
    }

    private void Start()
    {
        TrySubscribeInventoryEvents();

        /*
         * Đồng bộ item hiện đang được chọn trong trường hợp
         * InventorySystem đã phát event trước khi manager đăng ký.
         */
        InventorySystem inventory =
            InventorySystem.Instance;

        if (inventory != null)
        {
            HandleItemChange(
                inventory.GetHeldItem()
            );
        }
    }

    private void OnDisable()
    {
        SaveCurrentWeaponAmmo();
        UnsubscribeInventoryEvents();
    }

    private void OnDestroy()
    {
        SaveCurrentWeaponAmmo();
        UnsubscribeInventoryEvents();
    }

    private void ValidateReferences()
    {
        if (_fpsWeaponSocket == null)
        {
            Debug.LogError(
                "[PlayerEquipmentManager] FPSWeaponSocket has not been assigned.",
                this
            );
        }

        if (string.IsNullOrWhiteSpace(_fpsLayerName))
        {
            Debug.LogWarning(
                "[PlayerEquipmentManager] FPS layer name is empty.",
                this
            );
        }
        else if (LayerMask.NameToLayer(_fpsLayerName) < 0)
        {
            Debug.LogWarning(
                $"[PlayerEquipmentManager] Layer '{_fpsLayerName}' was not found.",
                this
            );
        }
    }

    private void TrySubscribeInventoryEvents()
    {
        if (_inventorySubscribed ||
            InventorySystem.Instance == null)
        {
            return;
        }

        InventorySystem.Instance.OnHeldItemChanged
            += HandleItemChange;

        _inventorySubscribed = true;
    }

    private void UnsubscribeInventoryEvents()
    {
        if (!_inventorySubscribed)
            return;

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnHeldItemChanged
                -= HandleItemChange;
        }

        _inventorySubscribed = false;
    }

    private void HandleItemChange(
        ItemDataSO heldItem)
    {
        /*
         * UnequipCurrentItem sẽ lưu đạn trước khi
         * prefab súng cũ bị Destroy.
         */
        UnequipCurrentItem(false);

        if (heldItem == null ||
            heldItem.equipPrefab == null)
        {
            NotifyWeaponChanged(null);
            return;
        }

        if (_fpsWeaponSocket == null)
        {
            Debug.LogError(
                "[PlayerEquipmentManager] Cannot equip item because FPSWeaponSocket is missing.",
                this
            );

            NotifyWeaponChanged(null);
            return;
        }

        SpawnEquippedItem(heldItem);
    }

    private void SpawnEquippedItem(
        ItemDataSO heldItem)
    {
        Vector3 localPosition =
            Vector3.zero;

        Vector3 localRotation =
            Vector3.zero;

        if (heldItem is WeaponDataSO weaponData)
        {
            localPosition =
                weaponData.gripOffset;

            localRotation =
                weaponData.gripRotation;
        }

        _currentEquippedModel = Instantiate(
            heldItem.equipPrefab,
            _fpsWeaponSocket
        );

        _currentEquippedModel.name =
            $"{heldItem.equipPrefab.name}_FPS";

        Transform equippedTransform =
            _currentEquippedModel.transform;

        equippedTransform.localPosition =
            localPosition;

        equippedTransform.localRotation =
            Quaternion.Euler(localRotation);

        equippedTransform.localScale =
            Vector3.one;

        ApplyFPSLayer(_currentEquippedModel);

        _currentWeaponRenderers =
            _currentEquippedModel
                .GetComponentsInChildren<Renderer>(true);

        ApplyWeaponVisualState();

        IEquippable equippable =
            _currentEquippedModel
                .GetComponent<IEquippable>();

        if (equippable == null)
        {
            equippable =
                _currentEquippedModel
                    .GetComponentInChildren<IEquippable>(true);
        }

        equippable?.OnEquip();

        /*
         * Dùng GetComponentInChildren để vẫn tìm thấy
         * WeaponInstance nếu nó không nằm ở root prefab.
         */
        _currentWeaponInstance =
            _currentEquippedModel
                .GetComponentInChildren<WeaponInstance>(true);

        BindCurrentWeaponToInventorySlot(
            heldItem
        );

        if (_showDebugLogs)
        {
            string itemType =
                _currentWeaponInstance != null
                    ? "Weapon"
                    : "Equippable item";

            Debug.Log(
                $"[PlayerEquipmentManager] Equipped {itemType}: {heldItem.name}",
                _currentEquippedModel
            );

            if (_currentWeaponInstance != null &&
                _currentWeaponInstance.weaponData != null)
            {
                Debug.Log(
                    $"[PlayerEquipmentManager] Magazine: " +
                    $"{_currentWeaponInstance.currentAmmo}/" +
                    $"{_currentWeaponInstance.weaponData.magazineSize}",
                    _currentWeaponInstance
                );
            }
        }

        /*
         * Chỉ phát event sau khi WeaponInstance đã được
         * bind vào InventorySlot và tải số đạn chính xác.
         */
        NotifyWeaponChanged(
            _currentWeaponInstance
        );
    }

    private void BindCurrentWeaponToInventorySlot(
        ItemDataSO equippedItem)
    {
        if (_currentWeaponInstance == null)
            return;

        InventorySystem inventory =
            InventorySystem.Instance;

        if (inventory == null ||
            inventory.weaponSlots == null)
        {
            Debug.LogWarning(
                "[PlayerEquipmentManager] Cannot bind weapon because InventorySystem is unavailable.",
                this
            );

            return;
        }

        int slotIndex =
            inventory.activeWeaponSlot;

        if (slotIndex < 0 ||
            slotIndex >= inventory.weaponSlots.Length)
        {
            /*
             * Item ở slot 4 có thể không phải vũ khí,
             * nên trường hợp activeWeaponSlot = -1 là hợp lệ.
             */
            return;
        }

        InventorySlot activeSlot =
            inventory.weaponSlots[slotIndex];

        if (activeSlot == null ||
            activeSlot.IsEmpty)
        {
            Debug.LogWarning(
                $"[PlayerEquipmentManager] Weapon slot {slotIndex} is empty.",
                this
            );

            return;
        }

        if (activeSlot.item != equippedItem)
        {
            Debug.LogWarning(
                "[PlayerEquipmentManager] Equipped item does not match the active weapon slot.",
                this
            );

            return;
        }

        _currentWeaponInstance.BindToSlot(
            activeSlot
        );
    }

    private void SaveCurrentWeaponAmmo()
    {
        if (_currentWeaponInstance == null)
            return;

        _currentWeaponInstance.SaveAmmoToSlot();
    }

    private void UnequipCurrentItem(
        bool notify)
    {
        /*
         * Phải lưu trước khi gọi OnUnequip và Destroy.
         */
        SaveCurrentWeaponAmmo();

        if (_currentEquippedModel != null)
        {
            WeaponAudioController weaponAudio =
                _currentEquippedModel
                    .GetComponent<WeaponAudioController>();

            if (weaponAudio == null)
            {
                weaponAudio =
                    _currentEquippedModel
                        .GetComponentInChildren
                            <WeaponAudioController>(true);
            }

            weaponAudio?.PlayHolsterDetached();

            IEquippable equippable =
                _currentEquippedModel
                    .GetComponent<IEquippable>();

            if (equippable == null)
            {
                equippable =
                    _currentEquippedModel
                        .GetComponentInChildren
                            <IEquippable>(true);
            }

            equippable?.OnUnequip();

            Destroy(_currentEquippedModel);
        }

        _currentEquippedModel = null;
        _currentWeaponInstance = null;
        _currentWeaponRenderers = null;

        if (notify)
        {
            NotifyWeaponChanged(null);
        }
    }

    public void UnequipCurrentItem()
    {
        UnequipCurrentItem(true);
    }

    /// <summary>
    /// Only hides the item renderers.
    /// The object, WeaponInstance and ammunition state remain active.
    /// </summary>
    public void SetWeaponVisualVisible(
        bool visible)
    {
        _weaponVisualVisible = visible;
        ApplyWeaponVisualState();

        if (_showDebugLogs)
        {
            Debug.Log(
                $"[PlayerEquipmentManager] Weapon visual: {visible}",
                this
            );
        }
    }

    public void HideWeaponVisual()
    {
        SetWeaponVisualVisible(false);
    }

    public void ShowWeaponVisual()
    {
        SetWeaponVisualVisible(true);
    }

    private void ApplyWeaponVisualState()
    {
        if (_currentWeaponRenderers == null)
            return;

        foreach (Renderer itemRenderer
                 in _currentWeaponRenderers)
        {
            if (itemRenderer != null)
            {
                itemRenderer.enabled =
                    _weaponVisualVisible;
            }
        }
    }

    private void ApplyFPSLayer(
        GameObject target)
    {
        int fpsLayer =
            LayerMask.NameToLayer(_fpsLayerName);

        if (fpsLayer < 0)
        {
            Debug.LogWarning(
                $"[PlayerEquipmentManager] Layer '{_fpsLayerName}' was not found. Prefab layers were preserved.",
                this
            );

            return;
        }

        SetLayerRecursively(
            target,
            fpsLayer
        );
    }

    private void NotifyWeaponChanged(
        WeaponInstance weaponInstance)
    {
        OnWeaponEquipped?.Invoke(
            weaponInstance
        );
    }

    private static void SetLayerRecursively(
        GameObject target,
        int layer)
    {
        if (target == null)
            return;

        target.layer = layer;

        foreach (Transform child
                 in target.transform)
        {
            SetLayerRecursively(
                child.gameObject,
                layer
            );
        }
    }
}