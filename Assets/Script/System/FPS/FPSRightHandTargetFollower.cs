using UnityEngine;

public class FPSRightHandTargetFollower : MonoBehaviour
{
    [Header("Equipment")]
    [SerializeField] private PlayerEquipmentManager equipmentManager;

    [Header("Target thuộc FPSRigLayer")]
    [SerializeField] private Transform ikTarget;

    [Header("Tên target trong prefab súng")]
    [SerializeField] private string weaponTargetName = "RightHandTarget";

    private Transform weaponTarget;

    private void Awake()
    {
        if (equipmentManager == null)
        {
            equipmentManager =
                GetComponentInParent<PlayerEquipmentManager>();
        }

        if (ikTarget == null)
        {
            Debug.LogError(
                "[FPSRightHandTargetFollower] Chưa gán RightHandIKTarget.",
                this
            );
        }
    }

    private void OnEnable()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnWeaponEquipped += HandleWeaponEquipped;
        }
    }

    private void OnDisable()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnWeaponEquipped -= HandleWeaponEquipped;
        }
    }

    private void HandleWeaponEquipped(WeaponInstance weapon)
    {
        weaponTarget = null;

        if (weapon == null)
        {
            return;
        }

        weaponTarget = FindDeepChild(
            weapon.transform,
            weaponTargetName
        );

        if (weaponTarget == null)
        {
            Debug.LogError(
                $"[FPSRightHandTargetFollower] Không tìm thấy " +
                $"{weaponTargetName} trong {weapon.name}.",
                weapon
            );
            return;
        }

        CopyTargetTransform();
    }

    private void LateUpdate()
    {
        CopyTargetTransform();
    }

    private void CopyTargetTransform()
    {
        if (ikTarget == null || weaponTarget == null)
        {
            return;
        }

        ikTarget.SetPositionAndRotation(
            weaponTarget.position,
            weaponTarget.rotation
        );
    }

    private static Transform FindDeepChild(
        Transform parent,
        string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
            {
                return child;
            }

            Transform result = FindDeepChild(child, targetName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}