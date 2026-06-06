using UnityEngine;

/// <summary>
/// NGUỒN SỰ THẬT DUY NHẤT cho trạng thái player.
/// Liên kết trực tiếp với InventorySystem.
/// </summary>
public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; private set; }

    // ── Animator Param IDs ────────────────────────────────
    private int _paramWeaponType;
    private int _paramIsHoldingItem;

    // ── Refs ──────────────────────────────────────────────
    private Animator _animator;

    // ── State ─────────────────────────────────────────────
    public bool IsAttacking { get; private set; } = false;
    public bool IsPickingUp { get; private set; } = false;

    // Giữ lại để không lỗi tham chiếu cũ
    public GameObject CurrentItemInHand { get; private set; } = null;

    // ── Events ────────────────────────────────────────────
    public event System.Action<int> OnWeaponChanged;
    public event System.Action OnItemDropped;

    // ── WeaponType map ────────────────────────────────────
    // 0 = Tay không (đánh tay)
    // 1 = Melee (gậy, rìu...)
    // 2 = Pistol/Shotgun
    // 3 = Rifle
    // 4 = Grenade
    // 5 = Cầm item (slot 5) — KHÔNG đánh, chỉ cầm
    public int WeaponType { get; private set; } = 0;

    // ── Lifecycle ─────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();

        _paramWeaponType = Animator.StringToHash("WeaponType");
        _paramIsHoldingItem = Animator.StringToHash("IsHoldingItem");

        // Lắng nghe InventorySystem
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnActiveSlotChanged += OnActiveSlotChanged;
            InventorySystem.Instance.OnHeldItemChanged += OnHeldItemChanged;
        }
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance == null) return;
        InventorySystem.Instance.OnActiveSlotChanged -= OnActiveSlotChanged;
        InventorySystem.Instance.OnHeldItemChanged -= OnHeldItemChanged;
    }

    // ── Callback từ InventorySystem ───────────────────────

    void OnActiveSlotChanged(int slotIndex)
    {
        switch (slotIndex)
        {
            case -1:
                // Tay không — đánh tay
                SetWeaponType(0);
                break;

            case 0:
                // Slot 1 — Pistol/Shotgun
                SetWeaponType(2);
                break;

            case 1:
                // Slot 2 — Rifle
                SetWeaponType(3);
                break;

            case 2:
                // Slot 3 — Melee
                SetWeaponType(1);
                break;

            case 3:
                // Slot 4 — Grenade
                SetWeaponType(4);
                break;

            case 4:
                // Slot 5 — Cầm item, KHÔNG phải vũ khí
                SetWeaponType(5);
                break;
        }
    }

    void OnHeldItemChanged(ItemDataSO item)
    {
        // Cập nhật IsHoldingItem cho animator
        bool holdingItem = item != null
                        && InventorySystem.Instance.activeSlot == 4;

        if (_animator != null)
            _animator.SetBool(_paramIsHoldingItem, holdingItem);

        Debug.Log($"[PlayerState] HeldItem: {item?.itemName ?? "None"} | IsHoldingItem: {holdingItem}");
    }

    // ── Weapon Type ───────────────────────────────────────
    void SetWeaponType(int type)
    {
        WeaponType = type;

        if (_animator != null)
        {
            _animator.SetInteger(_paramWeaponType, type);

            // Tắt IsHoldingItem nếu không phải slot 5
            if (type != 5)
                _animator.SetBool(_paramIsHoldingItem, false);
        }

        OnWeaponChanged?.Invoke(type);
        Debug.Log($"[PlayerState] WeaponType: {type} ({GetWeaponTypeName(type)})");
    }

    string GetWeaponTypeName(int type) => type switch
    {
        0 => "Tay không",
        1 => "Melee",
        2 => "Pistol/Shotgun",
        3 => "Rifle",
        4 => "Grenade",
        5 => "Cầm item",
        _ => "Unknown"
    };

    // ── API giữ lại để không lỗi tham chiếu cũ ───────────

    /// <summary>Dùng khi cần force set weapon type từ script khác</summary>
    public void EquipWeapon(int weaponType, GameObject itemObject)
    {
        CurrentItemInHand = itemObject;
        SetWeaponType(weaponType);
    }

    public void DropCurrentItem()
    {
        if (CurrentItemInHand != null)
        {
            CurrentItemInHand.transform.SetParent(null);
            var rb = CurrentItemInHand.GetComponent<Rigidbody>();
            var col = CurrentItemInHand.GetComponent<Collider>();
            if (rb != null) rb.isKinematic = false;
            if (col != null) col.enabled = true;
            CurrentItemInHand = null;
        }

        InventorySystem.Instance?.DeselectAll();
        OnItemDropped?.Invoke();
    }

    // ── Combat API ────────────────────────────────────────
    public void SetAttacking(bool value) => IsAttacking = value;
    public void SetPickingUp(bool value) => IsPickingUp = value;

    /// <summary>
    /// Kiểm tra player có thể tấn công không.
    /// Chỉ cho tấn công khi WeaponType != 5 (không phải đang cầm item)
    /// </summary>
    public bool CanAttack()
    {
        // Slot 5 (cầm item) → không cho tấn công
        if (WeaponType == 5) return false;

        // Các slot khác → cho tấn công
        return true;
    }
}