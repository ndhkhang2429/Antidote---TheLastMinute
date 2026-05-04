using UnityEngine;

/// <summary>
/// NGUỒN SỰ THẬT DUY NHẤT cho trạng thái player.
/// Các script khác đọc/ghi qua đây, KHÔNG đọc thẳng từ Animator.
/// </summary>
public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; private set; }

    // ── Weapon ──────────────────────────────────────────────
    // 0 = Tay không | 1 = Gậy/2 tay | 2 = Pistol | 3 = Rifle | 4 = Grenade
    public int WeaponType { get; private set; } = 0;
    public GameObject CurrentItemInHand { get; private set; } = null;

    // ── Combat ──────────────────────────────────────────────
    public bool IsAttacking { get; private set; } = false;
    public bool IsPickingUp { get; private set; } = false;

    // ── Events (các script khác subscribe để phản ứng) ──────
    public event System.Action<int> OnWeaponChanged;   // tham số: WeaponType mới
    public event System.Action OnItemDropped;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── Weapon API ───────────────────────────────────────────

    public void EquipWeapon(int weaponType, GameObject itemObject)
    {
        // Vứt đồ cũ nếu có
        if (CurrentItemInHand != null)
            DropCurrentItem();

        WeaponType = weaponType;
        CurrentItemInHand = itemObject;
        IsAttacking = false;

        OnWeaponChanged?.Invoke(WeaponType);
    }

    public void DropCurrentItem()
    {
        if (CurrentItemInHand != null)
        {
            CurrentItemInHand.transform.SetParent(null);
            var rb = CurrentItemInHand.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = false;
            var col = CurrentItemInHand.GetComponent<Collider>();
            if (col) col.enabled = true;

            CurrentItemInHand = null;
        }

        WeaponType = 0;
        OnWeaponChanged?.Invoke(WeaponType);
        OnItemDropped?.Invoke();
    }

    // ── Combat API ───────────────────────────────────────────

    public void SetAttacking(bool value) => IsAttacking = value;
    public void SetPickingUp(bool value) => IsPickingUp = value;
}