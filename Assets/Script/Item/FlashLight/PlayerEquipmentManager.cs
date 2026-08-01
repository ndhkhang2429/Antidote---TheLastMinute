using System;
using UnityEngine;

public class PlayerEquipmentManager : MonoBehaviour
{
    [Header("FPS Weapon Setup")]
    [Tooltip("Kéo FPSWeaponSocket nằm dưới WeaponRecoilRoot vào đây.")]
    [SerializeField] private Transform _fpsWeaponSocket;

    [Tooltip("Layer dành riêng cho tay và vũ khí góc nhìn thứ nhất.")]
    [SerializeField] private string _fpsLayerName = "FPS_Arms";

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs;

    /// <summary>
    /// Gửi WeaponInstance mới sau khi trang bị.
    /// Gửi null khi tháo vũ khí hoặc item hiện tại không phải vũ khí.
    /// </summary>
    public event Action<WeaponInstance> OnWeaponEquipped;

    private GameObject _currentEquippedModel;
    private WeaponInstance _currentWeaponInstance;
    private Renderer[] _currentWeaponRenderers;

    private bool _weaponVisualVisible = true;

    public GameObject CurrentEquippedModel => _currentEquippedModel;
    public WeaponInstance CurrentWeaponInstance => _currentWeaponInstance;
    public bool HasEquippedItem => _currentEquippedModel != null;
    public bool HasEquippedWeapon => _currentWeaponInstance != null;
    public bool IsWeaponVisualVisible => _weaponVisualVisible;

    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        SubscribeInventoryEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeInventoryEvents();
    }

    private void ValidateReferences()
    {
        if (_fpsWeaponSocket == null)
        {
            Debug.LogError(
                "[PlayerEquipmentManager] Chưa gán FPSWeaponSocket trong Inspector.",
                this
            );
        }

        if (string.IsNullOrWhiteSpace(_fpsLayerName))
        {
            Debug.LogWarning(
                "[PlayerEquipmentManager] Tên FPS Layer đang để trống.",
                this
            );
        }
        else if (LayerMask.NameToLayer(_fpsLayerName) < 0)
        {
            Debug.LogWarning(
                $"[PlayerEquipmentManager] Không tìm thấy layer '{_fpsLayerName}'.",
                this
            );
        }
    }

    private void SubscribeInventoryEvents()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning(
                "[PlayerEquipmentManager] Không tìm thấy InventorySystem.Instance.",
                this
            );

            return;
        }

        InventorySystem.Instance.OnHeldItemChanged += HandleItemChange;
    }

    private void UnsubscribeInventoryEvents()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnHeldItemChanged -= HandleItemChange;
        }
    }

    private void HandleItemChange(ItemDataSO heldItem)
    {
        UnequipCurrentItem(false);

        if (heldItem == null || heldItem.equipPrefab == null)
        {
            NotifyWeaponChanged(null);
            return;
        }

        if (_fpsWeaponSocket == null)
        {
            Debug.LogError(
                "[PlayerEquipmentManager] Không thể trang bị vì FPSWeaponSocket chưa được gán.",
                this
            );

            NotifyWeaponChanged(null);
            return;
        }

        SpawnEquippedItem(heldItem);
    }

    private void SpawnEquippedItem(ItemDataSO heldItem)
    {
        Vector3 localPosition = Vector3.zero;
        Vector3 localRotation = Vector3.zero;

        if (heldItem is WeaponDataSO weaponData)
        {
            localPosition = weaponData.gripOffset;
            localRotation = weaponData.gripRotation;
        }

        _currentEquippedModel = Instantiate(
            heldItem.equipPrefab,
            _fpsWeaponSocket
        );

        _currentEquippedModel.name =
            $"{heldItem.equipPrefab.name}_FPS";

        Transform equippedTransform =
            _currentEquippedModel.transform;

        equippedTransform.localPosition = localPosition;
        equippedTransform.localRotation =
            Quaternion.Euler(localRotation);
        equippedTransform.localScale = Vector3.one;

        ApplyFPSLayer(_currentEquippedModel);

        _currentWeaponRenderers =
            _currentEquippedModel.GetComponentsInChildren<Renderer>(true);

        // Giữ đúng trạng thái ẩn/hiện hiện tại.
        ApplyWeaponVisualState();

        IEquippable equippable =
            _currentEquippedModel.GetComponent<IEquippable>();

        equippable?.OnEquip();

        _currentWeaponInstance =
            _currentEquippedModel.GetComponent<WeaponInstance>();

        if (_showDebugLogs)
        {
            string itemType =
                _currentWeaponInstance != null
                    ? "Weapon"
                    : "Equippable item";

            Debug.Log(
                $"[PlayerEquipmentManager] Đã trang bị {itemType}: " +
                $"{heldItem.name}",
                _currentEquippedModel
            );
        }

        NotifyWeaponChanged(_currentWeaponInstance);
    }

    private void UnequipCurrentItem(bool notify)
    {
        if (_currentEquippedModel != null)
        {
            // Phải phát trước khi model súng bị Destroy.
            WeaponAudioController weaponAudio =
                _currentEquippedModel
                    .GetComponent<WeaponAudioController>();

            if (weaponAudio == null)
            {
                weaponAudio =
                    _currentEquippedModel
                        .GetComponentInChildren<WeaponAudioController>(true);
            }

            weaponAudio?.PlayHolsterDetached();

            IEquippable equippable =
                _currentEquippedModel.GetComponent<IEquippable>();

            if (equippable == null)
            {
                equippable =
                    _currentEquippedModel
                        .GetComponentInChildren<IEquippable>(true);
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
    /// Chỉ ẩn Renderer của item/súng.
    /// Object, WeaponInstance, gunBarrel và dữ liệu đạn vẫn tồn tại.
    /// </summary>
    public void SetWeaponVisualVisible(bool visible)
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
        {
            return;
        }

        foreach (Renderer itemRenderer in _currentWeaponRenderers)
        {
            if (itemRenderer != null)
            {
                itemRenderer.enabled = _weaponVisualVisible;
            }
        }
    }

    private void ApplyFPSLayer(GameObject target)
    {
        int fpsLayer = LayerMask.NameToLayer(_fpsLayerName);

        if (fpsLayer < 0)
        {
            Debug.LogWarning(
                $"[PlayerEquipmentManager] Không tìm thấy layer " +
                $"'{_fpsLayerName}'. Giữ nguyên layer của prefab.",
                this
            );

            return;
        }

        SetLayerRecursively(target, fpsLayer);
    }

    private void NotifyWeaponChanged(WeaponInstance weaponInstance)
    {
        OnWeaponEquipped?.Invoke(weaponInstance);
    }

    private static void SetLayerRecursively(
        GameObject target,
        int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}