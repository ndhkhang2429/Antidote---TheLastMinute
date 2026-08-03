using System;
using UnityEngine;

public class PlayerEquipmentManager : MonoBehaviour
{
    [Header("FPS Weapon Setup")]
    [Tooltip("Socket dành cho súng và item thông thường, nằm dưới WeaponRecoilRoot.")]
    [SerializeField] private Transform _fpsWeaponSocket;

    [Tooltip("Socket dành cho melee, phải là con của bone RightHand trong FPS_HANDS.")]
    [SerializeField] private Transform _fpsMeleeSocket;

    [Tooltip("Layer dùng cho tay và vũ khí góc nhìn thứ nhất.")]
    [SerializeField] private string _fpsLayerName = "FPS_Arms";

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs;

    /// <summary>
    /// Gửi WeaponInstance vừa được trang bị.
    /// Gửi null khi tháo vũ khí hoặc item không có WeaponInstance.
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
                "[PlayerEquipmentManager] FPSWeaponSocket chưa được gán.",
                this
            );
        }

        if (_fpsMeleeSocket == null)
        {
            Debug.LogWarning(
                "[PlayerEquipmentManager] FPSMeleeSocket chưa được gán. Vũ khí melee sẽ không thể trang bị.",
                this
            );
        }

        if (string.IsNullOrWhiteSpace(_fpsLayerName))
        {
            Debug.LogWarning(
                "[PlayerEquipmentManager] Tên FPS Layer đang trống.",
                this
            );
        }
        else if (LayerMask.NameToLayer(_fpsLayerName) < 0)
        {
            Debug.LogWarning(
                $"[PlayerEquipmentManager] Không tìm thấy Layer '{_fpsLayerName}'.",
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
         * Lưu đạn và hủy model cũ trước khi tạo model mới.
         */
        UnequipCurrentItem(false);

        if (heldItem == null ||
            heldItem.equipPrefab == null)
        {
            NotifyWeaponChanged(null);
            return;
        }

        Transform targetSocket =
            GetSocketForItem(heldItem);

        if (targetSocket == null)
        {
            Debug.LogError(
                $"[PlayerEquipmentManager] Không tìm thấy socket phù hợp cho item '{heldItem.name}'.",
                this
            );

            NotifyWeaponChanged(null);
            return;
        }

        SpawnEquippedItem(
            heldItem,
            targetSocket
        );
    }

    /// <summary>
    /// Melee được gắn vào socket bàn tay.
    /// Các item còn lại được gắn vào FPSWeaponSocket.
    /// </summary>
    private Transform GetSocketForItem(
        ItemDataSO heldItem)
    {
        if (heldItem is WeaponDataSO weaponData &&
            weaponData.weaponSlotType == WeaponSlotType.Melee)
        {
            return _fpsMeleeSocket;
        }

        return _fpsWeaponSocket;
    }

    private void SpawnEquippedItem(
        ItemDataSO heldItem,
        Transform targetSocket)
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

        /*
         * false: dùng tọa độ local của socket mới,
         * không giữ nguyên vị trí ngoài World.
         */
        _currentEquippedModel = Instantiate(
            heldItem.equipPrefab,
            targetSocket,
            false
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

        ApplyFPSLayer(
            _currentEquippedModel
        );

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
         * Dùng GetComponentInChildren để tìm WeaponInstance
         * ngay cả khi component không nằm ở root prefab.
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
                $"[PlayerEquipmentManager] Equipped {itemType}: " +
                $"{heldItem.name} vào socket {targetSocket.name}",
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
                "[PlayerEquipmentManager] Không thể bind vũ khí vì InventorySystem không khả dụng.",
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
             * Item không nằm trong weapon slot có thể khiến
             * activeWeaponSlot bằng -1. Đây là trường hợp hợp lệ.
             */
            return;
        }

        InventorySlot activeSlot =
            inventory.weaponSlots[slotIndex];

        if (activeSlot == null ||
            activeSlot.IsEmpty)
        {
            Debug.LogWarning(
                $"[PlayerEquipmentManager] Weapon slot {slotIndex} đang trống.",
                this
            );

            return;
        }

        if (activeSlot.item != equippedItem)
        {
            Debug.LogWarning(
                "[PlayerEquipmentManager] Item đang trang bị không khớp với active weapon slot.",
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
         * Phải lưu đạn trước khi gọi OnUnequip và Destroy.
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

            Destroy(
                _currentEquippedModel
            );
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
    /// Chỉ ẩn Renderer.
    /// GameObject, WeaponInstance và trạng thái đạn vẫn hoạt động.
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
                $"[PlayerEquipmentManager] Không tìm thấy Layer " +
                $"'{_fpsLayerName}'. Layer của prefab được giữ nguyên.",
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