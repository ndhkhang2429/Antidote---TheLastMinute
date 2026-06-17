using UnityEngine;

public class WeaponEquipManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;

    [Header("Override Attach Points (nếu không muốn dùng tên bone)")]
    [SerializeField] private Transform _rightHandOverride;  // drag bone RightHand vào đây
    [SerializeField] private Transform _leftHandOverride;   // optional

    private GameObject _currentWeaponGO;

    void Start()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        // Tự động lấy bone tay phải từ Animator nếu không override
        if (_rightHandOverride == null && _animator != null)
            _rightHandOverride = _animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHeldItemChanged += OnHeldItemChanged;
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHeldItemChanged -= OnHeldItemChanged;
    }

    void OnHeldItemChanged(ItemDataSO item)
    {
        if (_currentWeaponGO != null)
        {
            Destroy(_currentWeaponGO);
            _currentWeaponGO = null;
        }

        if (item == null || item.equipPrefab == null) return;

        Vector3 offset = Vector3.zero;
        Vector3 rot = Vector3.zero;

        if (item is WeaponDataSO weaponData)
        {
            offset = weaponData.gripOffset;
            rot = weaponData.gripRotation;
        }

        Transform attachPoint = _rightHandOverride;
        if (attachPoint == null)
        {
            Debug.LogWarning("[WeaponEquipManager] Không tìm thấy RightHand!");
            return;
        }

        _currentWeaponGO = Instantiate(
            item.equipPrefab,
            attachPoint.position,
            attachPoint.rotation,
            attachPoint
        );

        _currentWeaponGO.transform.localPosition = offset;
        _currentWeaponGO.transform.localRotation = Quaternion.Euler(rot);

        // ── Set toàn bộ weapon GO về layer FPSArms ──
        SetLayerRecursively(_currentWeaponGO, LayerMask.NameToLayer("FPSArms"));
    }

    void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}