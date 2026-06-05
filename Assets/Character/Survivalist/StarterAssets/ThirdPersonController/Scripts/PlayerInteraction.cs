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
    private PanelInteractZone _activePanelZone = null;

    [Header("Quest Items")]
    public bool hasElectricalKey = false;

    private int _paramPickUp;
    private int _paramWeaponType;

    private float InteractionRadius => _statsSO != null ? _statsSO.interactionRadius : 3f;

    // ── Lifecycle ──────────────────────────────────────────
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
        // ── Đang đọc tờ giấy ──────────────────────────────
        if (ExamineUIController.Instance != null && ExamineUIController.Instance.IsExamining)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape))
                ExamineUIController.Instance.CloseExamine();
            return;
        }

        // ── Đang trong panel mode ──────────────────────────
        if (_activePanelZone != null && _activePanelZone.IsInPanelMode)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape))
                _activePanelZone.TogglePanelMode();
            return;
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
                highlight?.ToggleHighlight(true);

                ShowPromptForTarget(hitObject);
            }
        }
        else
        {
            if (_currentTarget != null) ClearCurrentTarget();
        }
    }

    private void ShowPromptForTarget(GameObject hitObject)
    {
        // ── Examinable ────────────────────────────────────
        var examinable = hitObject.GetComponent<ExaminableObject>();
        if (examinable != null)
        {
            InteractionUIManager.Instance.ShowPrompt($"[F] Đọc {examinable.objectName}");
            return;
        }

        // ── Panel zone ────────────────────────────────────
        var panelZone = hitObject.GetComponentInParent<PanelInteractZone>();
        if (panelZone != null)
        {
            InteractionUIManager.Instance.ShowPrompt(panelZone.enterPrompt);
            return;
        }

        // ── FuseSlot trên thùng điện ──────────────────────
        var fuseSlot = hitObject.GetComponentInParent<FuseSlot>();
        if (fuseSlot != null && fuseSlot.requiresFuse && !fuseSlot.HasFuse)
        {
            // Kiểm tra inventory có đúng fuse không
            bool hasCorrectFuse = HasFuseInInventory(fuseSlot.correctFuseID);
            string msg = hasCorrectFuse
                ? $"[F] Gắn cầu chì vào slot {fuseSlot.slotIndex}"
                : $"Cần cầu chì {fuseSlot.correctFuseID} – kiểm tra inventory";
            InteractionUIManager.Instance.ShowPrompt(msg);
            return;
        }

        // ── Main switch ───────────────────────────────────
        var mainSwitch = hitObject.GetComponentInParent<MainSwitchInteractable>();
        if (mainSwitch != null)
        {
            InteractionUIManager.Instance.ShowPrompt("[F] Gạt cần điện");
            return;
        }

        // ── WorldItem — bao gồm cả FuseItem mới ──────────
        var worldItem = hitObject.GetComponent<WorldItem>();
        if (worldItem != null && worldItem.itemData != null)
        {
            // Hiện tên item, nếu là FuseItemDataSO thì hiện thêm ID
            string itemDisplayName = worldItem.itemData.itemName;
            if (worldItem.itemData is FuseItemDataSO fuseData)
                itemDisplayName = $"Cầu chì [{fuseData.fuseID}]";

            InteractionUIManager.Instance.ShowPrompt($"[F] Nhặt {itemDisplayName}");
            return;
        }

        // ── Electrical door ───────────────────────────────
        var door = hitObject.GetComponentInParent<ElectricalDoor>();
        if (door != null)
        {
            InteractionUIManager.Instance.ShowPrompt(
                door.isOpen ? "[F] Đóng cửa" : "[F] Mở tủ điện");
            return;
        }

        // ── Electrical key ────────────────────────────────
        if (hitObject.CompareTag("ElectricalKey"))
        {
            InteractionUIManager.Instance.ShowPrompt("[F] Lấy chìa khóa");
            return;
        }

        // Không nhận dạng được → bỏ target
        _currentTarget = null;
    }

    private void ClearCurrentTarget()
    {
        if (_currentTarget != null)
        {
            var highlight = _currentTarget.GetComponent<ItemHighlight>();
            highlight?.ToggleHighlight(false);
            _currentTarget = null;
        }
        InteractionUIManager.Instance?.HidePrompt();
    }

    // ── Interact ───────────────────────────────────────────
    private void InteractWithCurrentTarget()
    {
        if (_currentTarget == null) return;

        // ── Examinable ────────────────────────────────────
        var examinable = _currentTarget.GetComponent<ExaminableObject>();
        if (examinable != null)
        {
            ExamineUIController.Instance?.OpenExamine(examinable);
            return;
        }

        // ── Panel zone ────────────────────────────────────
        var panelZone = _currentTarget.GetComponentInParent<PanelInteractZone>();
        if (panelZone != null)
        {
            _activePanelZone = panelZone;
            panelZone.TogglePanelMode();
            ClearCurrentTarget();
            return;
        }

        // ── FuseSlot — gắn fuse từ inventory ─────────────
        var fuseSlot = _currentTarget.GetComponentInParent<FuseSlot>();
        if (fuseSlot != null && fuseSlot.requiresFuse && !fuseSlot.HasFuse)
        {
            bool hasCorrectFuse = HasFuseInInventory(fuseSlot.correctFuseID);
            if (!hasCorrectFuse)
            {
                InteractionUIManager.Instance.ShowPrompt(
                    $"Cần cầu chì {fuseSlot.correctFuseID} trong inventory!");
                ClearCurrentTarget();
                return;
            }

            if (_fusePanelManager != null)
            {
                bool success = _fusePanelManager.TryInsertHeldFuse(fuseSlot);
                InteractionUIManager.Instance.ShowPrompt(
                    success
                    ? $"✓ Gắn {fuseSlot.correctFuseID} thành công!"
                    : $"✗ Gắn thất bại!");
            }
            ClearCurrentTarget();
            return;
        }

        // ── Main switch ───────────────────────────────────
        var mainSwitch = _currentTarget.GetComponentInParent<MainSwitchInteractable>();
        if (mainSwitch != null)
        {
            mainSwitch.Interact();
            return;
        }

        // ── WorldItem — nhặt vào inventory ───────────────
        // Bao gồm cả FuseItem mới (dùng FuseItemDataSO + WorldItem)
        var worldItem = _currentTarget.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            PerformPickup();
            return;
        }

        // ── Electrical door ───────────────────────────────
        var door = _currentTarget.GetComponentInParent<ElectricalDoor>();
        if (door != null)
        {
            door.InteractWithDoor(hasElectricalKey);
            ClearCurrentTarget();
            return;
        }

        // ── Electrical key ────────────────────────────────
        if (_currentTarget.CompareTag("ElectricalKey"))
        {
            hasElectricalKey = true;
            Destroy(_currentTarget);
            ClearCurrentTarget();
        }
    }

    // ── Pickup ─────────────────────────────────────────────
    private void PerformPickup()
    {
        if (_animator == null || _currentTarget == null) return;
        PlayerState.Instance?.SetPickingUp(true);
        _animator.SetTrigger(_paramPickUp);
    }

    /// <summary>
    /// Gọi từ Animation Event khi frame pickup của animation.
    /// </summary>
    public void EquipItem()
    {
        if (_currentTarget == null)
        {
            PlayerState.Instance?.SetPickingUp(false);
            return;
        }

        GameObject itemToPickUp = _currentTarget;

        WorldItem worldItem = itemToPickUp.GetComponent<WorldItem>();
        if (worldItem == null || worldItem.itemData == null)
        {
            Debug.LogWarning("[PlayerInteraction] Không tìm thấy WorldItem hoặc itemData!");
            PlayerState.Instance?.SetPickingUp(false);
            return;
        }

        ItemDataSO data = worldItem.itemData;
        int qty = worldItem.quantity;

        ClearCurrentTarget();

        bool picked = InventorySystem.Instance != null
                   && InventorySystem.Instance.PickupItem(data, qty);

        if (picked)
        {
            Destroy(itemToPickUp);
            OnItemPickedUp?.Raise();
            Debug.Log($"[PlayerInteraction] Đã nhặt: {data.itemName} x{qty}");
        }
        else
        {
            Debug.Log("[PlayerInteraction] Balo đầy hoặc không nhặt được!");
        }

        PlayerState.Instance?.SetPickingUp(false);
    }

    // ── Drop ───────────────────────────────────────────────
    private void DropCurrentItem()
    {
        if (PlayerState.Instance?.CurrentItemInHand == null) return;
        PlayerState.Instance.DropCurrentItem();
        OnItemDropped?.Raise();
    }

    // ── Helper: kiểm tra inventory có fuse đúng ID không ──
    private bool HasFuseInInventory(string fuseID)
    {
        if (InventorySystem.Instance == null) return false;
        foreach (var slot in InventorySystem.Instance.GetItemSlots())
        {
            if (slot.IsEmpty) continue;
            if (slot.item is FuseItemDataSO fuse && fuse.fuseID == fuseID)
                return true;
        }
        return false;
    }

    // ── Helpers ────────────────────────────────────────────
    private void SyncAnimatorWeaponType(int weaponType)
    {
        if (_animator != null)
            _animator.SetInteger(_paramWeaponType, weaponType);
    }

    private void OnDrawGizmos()
    {
        if (_mainCamera == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_mainCamera.transform.position,
                       _mainCamera.transform.forward * InteractionRadius);
    }
}