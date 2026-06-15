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

    [Header("Tùy chỉnh UI Tương tác")]
    [SerializeField] private string _interactKey = "[F]";
    [SerializeField] private Color _keyColor = new Color(1f, 0.8f, 0f); // Màu Vàng mặc định
    [SerializeField] private Color _itemColor = new Color(0.6f, 0.6f, 0.6f); // Màu Xám mặc định

    private Animator _animator;
    private ThirdPersonController _tpc;
    private GameObject _currentTarget = null;
    private PanelInteractZone _activePanelZone = null;

    private int _paramPickUp;
    private int _paramWeaponType;
    private float InteractionRadius => _statsSO != null ? _statsSO.interactionRadius : 3f;

    // ── Lifecycle ──────────────────────────────────────────
    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _tpc = GetComponent<ThirdPersonController>();

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
            if (_tpc != null)
                _tpc.SmoothFaceCameraDirection();

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

                bool hasPrompt = ShowPromptForTarget(hitObject);

                if (hasPrompt)
                {
                    _currentTarget = hitObject;
                    _currentTarget.GetComponent<ItemHighlight>()?.ToggleHighlight(true);
                }
                else
                {
                    ClearCurrentTarget();
                }
            }
        }
        else
        {
            ClearCurrentTarget();
        }
    }

    bool ShowPromptForTarget(GameObject hitObject)
    {
        // Chuyển màu bạn chọn ở Inspector sang mã Hex HTML (chỉ chạy 1 lần khi đưa tâm vào vật)
        string hexKey = ColorUtility.ToHtmlStringRGB(_keyColor);
        string hexItem = ColorUtility.ToHtmlStringRGB(_itemColor);

        // Tạo sẵn nút bấm với màu bạn chọn
        string fBtn = $"<b><color=#{hexKey}>{_interactKey}</color></b>";

        // ── IQuestRequirement (cửa, máy móc cần item) ─────────
        var questReq = hitObject.GetComponentInParent<IQuestRequirement>();
        if (questReq != null)
        {
            string prompt = questReq.GetPrompt();
            if (prompt != null) InteractionUIManager.Instance.ShowPrompt(prompt);
            return prompt != null;
        }

        // ── IInteractable (quest items khác) ──────────────────
        var interactable = hitObject.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            string prompt = interactable.GetPrompt();
            if (prompt != null) InteractionUIManager.Instance.ShowPrompt(prompt);
            return prompt != null;
        }

        // ── Examinable ────────────────────────────────────────
        var examinable = hitObject.GetComponent<ExaminableObject>();
        if (examinable != null)
        {
            InteractionUIManager.Instance.ShowPrompt($"{fBtn} Đọc <color=#{hexItem}>{examinable.objectName}</color>");
            return true;
        }

        // ── Panel zone ────────────────────────────────────────
        var panelZone = hitObject.GetComponentInParent<PanelInteractZone>();
        if (panelZone != null)
        {
            InteractionUIManager.Instance.ShowPrompt($"{fBtn} {panelZone.enterPrompt}");
            return true;
        }

        // ── Main switch ───────────────────────────────────────
        var mainSwitch = hitObject.GetComponentInParent<MainSwitchInteractable>();
        if (mainSwitch != null)
        {
            InteractionUIManager.Instance.ShowPrompt($"{fBtn} Gạt cần điện");
            return true;
        }

        // ── WorldItem (Nhặt đồ) ────────────────────────────────
        var worldItem = hitObject.GetComponent<WorldItem>();
        if (worldItem != null && worldItem.itemData != null)
        {
            string name = worldItem.itemData is FuseItemDataSO f
                ? $"Cầu chì [{f.fuseID}]"
                : worldItem.itemData.itemName;

            string qtyText = worldItem.quantity > 1 ? $" (x{worldItem.quantity})" : "";

            // Phím [F] lấy màu _keyColor, Tên item lấy màu _itemColor
            InteractionUIManager.Instance.ShowPrompt($"{fBtn} Nhặt <color=#{hexItem}>{name}{qtyText}</color>");
            return true;
        }

        // ── Electrical key ────────────────────────────────────
        if (hitObject.CompareTag("ElectricalKey"))
        {
            InteractionUIManager.Instance.ShowPrompt($"{fBtn} Lấy chìa khóa");
            return true;
        }

        return false;
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

        // ── IQuestRequirement ─────────────────────────────────
        var questReq = _currentTarget.GetComponentInParent<IQuestRequirement>();
        if (questReq != null)
        {
            if (inv != null) questReq.TryUseItem(inv);
            ClearCurrentTarget();
            return;
        }

        // ── IInteractable ─────────────────────────────────────
        var interactable = _currentTarget.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            if (inv != null) interactable.TryInteract(inv);
            ClearCurrentTarget();
            return;
        }

        // ── Examinable ────────────────────────────────────────
        var examinable = _currentTarget.GetComponent<ExaminableObject>();
        if (examinable != null)
        {
            ExamineUIController.Instance?.OpenExamine(examinable);
            return;
        }

        // ── Panel zone ────────────────────────────────────────
        var panelZone = _currentTarget.GetComponentInParent<PanelInteractZone>();
        if (panelZone != null)
        {
            _activePanelZone = panelZone;
            panelZone.TogglePanelMode();
            ClearCurrentTarget();
            return;
        }

        // ── Main switch ───────────────────────────────────────
        var mainSwitch = _currentTarget.GetComponentInParent<MainSwitchInteractable>();
        if (mainSwitch != null)
        {
            mainSwitch.Interact();
            return;
        }

        // ── WorldItem ─────────────────────────────────────────
        var worldItem = _currentTarget.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            PerformPickup();
            return;
        }

        // ── Electrical key (pickup & store flag) ──────────────
        if (_currentTarget.CompareTag("ElectricalKey"))
        {
            PickupElectricalKey();
            return;
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
        int initialQty = worldItem.quantity;

        ClearCurrentTarget();

        // 1. NHẶT VÀ LẤY SỐ LƯỢNG DƯ
        int leftover = initialQty;
        if (InventorySystem.Instance != null)
        {
            leftover = InventorySystem.Instance.PickupItem(data, initialQty);
        }

        // Tính số lượng thực tế đã nhét vào Balo
        int pickedAmount = initialQty - leftover;

        // 2. XỬ LÝ LOGIC HIỂN THỊ VÀ HỘP ĐẠN
        if (pickedAmount > 0)
        {
            OnItemPickedUp?.Raise();

            if (leftover <= 0)
            {
                // Trường hợp 1: Nhặt sạch sẽ -> Xóa hộp đạn
                itemToPickUp.GetComponent<Collider>().enabled = false;
                Destroy(itemToPickUp);

                if (NotificationUI.Instance != null)
                    NotificationUI.Instance.ShowNotification($"Đã nhặt {data.itemName} x{pickedAmount}");

                Debug.Log($"[PlayerInteraction] Nhặt sạch: {data.itemName} x{pickedAmount}");
            }
            else
            {
                // Trường hợp 2: Balo đầy giữa chừng -> Chỉ nhặt 1 phần, cập nhật số dư cho hộp đạn
                worldItem.quantity = leftover;

                if (NotificationUI.Instance != null)
                    NotificationUI.Instance.ShowNotification($"Nhặt {pickedAmount}. Balo đầy, bỏ lại {leftover}!");

                Debug.Log($"[PlayerInteraction] Nhặt {pickedAmount}, dư lại {leftover} viên trên mặt đất.");
            }
        }
        else
        {
            // Trường hợp 3: Balo đầy cứng không nhét nổi viên nào
            if (NotificationUI.Instance != null)
                NotificationUI.Instance.ShowNotification("Balo đã đầy cứng!");

            Debug.Log("[PlayerInteraction] Balo đầy, không nhặt được viên nào!");
        }

        PlayerState.Instance?.SetPickingUp(false);
    }

    void PickupElectricalKey()
    {
        if (_currentTarget == null) return;

        _currentTarget.GetComponent<Collider>().enabled = false;
        Destroy(_currentTarget);
        ClearCurrentTarget();

        Debug.Log("[PlayerInteraction] Đã nhặt chìa khóa điện!");
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