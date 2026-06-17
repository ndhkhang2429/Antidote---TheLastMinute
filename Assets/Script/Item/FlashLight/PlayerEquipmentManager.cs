using UnityEngine;

public class PlayerEquipmentManager : MonoBehaviour
{
    [Header("FPS Layer")]
    [SerializeField] private string _fpsLayerName = "FPSArms";

    public event System.Action<WeaponInstance> OnWeaponEquipped;

    private Animator _animator;
    private Transform _rightHandBone;
    private GameObject _currentEquippedModel;
    private Vector3 _gripOffset;
    private Vector3 _gripRotation;

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();

        // Ưu tiên Weapon_Root, fallback RightHand, fallback hand_r
        _rightHandBone = FindDeepChild(transform, "Weapon_Root");
        if (_rightHandBone == null)
            _rightHandBone = FindDeepChild(transform, "RightHand");
        if (_rightHandBone == null)
            _rightHandBone = FindDeepChild(transform, "hand_r");
        if (_rightHandBone == null)
            Debug.LogError("[PlayerEquipmentManager] Không tìm thấy bone gắn vũ khí!");

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHeldItemChanged += HandleItemChange;
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHeldItemChanged -= HandleItemChange;
    }


    void HandleItemChange(ItemDataSO heldItem)
    {
        if (_currentEquippedModel != null)
        {
            _currentEquippedModel.GetComponent<IEquippable>()?.OnUnequip();
            Destroy(_currentEquippedModel);
            _currentEquippedModel = null;
        }

        // Notify weapon removed
        OnWeaponEquipped?.Invoke(null);

        _gripOffset = Vector3.zero;
        _gripRotation = Vector3.zero;

        if (heldItem == null || heldItem.equipPrefab == null) return;
        if (_rightHandBone == null) return;

        if (heldItem is WeaponDataSO weaponData)
        {
            _gripOffset = weaponData.gripOffset;
            _gripRotation = weaponData.gripRotation;
        }

        _currentEquippedModel = Instantiate(
            heldItem.equipPrefab,
            _rightHandBone.position,
            _rightHandBone.rotation,
            _rightHandBone
        );

        _currentEquippedModel.transform.localPosition = _gripOffset;
        _currentEquippedModel.transform.localRotation = Quaternion.Euler(_gripRotation);

        int layer = LayerMask.NameToLayer(_fpsLayerName);
        if (layer >= 0)
            SetLayerRecursively(_currentEquippedModel, layer);

        _currentEquippedModel.GetComponent<IEquippable>()?.OnEquip();

        // Notify weapon equipped
        var weaponInstance = _currentEquippedModel.GetComponent<WeaponInstance>();
        OnWeaponEquipped?.Invoke(weaponInstance);
    }


    Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}