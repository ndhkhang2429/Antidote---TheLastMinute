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

    [Header("Interaction Audio")]
    [SerializeField] private AudioSource _interactionAudioSource;
    [SerializeField] private AudioClip[] _pickupClips;
    [SerializeField] private AudioClip _electricalKeyPickupClip;

    [SerializeField, Range(0f, 1f)]
    private float _pickupVolume = 0.65f;

    [SerializeField]
    private Vector2 _pickupPitchRange =
        new Vector2(0.97f, 1.03f);

    [Header("Tùy chỉnh UI Tương tác")]
    [SerializeField] private string _interactKey = "[F]";

    [SerializeField]
    private Color _keyColor =
        new Color(1f, 0.8f, 0f);

    [SerializeField]
    private Color _itemColor =
        new Color(0.6f, 0.6f, 0.6f);

    private Animator _animator;
    private ThirdPersonController _tpc;

    private GameObject _currentTarget;
    private PanelInteractZone _activePanelZone;

    private int _paramPickUp;
    private int _paramWeaponType;

    [Header("Interaction Distance Safety")]
    [SerializeField, Min(0.5f)]
    private float _maximumInteractionDistance = 3f;

    private float InteractionRadius
    {
        get
        {
            float statsDistance =
                _statsSO != null
                    ? _statsSO.interactionRadius
                    : _maximumInteractionDistance;

            return Mathf.Min(
                statsDistance,
                _maximumInteractionDistance
            );
        }
    }

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _tpc = GetComponent<ThirdPersonController>();

        FindMainCamera();

        _paramPickUp =
            Animator.StringToHash("PickUp");

        _paramWeaponType =
            Animator.StringToHash("WeaponType");

        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnWeaponChanged +=
                SyncAnimatorWeaponType;
        }

        ClearCurrentTarget();
    }

    private void OnEnable()
    {
        /*
         * Xóa prompt còn sót lại trước khi hệ thống
         * tương tác bắt đầu hoạt động trở lại.
         */
        ClearCurrentTarget();
    }

    private void OnDisable()
    {
        /*
         * Khi cutscene khóa PlayerInteraction,
         * xóa ngay target, highlight và prompt.
         */
        ClearCurrentTarget();
        _activePanelZone = null;
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnWeaponChanged -=
                SyncAnimatorWeaponType;
        }

        ClearCurrentTarget();
    }

    private void Update()
    {
        if (ExamineUIController.Instance != null &&
            ExamineUIController.Instance.IsExamining)
        {
            if (Input.GetKeyDown(KeyCode.F) ||
                Input.GetKeyDown(KeyCode.Escape))
            {
                ExamineUIController.Instance.CloseExamine();
            }

            return;
        }

        if (_activePanelZone != null &&
            _activePanelZone.IsInPanelMode)
        {
            if (Input.GetKeyDown(KeyCode.F) ||
                Input.GetKeyDown(KeyCode.Escape))
            {
                _activePanelZone.TogglePanelMode();
            }

            return;
        }

        HandleRaycast();

        if (_currentTarget != null &&
            Input.GetKeyDown(KeyCode.F))
        {
            if (_tpc != null)
                _tpc.SmoothFaceCameraDirection();

            InteractWithCurrentTarget();
        }

        if (Input.GetKeyDown(KeyCode.G))
            DropCurrentItem();
    }

    // ─────────────────────────────────────────────────────
    // Camera
    // ─────────────────────────────────────────────────────

    private bool FindMainCamera()
    {
        if (_mainCamera != null)
            return true;

        _mainCamera = Camera.main;

        return _mainCamera != null;
    }

    // ─────────────────────────────────────────────────────
    // Raycast
    // ─────────────────────────────────────────────────────

    private void HandleRaycast()
    {
        if (!FindMainCamera())
        {
            ClearCurrentTarget();
            return;
        }

        Ray ray = new Ray(
            _mainCamera.transform.position,
            _mainCamera.transform.forward
        );

        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            InteractionRadius,
            _interactLayer,
            QueryTriggerInteraction.Collide
        );

        if (!hitSomething)
        {
            ClearCurrentTarget();
            return;
        }

        GameObject hitObject = hit.collider.gameObject;

        /*
         * Nếu vẫn đang nhìn đúng collider cũ thì giữ prompt,
         * không cần tạo lại mỗi frame.
         */
        if (hitObject == _currentTarget)
            return;

        Debug.Log(
            $"[Interaction Raycast] Hit: {hit.collider.name} | " +
            $"Distance: {hit.distance:F2} | " +
            $"Root: {hit.collider.transform.root.name}",
            hit.collider
        );

        ClearCurrentTarget();

        bool hasPrompt = ShowPromptForTarget(hitObject);

        if (!hasPrompt)
        {
            ClearCurrentTarget();
            return;
        }

        _currentTarget = hitObject;

        _currentTarget
            .GetComponentInParent<ItemHighlight>()
            ?.ToggleHighlight(true);
    }

    private bool ShowPromptForTarget(
        GameObject hitObject)
    {
        if (hitObject == null)
            return false;

        if (InteractionUIManager.Instance == null)
            return false;

        string hexKey =
            ColorUtility.ToHtmlStringRGB(_keyColor);

        string hexItem =
            ColorUtility.ToHtmlStringRGB(_itemColor);

        string fButton =
            $"<b><color=#{hexKey}>{_interactKey}</color></b>";

        // ── Cửa hoặc máy móc cần Quest Item ──────────────

        IQuestRequirement questRequirement =
            hitObject.GetComponentInParent<IQuestRequirement>();

        if (questRequirement != null)
        {
            string prompt =
                questRequirement.GetPrompt();

            if (!string.IsNullOrEmpty(prompt))
            {
                InteractionUIManager.Instance
                    .ShowPrompt(prompt);

                return true;
            }

            return false;
        }

        // ── Các object triển khai IInteractable ───────────

        IInteractable interactable =
            hitObject.GetComponentInParent<IInteractable>();

        if (interactable != null)
        {
            string prompt =
                interactable.GetPrompt();

            if (!string.IsNullOrEmpty(prompt))
            {
                InteractionUIManager.Instance
                    .ShowPrompt(prompt);

                return true;
            }

            return false;
        }

        // ── Tài liệu có thể đọc ───────────────────────────

        ExaminableObject examinable =
            hitObject.GetComponentInParent<ExaminableObject>();

        if (examinable != null)
        {
            InteractionUIManager.Instance.ShowPrompt(
                $"{fButton} Read " +
                $"<color=#{hexItem}>" +
                $"{examinable.objectName}</color>"
            );

            return true;
        }

        // ── Panel zone ────────────────────────────────────

        PanelInteractZone panelZone =
            hitObject.GetComponentInParent<PanelInteractZone>();

        if (panelZone != null)
        {
            InteractionUIManager.Instance.ShowPrompt(
                $"{fButton} {panelZone.enterPrompt}"
            );

            return true;
        }

        // ── Công tắc điện chính ───────────────────────────

        MainSwitchInteractable mainSwitch =
            hitObject.GetComponentInParent
                <MainSwitchInteractable>();

        if (mainSwitch != null)
        {
            InteractionUIManager.Instance.ShowPrompt(
                $"{fButton} Flip the electric lever."
            );

            return true;
        }

        // ── Item ngoài thế giới ───────────────────────────

        WorldItem worldItem =
            hitObject.GetComponentInParent<WorldItem>();

        if (worldItem != null &&
            worldItem.itemData != null)
        {
            string itemName =
                worldItem.itemData is FuseItemDataSO fuse
                    ? $"Cầu chì [{fuse.fuseID}]"
                    : worldItem.itemData.itemName;

            string quantityText =
                worldItem.quantity > 1
                    ? $" (x{worldItem.quantity})"
                    : "";

            InteractionUIManager.Instance.ShowPrompt(
                $"{fButton} Pick " +
                $"<color=#{hexItem}>" +
                $"{itemName}{quantityText}</color>"
            );

            return true;
        }

        // ── Chìa khóa điện ────────────────────────────────

        if (hitObject.CompareTag("ElectricalKey"))
        {
            InteractionUIManager.Instance.ShowPrompt(
                $"{fButton} Pick electrical key"
            );

            return true;
        }

        return false;
    }

    private void ClearCurrentTarget()
    {
        if (_currentTarget != null)
        {
            _currentTarget
                .GetComponentInParent<ItemHighlight>()
                ?.ToggleHighlight(false);

            _currentTarget = null;
        }

        InteractionUIManager.Instance?.HidePrompt();
    }

    // ─────────────────────────────────────────────────────
    // Interaction
    // ─────────────────────────────────────────────────────

    private void InteractWithCurrentTarget()
    {
        if (_currentTarget == null)
            return;

        InventorySystem inventory =
            InventorySystem.Instance;

        // ── Quest Requirement ─────────────────────────────

        IQuestRequirement questRequirement =
            _currentTarget
                .GetComponentInParent<IQuestRequirement>();

        if (questRequirement != null)
        {
            if (inventory != null)
                questRequirement.TryUseItem(inventory);

            ClearCurrentTarget();
            return;
        }

        // ── IInteractable ─────────────────────────────────

        IInteractable interactable =
            _currentTarget
                .GetComponentInParent<IInteractable>();

        if (interactable != null)
        {
            if (inventory != null)
                interactable.TryInteract(inventory);

            ClearCurrentTarget();
            return;
        }

        // ── Examinable ────────────────────────────────────

        ExaminableObject examinable =
            _currentTarget
                .GetComponentInParent<ExaminableObject>();

        if (examinable != null)
        {
            ExamineUIController.Instance
                ?.OpenExamine(examinable);

            ClearCurrentTarget();
            return;
        }

        // ── Panel zone ────────────────────────────────────

        PanelInteractZone panelZone =
            _currentTarget
                .GetComponentInParent<PanelInteractZone>();

        if (panelZone != null)
        {
            _activePanelZone = panelZone;
            panelZone.TogglePanelMode();

            ClearCurrentTarget();
            return;
        }

        // ── Main switch ───────────────────────────────────

        MainSwitchInteractable mainSwitch =
            _currentTarget
                .GetComponentInParent
                    <MainSwitchInteractable>();

        if (mainSwitch != null)
        {
            mainSwitch.Interact();
            ClearCurrentTarget();
            return;
        }

        // ── WorldItem ─────────────────────────────────────

        WorldItem worldItem =
            _currentTarget
                .GetComponentInParent<WorldItem>();

        if (worldItem != null)
        {
            /*
             * Đưa đúng GameObject chứa WorldItem vào target
             * để EquipItem lấy component chính xác.
             */
            _currentTarget = worldItem.gameObject;

            PerformPickup();
            return;
        }

        // ── Electrical key ────────────────────────────────

        if (_currentTarget.CompareTag("ElectricalKey"))
        {
            PickupElectricalKey();
        }
    }

    // ─────────────────────────────────────────────────────
    // Pickup
    // ─────────────────────────────────────────────────────

    private void PerformPickup()
    {
        if (_animator == null ||
            _currentTarget == null)
        {
            return;
        }

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

        WorldItem worldItem =
            itemToPickUp.GetComponent<WorldItem>();

        if (worldItem == null ||
            worldItem.itemData == null)
        {
            Debug.LogWarning(
                "[PlayerInteraction] Không tìm thấy WorldItem!"
            );

            PlayerState.Instance?.SetPickingUp(false);
            return;
        }

        ItemDataSO itemData = worldItem.itemData;
        int initialQuantity = worldItem.quantity;
        int leftover = initialQuantity;

        ClearCurrentTarget();

        if (InventorySystem.Instance != null)
        {
            leftover =
                InventorySystem.Instance.PickupItem(
                    itemData,
                    initialQuantity
                );
        }

        int pickedAmount =
            initialQuantity - leftover;

        if (pickedAmount > 0)
        {
            OnItemPickedUp?.Raise();
            PlayPickupSound();

            if (leftover <= 0)
            {
                Collider itemCollider =
                    itemToPickUp.GetComponent<Collider>();

                if (itemCollider != null)
                    itemCollider.enabled = false;

                if (NotificationUI.Instance != null)
                {
                    NotificationUI.Instance.ShowNotification(
                        $"Picked {itemData.itemName} " +
                        $"x{pickedAmount}"
                    );
                }

                worldItem.TriggerPickedUp();

                OpeningTutorialHints tutorial =
                    FindObjectOfType<OpeningTutorialHints>();

                tutorial?.NotifyItemPickedUp(itemData);

                Destroy(itemToPickUp);

                Debug.Log(
                    "[PlayerInteraction] Nhặt sạch: " +
                    $"{itemData.itemName} x{pickedAmount}"
                );
            }
            else
            {
                worldItem.quantity = leftover;

                if (NotificationUI.Instance != null)
                {
                    NotificationUI.Instance.ShowNotification(
                        $"Pick {pickedAmount}. " +
                        $"Balo đầy, bỏ lại {leftover}!"
                    );
                }

                Debug.Log(
                    $"[PlayerInteraction] Nhặt {pickedAmount}, " +
                    $"dư lại {leftover} trên mặt đất."
                );
            }
        }
        else
        {
            if (NotificationUI.Instance != null)
            {
                NotificationUI.Instance.ShowNotification(
                    "Balo fulled!"
                );
            }

            Debug.Log(
                "[PlayerInteraction] Balo đầy, " +
                "không nhặt được item nào!"
            );
        }

        PlayerState.Instance?.SetPickingUp(false);
    }

    private void PickupElectricalKey()
    {
        if (_currentTarget == null)
            return;

        GameObject electricalKey = _currentTarget;

        PlayInteractionSound(
            _electricalKeyPickupClip,
            _pickupVolume
        );

        Collider keyCollider =
            electricalKey.GetComponent<Collider>();

        if (keyCollider != null)
            keyCollider.enabled = false;

        ClearCurrentTarget();
        Destroy(electricalKey);

        Debug.Log(
            "[PlayerInteraction] Đã nhặt chìa khóa điện!"
        );
    }

    // ─────────────────────────────────────────────────────
    // Drop
    // ─────────────────────────────────────────────────────

    private void DropCurrentItem()
    {
        if (PlayerState.Instance?.CurrentItemInHand == null)
            return;

        PlayerState.Instance.DropCurrentItem();
        OnItemDropped?.Raise();
    }

    // ─────────────────────────────────────────────────────
    // Animator
    // ─────────────────────────────────────────────────────

    private void SyncAnimatorWeaponType(int weaponType)
    {
        if (_animator != null)
        {
            _animator.SetInteger(
                _paramWeaponType,
                weaponType
            );
        }
    }

    // ─────────────────────────────────────────────────────
    // Audio
    // ─────────────────────────────────────────────────────

    private void PlayInteractionSound(
        AudioClip clip,
        float volume)
    {
        if (_interactionAudioSource == null ||
            clip == null)
        {
            return;
        }

        _interactionAudioSource.pitch =
            Random.Range(
                _pickupPitchRange.x,
                _pickupPitchRange.y
            );

        _interactionAudioSource.PlayOneShot(
            clip,
            volume
        );
    }

    private void PlayPickupSound()
    {
        if (_pickupClips == null ||
            _pickupClips.Length == 0)
        {
            return;
        }

        AudioClip clip =
            _pickupClips[
                Random.Range(0, _pickupClips.Length)
            ];

        PlayInteractionSound(
            clip,
            _pickupVolume
        );
    }

    // ─────────────────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Camera cameraToUse =
            _mainCamera != null
                ? _mainCamera
                : Camera.main;

        if (cameraToUse == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawRay(
            cameraToUse.transform.position,
            cameraToUse.transform.forward *
            InteractionRadius
        );
    }
}