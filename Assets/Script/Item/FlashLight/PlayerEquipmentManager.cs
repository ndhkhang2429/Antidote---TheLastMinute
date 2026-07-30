using UnityEngine;

public class PlayerEquipmentManager : MonoBehaviour
{
    [Header("FPS Weapon Setup")]
    [Tooltip("Kéo FPSWeaponSocket nằm bên trong FPS_HANDS vào đây.")]
    [SerializeField] private Transform _fpsWeaponSocket;

    [Tooltip("Layer dùng riêng cho tay và súng góc nhìn thứ nhất.")]
    [SerializeField] private string _fpsLayerName = "FPS_Arms";

    public event System.Action<WeaponInstance> OnWeaponEquipped;

    private GameObject _currentEquippedModel;
    private WeaponInstance _currentWeaponInstance;

    private void Start()
    {
        if (_fpsWeaponSocket == null)
        {
            Debug.LogError(
                "[PlayerEquipmentManager] Chưa gán FPSWeaponSocket trong Inspector!",
                this
            );
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnHeldItemChanged += HandleItemChange;
        }
        else
        {
            Debug.LogWarning(
                "[PlayerEquipmentManager] Không tìm thấy InventorySystem.Instance.",
                this
            );
        }
    }

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnHeldItemChanged -= HandleItemChange;
        }
    }

    private void HandleItemChange(ItemDataSO heldItem)
    {
        UnequipCurrentItem();

        if (heldItem == null || heldItem.equipPrefab == null)
        {
            OnWeaponEquipped?.Invoke(null);
            return;
        }

        if (_fpsWeaponSocket == null)
        {
            Debug.LogError(
                "[PlayerEquipmentManager] Không thể trang bị vì FPSWeaponSocket chưa được gán.",
                this
            );

            OnWeaponEquipped?.Invoke(null);
            return;
        }

        Vector3 gripOffset = Vector3.zero;
        Vector3 gripRotation = Vector3.zero;

        if (heldItem is WeaponDataSO weaponData)
        {
            gripOffset = weaponData.gripOffset;
            gripRotation = weaponData.gripRotation;
        }

        _currentEquippedModel = Instantiate(
            heldItem.equipPrefab,
            _fpsWeaponSocket
        );

        Transform equippedTransform = _currentEquippedModel.transform;

        equippedTransform.localPosition = gripOffset;
        equippedTransform.localRotation = Quaternion.Euler(gripRotation);
        equippedTransform.localScale = Vector3.one;

        int fpsLayer = LayerMask.NameToLayer(_fpsLayerName);

        if (fpsLayer >= 0)
        {
            SetLayerRecursively(_currentEquippedModel, fpsLayer);
        }
        else
        {
            Debug.LogWarning(
                $"[PlayerEquipmentManager] Không tìm thấy layer '{_fpsLayerName}'.",
                this
            );
        }

        IEquippable equippable =
            _currentEquippedModel.GetComponent<IEquippable>();

        equippable?.OnEquip();

        _currentWeaponInstance =
            _currentEquippedModel.GetComponent<WeaponInstance>();

        OnWeaponEquipped?.Invoke(_currentWeaponInstance);
    }

    private void UnequipCurrentItem()
    {
        if (_currentEquippedModel == null)
        {
            _currentWeaponInstance = null;
            return;
        }

        IEquippable equippable =
            _currentEquippedModel.GetComponent<IEquippable>();

        equippable?.OnUnequip();

        Destroy(_currentEquippedModel);

        _currentEquippedModel = null;
        _currentWeaponInstance = null;

        OnWeaponEquipped?.Invoke(null);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
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