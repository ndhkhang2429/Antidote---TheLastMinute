using UnityEngine;

public class FPSInteractionVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEquipmentManager _equipmentManager;

    [Header("Optional Animator")]
    [SerializeField] private Animator _fpsAnimator;
    [SerializeField] private string _lootTriggerName = "PickUp";

    private bool _isInteracting;

    public bool IsInteracting => _isInteracting;

    private void Awake()
    {
        if (_equipmentManager == null)
        {
            _equipmentManager =
                GetComponentInParent<PlayerEquipmentManager>();
        }

        if (_fpsAnimator == null)
        {
            _fpsAnimator = GetComponent<Animator>();
        }
    }

    /// <summary>
    /// Dùng khi muốn script này tự kích hoạt animation nhặt.
    /// </summary>
    public void BeginLoot()
    {
        if (_isInteracting)
            return;

        HideWeaponForInteraction();

        if (_fpsAnimator != null &&
            !string.IsNullOrWhiteSpace(_lootTriggerName))
        {
            _fpsAnimator.ResetTrigger(_lootTriggerName);
            _fpsAnimator.SetTrigger(_lootTriggerName);
        }
    }

    /// <summary>
    /// Gọi bằng Animation Event ở đầu clip nhặt đồ.
    /// Hàm này chỉ ẩn súng, không kích hoạt lại trigger.
    /// </summary>
    public void HideWeaponForInteraction()
    {
        _isInteracting = true;
        _equipmentManager?.HideWeaponVisual();

        Debug.Log(
            "[FPSInteractionVisualController] Đã ẩn súng khi tương tác.",
            this
        );
    }

    /// <summary>
    /// Gọi bằng Animation Event ở cuối clip nhặt đồ.
    /// </summary>
    public void ShowWeaponAfterInteraction()
    {
        _equipmentManager?.ShowWeaponVisual();
        _isInteracting = false;

        Debug.Log(
            "[FPSInteractionVisualController] Đã hiện lại súng.",
            this
        );
    }

    private void OnDisable()
    {
        _equipmentManager?.ShowWeaponVisual();
        _isInteracting = false;
    }
}