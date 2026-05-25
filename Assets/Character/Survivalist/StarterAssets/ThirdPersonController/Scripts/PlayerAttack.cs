using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

/// <summary>
/// Xử lý toàn bộ logic tấn công của player.
/// Đọc WeaponType từ PlayerState, KHÔNG đọc từ Animator.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Cài đặt Cooldown")]
    public float unarmedCooldown = 0.4f;
    public float weaponCooldown = 0.7f;  // Gậy/xà beng thường chậm hơn đấm

    [Header("Melee Damage")]
    public float unarmedDamage = 10f;   // Damage đánh tay
    public float meleeDamage = 25f;     // Damage đánh bằng gậy

    [Header("Melee Hitbox")]
    public float hitRadius = 1.2f;          // Bán kính vùng đánh
    public float hitDistance = 1.0f;        // Khoảng cách trước mặt
    public float hitHeight = 1.0f;          // Độ cao vùng đánh
    public LayerMask zombieLayer;           

    [Header("Combo Punch")]
    // PunchIndex: 0 = Punching, 1 = Punching1 (khớp với Animator)
    private int _punchComboIndex = 0;
    private float _comboResetTime = 0.8f;  // Thời gian reset combo nếu không bấm tiếp
    private float _lastPunchTime = 0f;

    // ── Private ─────────────────────────────────────────────
    private Animator _animator;
    private float _nextAttackTime = 0f;
    private float _currentDamage = 0f;

    // Animator Parameter IDs
    private int _paramWeaponAttack;
    private int _paramPunch;
    private int _paramPunchIndex;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
            Debug.LogError("[PlayerAttack] Không tìm thấy Animator!");

        _paramWeaponAttack = Animator.StringToHash("WeaponAttack");
        _paramPunch = Animator.StringToHash("Punch");
        _paramPunchIndex = Animator.StringToHash("PunchIndex");
    }

    private void Update()
    {
        // Reset combo nếu lâu không bấm
        if (Time.time - _lastPunchTime > _comboResetTime)
            _punchComboIndex = 0;

        // Detect click chuột trái
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime)
        {
            Debug.Log($"[PlayerAttack] Cooldown chưa hết, còn {_nextAttackTime - Time.time:F2}s");
            return;
        }

        if (PlayerState.Instance == null)
        {
            Debug.LogError("[PlayerAttack] Không tìm thấy PlayerState!");
            return;
        }

        int weaponType = PlayerState.Instance.WeaponType;

        if (weaponType == 0)
        {
            _currentDamage = unarmedDamage;
            PerformPunch();
            _nextAttackTime = Time.time + unarmedCooldown;
        }
        else
        {
            // WeaponType 1, 2, 3... đều dùng WeaponAttack trigger
            // Mở rộng sau nếu muốn animation khác nhau theo từng loại
            _currentDamage = meleeDamage;
            PerformWeaponAttack();
            _nextAttackTime = Time.time + weaponCooldown;
        }
    }

    private void PerformPunch()
    {
        if (_animator == null) return;

        // Combo: 0 → 1 → 0 → 1...
        _animator.SetInteger(_paramPunchIndex, _punchComboIndex);
        _animator.SetTrigger(_paramPunch);

        _punchComboIndex = (_punchComboIndex + 1) % 2;
        _lastPunchTime = Time.time;

        Debug.Log($"[PlayerAttack] Punch! ComboIndex: {_punchComboIndex}");
    }

    private void PerformWeaponAttack()
    {
        if (_animator == null) return;

        _animator.SetTrigger(_paramWeaponAttack);

        int wt = PlayerState.Instance.WeaponType;
        string weaponName = wt == 1 ? "Gậy/2 tay" : wt == 2 ? "Pistol" : wt == 3 ? "Rifle" : "Weapon";
        Debug.Log($"[PlayerAttack] Tấn công bằng: {weaponName}");
    }
    public void OnMeleeHit()
    {
        Vector3 hitCenter = transform.position
                          + transform.forward * hitDistance
                          + Vector3.up * hitHeight;

        Collider[] hits = Physics.OverlapSphere(hitCenter, hitRadius, zombieLayer);

        if (hits.Length == 0) return;

        foreach (Collider hit in hits)
        {
            ZombieBase zombie = hit.GetComponent<ZombieBase>();
            if (zombie == null)
                zombie = hit.GetComponentInParent<ZombieBase>();

            if (zombie != null)
            {
                // Damage (giữ nguyên)
                zombie.TakeDamage(_currentDamage, gameObject);

                // ── Thêm mới: Gọi Blood VFX ──
                Vector3 hitPoint = hit.ClosestPoint(hitCenter);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;

                var bloodFX = hit.GetComponentInParent<ZombieBloodFXHandler>();
                if (bloodFX != null)
                    bloodFX.OnHitMelee(hitPoint, hitNormal);

                break;
            }
        }
    }

    /// <summary>
    /// Gọi từ bên ngoài (ví dụ input system mới) nếu cần
    /// </summary>
    public void OnAttackInput()
    {
        TryAttack();
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 hitCenter = transform.position
                          + transform.forward * hitDistance
                          + Vector3.up * hitHeight;
        Gizmos.DrawWireSphere(hitCenter, hitRadius);
    }
}