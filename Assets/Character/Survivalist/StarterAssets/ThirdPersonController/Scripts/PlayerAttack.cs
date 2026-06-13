using UnityEngine;
using StarterAssets;

public class PlayerAttack : MonoBehaviour
{
    [Header("Fallback tay không")]
    [SerializeField] private WeaponDataSO _unarmedData;
    [Header("Zombie Layer")]
    [SerializeField] private LayerMask _zombieLayer;
    [Header("Events")]
    [SerializeField] private GameEventSO OnWeaponFired;

    private Animator _animator;
    private StarterAssetsInputs _input;

    private int _punchComboIndex = 0;
    private float _lastPunchTime = 0f;
    private float _nextAttackTime = 0f;
    private float _currentDamage = 0f;

    private int _hashWeaponAttack;
    private int _hashPunch;
    private int _hashPunchIndex;
    private int _paramWeaponType;

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _input = GetComponent<StarterAssetsInputs>();

        _hashWeaponAttack = Animator.StringToHash("WeaponAttack");
        _hashPunch = Animator.StringToHash("Punch");
        _hashPunchIndex = Animator.StringToHash("PunchIndex");
        _paramWeaponType = Animator.StringToHash("WeaponType");

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHeldItemChanged += OnHeldItemChangedCallback;
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnHeldItemChanged -= OnHeldItemChangedCallback;
    }

    WeaponDataSO CurrentWeapon()
    {
        if (InventorySystem.Instance == null) return _unarmedData;
        ItemDataSO heldItem = InventorySystem.Instance.GetHeldItem();
        if (heldItem != null && heldItem is WeaponDataSO weapon) return weapon;
        return _unarmedData;
    }

    void Update()
    {
        var current = CurrentWeapon();
        if (current != null && Time.time - _lastPunchTime > current.comboResetTime)
            _punchComboIndex = 0;

        if (_input != null && _input.shoot)
        {
            TryAttack();
        }
    }

    void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;
        if (PlayerState.Instance != null && !PlayerState.Instance.CanAttack()) return;

        // BẢO VỆ TUYỆT ĐỐI: Nếu Animator đang là dáng súng (1 = Rifle, 2 = Pistol) -> NGỪNG!
        int currentAnimWeaponType = _animator != null ? _animator.GetInteger(_paramWeaponType) : 0;
        if (currentAnimWeaponType == 1 || currentAnimWeaponType == 2)
        {
            return; // Trả quyền cho PlayerGunAnimator bắn đạn
        }

        var data = CurrentWeapon();
        if (data == null) data = _unarmedData;

        // Bảo vệ lớp 2: Đề phòng Inventory báo là súng
        if (data.weaponSlotType == WeaponSlotType.Rifle || data.weaponSlotType == WeaponSlotType.PistolOrShotgun)
        {
            return;
        }

        // CHỈ CHẠY XUỐNG ĐÂY KHI LÀ CẬN CHIẾN HOẶC TAY KHÔNG
        _currentDamage = data.damage;
        _nextAttackTime = Time.time + data.cooldown;

        bool isMelee = (data.weaponSlotType == WeaponSlotType.Melee || currentAnimWeaponType == 3);
        if (isMelee) PerformWeaponAttack(data);
        else PerformPunch(data);

        OnWeaponFired?.Raise();
        _input.shoot = false; // Bắt buộc phải click lại cho nhát chém tiếp theo
    }

    void PerformPunch(WeaponDataSO data)
    {
        if (_animator == null) return;
        _animator.SetInteger(_hashPunchIndex, _punchComboIndex);
        _animator.SetTrigger(_hashPunch);
        _punchComboIndex = (_punchComboIndex + 1) % Mathf.Max(1, data.comboSteps);
        _lastPunchTime = Time.time;
    }

    void PerformWeaponAttack(WeaponDataSO data)
    {
        if (_animator == null) return;
        _animator.SetTrigger(_hashWeaponAttack);
    }

    public void OnMeleeHit()
    {
        var data = CurrentWeapon();
        if (data == null) data = _unarmedData;

        Vector3 hitCenter = transform.position + transform.forward * data.hitDistance + Vector3.up * data.hitHeight;
        Collider[] hits = Physics.OverlapSphere(hitCenter, data.hitRadius, _zombieLayer);

        if (hits.Length == 0) return;

        foreach (var hit in hits)
        {
            var zombie = hit.GetComponent<ZombieBase>() ?? hit.GetComponentInParent<ZombieBase>();
            if (zombie == null) continue;

            zombie.TakeDamage(_currentDamage, gameObject);

            Vector3 hitPoint = hit.ClosestPoint(hitCenter);
            Vector3 hitNormal = (hitPoint - transform.position).normalized;
            hit.GetComponentInParent<ZombieBloodFXHandler>()?.OnHitMelee(hitPoint, hitNormal);
            break;
        }
    }

    void OnHeldItemChangedCallback(ItemDataSO item)
    {
        _nextAttackTime = Time.time + 0.5f;
        _punchComboIndex = 0;
        if (_input != null) _input.shoot = false;

        if (_animator != null)
        {
            _animator.ResetTrigger(_hashWeaponAttack);
            _animator.ResetTrigger(_hashPunch);

            if (item != null)
            {
                if (item is WeaponDataSO weapon)
                {
                    int poseID = weapon.weaponSlotType switch
                    {
                        WeaponSlotType.Rifle => 1,
                        WeaponSlotType.PistolOrShotgun => 2,
                        WeaponSlotType.Melee => 3,
                        _ => 0
                    };
                    _animator.SetInteger(_paramWeaponType, poseID);
                }
                else if (item.category == ItemCategory.QuestItem)
                {
                    _animator.SetInteger(_paramWeaponType, 5);
                }
                else _animator.SetInteger(_paramWeaponType, 0);
            }
            else _animator.SetInteger(_paramWeaponType, 0);
        }
    }

    void OnDrawGizmosSelected()
    {
        var data = Application.isPlaying ? CurrentWeapon() : _unarmedData;
        if (data == null) return;
        Gizmos.color = Color.red;
        Vector3 hitCenter = transform.position + transform.forward * data.hitDistance + Vector3.up * data.hitHeight;
        Gizmos.DrawWireSphere(hitCenter, data.hitRadius);
    }
}