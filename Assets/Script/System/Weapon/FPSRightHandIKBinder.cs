using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class FPSRightHandIKBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEquipmentManager equipmentManager;
    [SerializeField] private RigBuilder rigBuilder;
    [SerializeField] private TwoBoneIKConstraint rightArmIK;

    [Header("Tên object trong prefab súng")]
    [SerializeField] private string rightHandTargetName = "RightHandTarget";

    private void Awake()
    {
        if (equipmentManager == null)
            equipmentManager = GetComponent<PlayerEquipmentManager>();

        if (rigBuilder == null)
            rigBuilder = GetComponent<RigBuilder>();
    }

    private void OnEnable()
    {
        if (equipmentManager != null)
            equipmentManager.OnWeaponEquipped += HandleWeaponEquipped;
    }

    private void OnDisable()
    {
        if (equipmentManager != null)
            equipmentManager.OnWeaponEquipped -= HandleWeaponEquipped;
    }

    private void HandleWeaponEquipped(WeaponInstance weapon)
    {
        StopAllCoroutines();
        StartCoroutine(BindTargetNextFrame(weapon));
    }

    private IEnumerator BindTargetNextFrame(WeaponInstance weapon)
    {
        // Chờ súng và toàn bộ object con được tạo hoàn chỉnh.
        yield return null;

        if (weapon == null)
        {
            ClearTarget();
            yield break;
        }

        Transform target = FindDeepChild(
            weapon.transform,
            rightHandTargetName
        );

        if (target == null)
        {
            Debug.LogError(
                $"[FPSRightHandIKBinder] Không tìm thấy " +
                $"{rightHandTargetName} trong {weapon.name}.",
                weapon
            );

            ClearTarget();
            yield break;
        }

        TwoBoneIKConstraintData data = rightArmIK.data;

        data.target = target;
        data.hint = null;

        data.targetPositionWeight = 1f;
        data.targetRotationWeight = 0f;
        data.hintWeight = 0f;

        data.maintainTargetPositionOffset = false;
        data.maintainTargetRotationOffset = false;

        rightArmIK.data = data;
        rightArmIK.weight = 1f;

        RebuildRig();

        Debug.Log(
            $"[FPSRightHandIKBinder] Đã gán IK Target: {target.name}",
            target
        );
    }

    private void ClearTarget()
    {
        if (rightArmIK == null)
            return;

        TwoBoneIKConstraintData data = rightArmIK.data;

        data.target = null;
        data.hint = null;

        rightArmIK.data = data;
        rightArmIK.weight = 0f;

        RebuildRig();
    }

    private void RebuildRig()
    {
        if (rigBuilder == null)
            return;

        rigBuilder.Clear();
        rigBuilder.Build();
    }

    private static Transform FindDeepChild(
        Transform parent,
        string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            Transform result = FindDeepChild(child, targetName);

            if (result != null)
                return result;
        }

        return null;
    }
}