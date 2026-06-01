using UnityEngine;
using StarterAssets;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerStatsSO _statsSO;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private LayerMask _interactLayer; // Chọn layer "Interactable" trong Inspector

    [Header("Thành phần kết nối")]
    [SerializeField] private Transform _weaponSlot;

    [Header("Events")]
    [SerializeField] private GameEventSO OnItemPickedUp;
    [SerializeField] private GameEventSO OnItemDropped;

    // ── Private Refs ───────────────────────────────────────
    private Animator _animator;
    private GameObject _currentTarget = null;

    // ── Animator param IDs ─────────────────────────────────
    private int _paramPickUp;
    private int _paramWeaponType;

    private float InteractionRadius => _statsSO != null ? _statsSO.interactionRadius : 3f;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_mainCamera == null) _mainCamera = Camera.main;

        _paramPickUp = Animator.StringToHash("PickUp");
        _paramWeaponType = Animator.StringToHash("WeaponType");

        if (PlayerState.Instance != null)
            PlayerState.Instance.OnWeaponChanged += SyncAnimatorWeaponType;
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnWeaponChanged -= SyncAnimatorWeaponType;
    }

    private void Update()
    {
        HandleRaycast();

        if (_currentTarget != null && Input.GetKeyDown(KeyCode.F))
            PerformPickup();

        if (Input.GetKeyDown(KeyCode.G))
            DropCurrentItem();
    }

    // ── Xử lý tia nhìn ─────────────────────────────────────
    private void HandleRaycast()
    {
        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, InteractionRadius, _interactLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Nếu tia nhìn chạm vào một vật MỚI
            if (hitObject != _currentTarget)
            {
                ClearCurrentTarget(); // Tắt vật cũ đi trước

                ItemData itemData = hitObject.GetComponent<ItemData>();
                if (itemData != null)
                {
                    _currentTarget = hitObject;

                    // Bật sáng vật mới
                    var highlight = _currentTarget.GetComponent<ItemHighlight>();
                    if (highlight != null) highlight.ToggleHighlight(true);

                    // Gọi UI hiển thị
                    InteractionUIManager.Instance.ShowPrompt($"[F] Nhặt {itemData.itemName}");
                }
            }
        }
        else
        {
            // Nếu nhìn ra chỗ khác (trượt khỏi vật phẩm)
            if (_currentTarget != null)
            {
                ClearCurrentTarget();
            }
        }
    }

    // Tắt highlight và ẩn UI
    private void ClearCurrentTarget()
    {
        if (_currentTarget != null)
        {
            var highlight = _currentTarget.GetComponent<ItemHighlight>();
            if (highlight != null) highlight.ToggleHighlight(false);

            _currentTarget = null;
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.HidePrompt();
        }
    }

    // ── Logic Nhặt / Vứt (Giữ nguyên của bạn) ──────────────
    private void PerformPickup()
    {
        if (_animator == null || _currentTarget == null) return;

        PlayerState.Instance?.SetPickingUp(true);
        _animator.SetTrigger(_paramPickUp);
    }

    public void EquipItem()
    {
        if (_currentTarget == null)
        {
            PlayerState.Instance?.SetPickingUp(false);
            return;
        }

        // 1. LƯU LẠI VẬT THỂ VÀO BIẾN TẠM
        GameObject itemToPickUp = _currentTarget;
        ItemData data = itemToPickUp.GetComponent<ItemData>();
        if (data == null) return;

        // 2. Bây giờ bạn gọi Clear vô tư, vì ta đã có itemToPickUp giữ data rồi
        ClearCurrentTarget();

        // 3. Thay _currentTarget bằng itemToPickUp ở mọi dòng bên dưới
        var rb = itemToPickUp.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        var col = itemToPickUp.GetComponent<Collider>();
        if (col) col.enabled = false;

        itemToPickUp.transform.SetParent(_weaponSlot);
        itemToPickUp.transform.localPosition = data.holdPositionOffset;
        itemToPickUp.transform.localRotation = Quaternion.Euler(data.holdRotationOffset);

        PlayerState.Instance?.EquipWeapon(data.weaponType, itemToPickUp);
        PlayerState.Instance?.SetPickingUp(false);

        OnItemPickedUp?.Raise();
    }

    private void DropCurrentItem()
    {
        if (PlayerState.Instance?.CurrentItemInHand == null) return;

        PlayerState.Instance.DropCurrentItem();
        OnItemDropped?.Raise();
    }

    private void SyncAnimatorWeaponType(int weaponType)
    {
        if (_animator != null) _animator.SetInteger(_paramWeaponType, weaponType);
    }

    // ── GIZMOS DEBUG ─────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (_mainCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(_mainCamera.transform.position, _mainCamera.transform.forward * InteractionRadius);
        }
    }
}