using UnityEngine;
using StarterAssets;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerStatsSO _statsSO;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private LayerMask _interactLayer;

    [Header("Thành phần kết nối")]
    [SerializeField] private Transform _weaponSlot;
    [SerializeField] private FusePanelManager _fusePanelManager;

    [Header("Events")]
    [SerializeField] private GameEventSO OnItemPickedUp;
    [SerializeField] private GameEventSO OnItemDropped;

    // ── Private ────────────────────────────────────────────
    private Animator _animator;
    private GameObject _currentTarget = null;

    // Lưu PanelInteractZone đang active (nếu có)
    private PanelInteractZone _activePanelZone = null;

    [Header("Quest Items")]
    public bool hasElectricalKey = false;

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
        // ── Khi đang examine (đọc tờ giấy) ───────────────
        if (ExamineUIController.Instance != null && ExamineUIController.Instance.IsExamining)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape))
                ExamineUIController.Instance.CloseExamine();
            return; // Không xử lý gì khác khi đang đọc
        }

        // ── Khi đang trong panel mode ──────────────────────
        if (_activePanelZone != null && _activePanelZone.IsInPanelMode)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape))
                _activePanelZone.TogglePanelMode();
            return; // Input còn lại (click switch...) do PanelInteractZone tự xử lý
        }

        // ── Gameplay bình thường ───────────────────────────
        HandleRaycast();

        if (_currentTarget != null && Input.GetKeyDown(KeyCode.F))
            InteractWithCurrentTarget();

        if (Input.GetKeyDown(KeyCode.G))
            DropCurrentItem();
    }

    // ── Raycast ────────────────────────────────────────────
    private void HandleRaycast()
    {
        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, InteractionRadius, _interactLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject != _currentTarget)
            {
                ClearCurrentTarget();
                _currentTarget = hitObject;

                var highlight = _currentTarget.GetComponent<ItemHighlight>();
                if (highlight != null) highlight.ToggleHighlight(true);

                // ── Phân loại UI prompt ────────────────────
                var itemData = hitObject.GetComponent<ItemDataSO>();
                var door = hitObject.GetComponentInParent<ElectricalDoor>();
                var mainSwitch = hitObject.GetComponentInParent<MainSwitchInteractable>();
                var fuseItem = hitObject.GetComponent<FuseItem>();
                var fuseSlot = hitObject.GetComponentInParent<FuseSlot>();
                var panelZone = hitObject.GetComponentInParent<PanelInteractZone>();   // NEW
                var examinable = hitObject.GetComponent<ExaminableObject>();             // NEW

                if (examinable != null)
                {
                    // Tờ giấy / vật phẩm đọc được
                    InteractionUIManager.Instance.ShowPrompt($"[F] Đọc {examinable.objectName}");
                }
                else if (panelZone != null)
                {
                    // Workstation có thể zoom vào
                    InteractionUIManager.Instance.ShowPrompt(panelZone.enterPrompt);
                }
                else if (fuseItem != null)
                {
                    InteractionUIManager.Instance.ShowPrompt($"[F] Nhặt {fuseItem.displayName}");
                }
                else if (fuseSlot != null && fuseSlot.requiresFuse && !fuseSlot.HasFuse)
                {
                    if (_fusePanelManager != null && _fusePanelManager.HasFuseInHand)
                        InteractionUIManager.Instance.ShowPrompt($"[F] Gắn cầu chì vào slot {fuseSlot.slotIndex}");
                    else
                        InteractionUIManager.Instance.ShowPrompt($"Slot {fuseSlot.slotIndex} trống – cần cầu chì");
                }
                else if (mainSwitch != null)
                {
                    InteractionUIManager.Instance.ShowPrompt("[F] Gạt cần điện");
                }
                else if (itemData != null)
                {
                    InteractionUIManager.Instance.ShowPrompt($"[F] Nhặt {itemData.itemName}");
                }
                else if (door != null)
                {
                    InteractionUIManager.Instance.ShowPrompt(door.isOpen ? "[F] Đóng cửa" : "[F] Mở tủ điện");
                }
                else if (hitObject.CompareTag("ElectricalKey"))
                {
                    InteractionUIManager.Instance.ShowPrompt("[F] Lấy chìa khóa");
                }
                else
                {
                    _currentTarget = null;
                }
            }
        }
        else
        {
            if (_currentTarget != null) ClearCurrentTarget();
        }
    }

    private void ClearCurrentTarget()
    {
        if (_currentTarget != null)
        {
            var highlight = _currentTarget.GetComponent<ItemHighlight>();
            if (highlight != null) highlight.ToggleHighlight(false);
            _currentTarget = null;
        }
        InteractionUIManager.Instance?.HidePrompt();
    }

    // ── Interact ───────────────────────────────────────────
    private void InteractWithCurrentTarget()
    {
        var itemData = _currentTarget.GetComponent<ItemDataSO>();
        var door = _currentTarget.GetComponentInParent<ElectricalDoor>();
        var mainSwitch = _currentTarget.GetComponentInParent<MainSwitchInteractable>();
        var fuseItem = _currentTarget.GetComponent<FuseItem>();
        var fuseSlot = _currentTarget.GetComponentInParent<FuseSlot>();
        var panelZone = _currentTarget.GetComponentInParent<PanelInteractZone>();
        var examinable = _currentTarget.GetComponent<ExaminableObject>();

        if (examinable != null)
        {
            // Mở UI đọc - object vẫn còn nguyên tại chỗ
            ExamineUIController.Instance?.OpenExamine(examinable);
        }
        else if (panelZone != null)
        {
            // Zoom camera vào panel
            _activePanelZone = panelZone;
            panelZone.TogglePanelMode();
            ClearCurrentTarget();
        }
        else if (fuseItem != null)
        {
            _fusePanelManager?.PickUpFuse(fuseItem.fuseID);
            InteractionUIManager.Instance.ShowPrompt($"Đã nhặt {fuseItem.displayName}");
            Destroy(_currentTarget);
            ClearCurrentTarget();
        }
        else if (fuseSlot != null && fuseSlot.requiresFuse && !fuseSlot.HasFuse)
        {
            if (_fusePanelManager != null && _fusePanelManager.HasFuseInHand)
            {
                bool success = _fusePanelManager.TryInsertHeldFuse(fuseSlot);
                InteractionUIManager.Instance.ShowPrompt(
                    success ? "Gắn cầu chì thành công!" : "Sai cầu chì cho slot này!");
            }
            else
            {
                InteractionUIManager.Instance.ShowPrompt("Không có cầu chì trong tay!");
            }
            ClearCurrentTarget();
        }
        else if (mainSwitch != null)
        {
            mainSwitch.Interact();
        }
        else if (itemData != null)
        {
            PerformPickup();
        }
        else if (door != null)
        {
            door.InteractWithDoor(hasElectricalKey);
            ClearCurrentTarget();
        }
        else if (_currentTarget.CompareTag("ElectricalKey"))
        {
            hasElectricalKey = true;
            Destroy(_currentTarget);
            ClearCurrentTarget();
        }
    }

    // ── Pickup / Drop ──────────────────────────────────────
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

        GameObject itemToPickUp = _currentTarget;

        // Lấy data từ WorldItem thay vì GetComponent<ItemDataSO>()
        WorldItem worldItem = itemToPickUp.GetComponent<WorldItem>();
        if (worldItem == null || worldItem.itemData == null)
        {
            PlayerState.Instance?.SetPickingUp(false);
            return;
        }

        ItemDataSO data = worldItem.itemData;
        int quantity = worldItem.quantity;

        ClearCurrentTarget();

        // Dùng PickupItem public thay vì TryAddToGrid private
        bool picked = InventorySystem.Instance != null
                      ? InventorySystem.Instance.PickupItem(data, quantity)
                      : false;

        if (picked)
        {
            Destroy(itemToPickUp);
            OnItemPickedUp?.Raise();
        }
        else
        {
            Debug.Log("[PlayerInteraction] Balo đầy hoặc không nhặt được!");
        }

        PlayerState.Instance?.SetPickingUp(false);
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

    private void OnDrawGizmos()
    {
        if (_mainCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(_mainCamera.transform.position,
                           _mainCamera.transform.forward * InteractionRadius);
        }
    }
}