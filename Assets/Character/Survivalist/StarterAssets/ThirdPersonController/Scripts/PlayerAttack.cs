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
    private int _paramWeaponType;

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
        _paramWeaponType = Animator.StringToHash("WeaponType");

        // [SỬA LỖI]: Lắng nghe sự kiện OnHeldItemChanged (Khi thực sự RÚT vũ khí ra tay)
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHeldItemChanged += OnHeldItemChangedCallback;
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHeldItemChanged -= OnHeldItemChangedCallback;
    }

    // ── Vũ khí hiện tại lấy từ Inventory ─────────────────
    WeaponDataSO CurrentWeapon()
    {
        if (InventorySystem.Instance == null) return _unarmedData;

        // [SỬA LỖI]: Chỉ lấy vũ khí mà người chơi ĐANG CẦM TRÊN TAY (Active Slot)
        ItemDataSO heldItem = InventorySystem.Instance.GetHeldItem();

        if (heldItem != null && heldItem is WeaponDataSO weapon)
        {
            return weapon;
        }

        // Nếu đang không cầm gì, hoặc đồ cầm không phải vũ khí -> Trả về tay không
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

            if (PlayerState.Instance != null && !PlayerState.Instance.CanAttack())
            {
                Debug.Log("[PlayerAttack] Đang bận, không thể tấn công!");
                return;
            }

            TryAttack();
        }
    }

    // ── Logic tấn công ────────────────────────────────────
    void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        if (PlayerState.Instance != null && !PlayerState.Instance.CanAttack()) return;

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

            break;
        }
    }

    // ── Callback khi ĐỔI ĐỒ TRÊN TAY ───────────────────────
    void OnHeldItemChangedCallback(ItemDataSO item)
    {
        _nextAttackTime = Time.time + 0.5f;
        _punchComboIndex = 0;

        Debug.Log($"[PlayerAttack] Chuyển đồ trên tay: {item?.itemName ?? "Tay Không"}");

        if (_input != null) _input.shoot = false;

        if (_animator != null)
        {
            _animator.ResetTrigger(_hashWeaponAttack);
            _animator.ResetTrigger(_hashPunch);

            if (item != null)
            {
                if (item is WeaponDataSO weapon)
                {
                    int poseID = 0;
                    switch (weapon.weaponSlotType)
                    {
                        case WeaponSlotType.Rifle: poseID = 1; break;
                        case WeaponSlotType.PistolOrShotgun: poseID = 2; break;
                        case WeaponSlotType.Melee: poseID = 3; break;
                    }
                    _animator.SetInteger(_paramWeaponType, poseID);
                }
                else if (item.category == ItemCategory.QuestItem)
                {
                    // [SỬA Ở ĐÂY]: Xử lý cho Đèn pin / Item Slot 5
                    // Lưu ý: Bạn hãy mở Animator lên xem Dáng cầm đèn pin của bạn đang là số mấy (ví dụ: 4 hoặc 5), 
                    // rồi thay số 4 ở dưới đây thành số chuẩn của bạn nhé!
                    _animator.SetInteger(_paramWeaponType, 5);
                }
                else
                {
                    _animator.SetInteger(_paramWeaponType, 0);
                }
            }
            else
            {
                // Khi thực sự không cầm gì cả (item == null)
                _animator.SetInteger(_paramWeaponType, 0);
            }
        }
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