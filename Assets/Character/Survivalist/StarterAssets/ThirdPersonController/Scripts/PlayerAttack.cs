using UnityEngine;
using StarterAssets;

public class PlayerAttack : MonoBehaviour
{
    [Header("Fallback khi chưa có vũ khí trong Inventory")]
    [SerializeField] private WeaponDataSO _unarmedData;

    [Header("Zombie Layer")]
    [SerializeField] private LayerMask _zombieLayer;

    [Header("Events")]
    [SerializeField] private GameEventSO OnWeaponFired;

    // ── Private refs ───────────────────────────────────────
    private Animator _animator;
    private StarterAssetsInputs _input;

    // ── Combo state ────────────────────────────────────────
    private int _punchComboIndex = 0;
    private float _lastPunchTime = 0f;

    // ── Cooldown ───────────────────────────────────────────
    private float _nextAttackTime = 0f;
    private float _currentDamage = 0f;

    // ── Animator hashes ────────────────────────────────────
    private int _hashWeaponAttack;
    private int _hashPunch;
    private int _hashPunchIndex;

    // ─────────────────────────────────────────────────────
    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _input = GetComponent<StarterAssetsInputs>();

        if (_animator == null) Debug.LogError("[PlayerAttack] Thiếu Animator!");
        if (_input == null) Debug.LogError("[PlayerAttack] Thiếu StarterAssetsInputs!");
        if (_unarmedData == null) Debug.LogError("[PlayerAttack] Chưa gán _unarmedData!");

        _hashWeaponAttack = Animator.StringToHash("WeaponAttack");
        _hashPunch = Animator.StringToHash("Punch");
        _hashPunchIndex = Animator.StringToHash("PunchIndex");

        // Lắng nghe khi trang bị vũ khí mới từ Inventory
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnWeaponEquipped += OnWeaponEquipped;
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnWeaponEquipped -= OnWeaponEquipped;
    }

    // ── Vũ khí hiện tại lấy từ Inventory ─────────────────
    // Ưu tiên: Melee slot (index 2) → Pistol slot (index 0) → unarmed
    WeaponDataSO CurrentWeapon()
    {
        if (InventorySystem.Instance == null) return _unarmedData;

        var slots = InventorySystem.Instance.weaponSlots;

        // Ô 2 = cận chiến
        if (!slots[2].IsEmpty && slots[2].item is WeaponDataSO melee)
            return melee;

        // Ô 0 = súng lục/shotgun (dùng khi có)
        if (!slots[0].IsEmpty && slots[0].item is WeaponDataSO pistol)
            return pistol;

        return _unarmedData;
    }

    // ── Update ────────────────────────────────────────────
    void Update()
    {
        var current = CurrentWeapon();
        if (current != null && Time.time - _lastPunchTime > current.comboResetTime)
            _punchComboIndex = 0;

        if (_input != null && _input.shoot)
        {
            _input.shoot = false;

            // Chặn tấn công khi đang cầm item (slot 5)
            if (PlayerState.Instance != null && !PlayerState.Instance.CanAttack())
            {
                Debug.Log("[PlayerAttack] Đang cầm item, không thể tấn công!");
                return;
            }

            TryAttack();
        }
    }

    // ── Logic tấn công ────────────────────────────────────
    void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        // Chặn khi đang cầm item slot 5
        if (PlayerState.Instance != null && !PlayerState.Instance.CanAttack())
        {
            Debug.Log("[PlayerAttack] Đang cầm item, không thể tấn công!");
            return;
        }

        var data = CurrentWeapon();
        if (data == null) return;

        _currentDamage = data.damage;
        _nextAttackTime = Time.time + data.cooldown;

        bool isMelee = data.weaponSlotType == WeaponSlotType.Melee;
        if (isMelee) PerformWeaponAttack(data);
        else PerformPunch(data);

        OnWeaponFired?.Raise();
    }

    void PerformPunch(WeaponDataSO data)
    {
        if (_animator == null) return;

        _animator.SetInteger(_hashPunchIndex, _punchComboIndex);
        _animator.SetTrigger(_hashPunch);

        _punchComboIndex = (_punchComboIndex + 1) % Mathf.Max(1, data.comboSteps);
        _lastPunchTime = Time.time;

        Debug.Log($"[PlayerAttack] Punch combo {_punchComboIndex}");
    }

    void PerformWeaponAttack(WeaponDataSO data)
    {
        if (_animator == null) return;
        _animator.SetTrigger(_hashWeaponAttack);
        Debug.Log($"[PlayerAttack] Tấn công: {data.itemName}");
    }

    // ── Animation Event ───────────────────────────────────
    /// <summary>Gắn vào đúng frame trong Animation Clip</summary>
    public void OnMeleeHit()
    {
        var data = CurrentWeapon();
        if (data == null) return;

        Vector3 hitCenter = transform.position
                          + transform.forward * data.hitDistance
                          + Vector3.up * data.hitHeight;

        Collider[] hits = Physics.OverlapSphere(hitCenter, data.hitRadius, _zombieLayer);
        if (hits.Length == 0) return;

        foreach (var hit in hits)
        {
            var zombie = hit.GetComponent<ZombieBase>()
                      ?? hit.GetComponentInParent<ZombieBase>();

            if (zombie == null) continue;

            zombie.TakeDamage(_currentDamage, gameObject);

            Vector3 hitPoint = hit.ClosestPoint(hitCenter);
            Vector3 hitNormal = (hitPoint - transform.position).normalized;
            hit.GetComponentInParent<ZombieBloodFXHandler>()
               ?.OnHitMelee(hitPoint, hitNormal);

            break; // chỉ hit 1 zombie gần nhất
        }
    }

    // ── Callback khi Inventory trang bị vũ khí mới ───────
    void OnWeaponEquipped(ItemDataSO item)
    {
        // Reset cooldown để dùng vũ khí mới ngay
        _nextAttackTime = 0f;
        _punchComboIndex = 0;
        Debug.Log($"[PlayerAttack] Trang bị vũ khí mới: {item.itemName}");
    }

    // ── Gizmos ────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        var data = Application.isPlaying ? CurrentWeapon() : _unarmedData;
        if (data == null) return;

        Gizmos.color = Color.red;
        Vector3 hitCenter = transform.position
                          + transform.forward * data.hitDistance
                          + Vector3.up * data.hitHeight;
        Gizmos.DrawWireSphere(hitCenter, data.hitRadius);
    }
}