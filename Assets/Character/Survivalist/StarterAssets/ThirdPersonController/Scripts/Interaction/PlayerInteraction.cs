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

    private Animator _animator;
    private ThirdPersonController _tpc; // [THÊM MỚI] Biến lưu trữ ThirdPersonController
    private GameObject _currentTarget = null;
    private PanelInteractZone _activePanelZone = null;

    [Header("Quest Items")]
    public bool hasElectricalKey = false;

    private int _paramPickUp;
    private int _paramWeaponType;
    private float InteractionRadius => _statsSO != null ? _statsSO.interactionRadius : 3f;

    // ── Lifecycle ──────────────────────────────────────────
    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _tpc = GetComponent<ThirdPersonController>(); // [THÊM MỚI] Lấy component TPC

        if (_mainCamera == null) _mainCamera = Camera.main;

        _paramPickUp = Animator.StringToHash("PickUp");
        _paramWeaponType = Animator.StringToHash("WeaponType");

        if (PlayerState.Instance != null)
            PlayerState.Instance.OnWeaponChanged += SyncAnimatorWeaponType;
    }

    void OnDestroy()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnWeaponChanged -= SyncAnimatorWeaponType;
    }

    void Update()
    {
        if (ExamineUIController.Instance != null && ExamineUIController.Instance.IsExamining)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape))
                ExamineUIController.Instance.CloseExamine();
            return;
        }

        if (_activePanelZone != null && _activePanelZone.IsInPanelMode)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape))
                _activePanelZone.TogglePanelMode();
            return;
        }

        HandleRaycast();

        if (_currentTarget != null && Input.GetKeyDown(KeyCode.F))
        {
            // [THÊM MỚI] Ép thân người xoay mặt về hướng Camera ngay lập tức trước khi tương tác
            if (_tpc != null)
            {
                _tpc.SmoothFaceCameraDirection();
            }

            InteractWithCurrentTarget();
        }

        if (Input.GetKeyDown(KeyCode.G))
            DropCurrentItem();
    }

    // ── Raycast ────────────────────────────────────────────
    void HandleRaycast()
    {
        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, InteractionRadius, _interactLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject != _currentTarget)
            {
                ClearCurrentTarget();
                _currentTarget = hitObject;
                _currentTarget.GetComponent<ItemHighlight>()?.ToggleHighlight(true);
                ShowPromptForTarget(hitObject);
            }
        }
        else
        {
            if (_currentTarget != null) ClearCurrentTarget();
        }
    }

    void ShowPromptForTarget(GameObject hitObject)
    {
        // ── IInteractable — tất cả quest slot ─────────────
        var interactable = hitObject.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            string prompt = interactable.GetPrompt();
            if (prompt != null)
                InteractionUIManager.Instance.ShowPrompt(prompt);
            else
                _currentTarget = null;
            return;
        }

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

        // ── Main switch ───────────────────────────────────
        var mainSwitch = hitObject.GetComponentInParent<MainSwitchInteractable>();
        if (mainSwitch != null)
        {
            InteractionUIManager.Instance.ShowPrompt("[F] Gạt cần điện");
            return;
        }

        // ── WorldItem ─────────────────────────────────────
        var worldItem = hitObject.GetComponent<WorldItem>();
        if (worldItem != null && worldItem.itemData != null)
        {
            string name = worldItem.itemData is FuseItemDataSO f
                ? $"Cầu chì [{f.fuseID}]"
                : worldItem.itemData.itemName;
            InteractionUIManager.Instance.ShowPrompt($"[F] Nhặt {name}");
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

        _currentTarget = null;
    }

    void ClearCurrentTarget()
    {
        if (_currentTarget != null)
        {
            _currentTarget.GetComponent<ItemHighlight>()?.ToggleHighlight(false);
            _currentTarget = null;
        }
        InteractionUIManager.Instance?.HidePrompt();
    }

    // ── Interact ───────────────────────────────────────────
    void InteractWithCurrentTarget()
    {
        if (_currentTarget == null) return;
        var inv = InventorySystem.Instance;

        // ── IInteractable ─────────────────────────────────
        var interactable = _currentTarget.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            interactable.TryInteract(inv);
            ClearCurrentTarget();
            return;
        }

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

        // ── Main switch ───────────────────────────────────
        var mainSwitch = _currentTarget.GetComponentInParent<MainSwitchInteractable>();
        if (mainSwitch != null)
        {
            mainSwitch.Interact();
            return;
        }

        // ── WorldItem ─────────────────────────────────────
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
    void PerformPickup()
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
        WorldItem worldItem = itemToPickUp.GetComponent<WorldItem>();

        if (worldItem == null || worldItem.itemData == null)
        {
            Debug.LogWarning("[PlayerInteraction] Không tìm thấy WorldItem!");
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

    void DropCurrentItem()
    {
        if (PlayerState.Instance?.CurrentItemInHand == null) return;
        PlayerState.Instance.DropCurrentItem();
        OnItemDropped?.Raise();
    }

    void SyncAnimatorWeaponType(int weaponType)
    {
        if (_animator != null)
            _animator.SetInteger(_paramWeaponType, weaponType);
    }

    void OnDrawGizmos()
    {
        if (_mainCamera == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_mainCamera.transform.position,
                       _mainCamera.transform.forward * InteractionRadius);
    }
}