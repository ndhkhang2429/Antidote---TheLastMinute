using System;
using UnityEngine;

/// <summary>
/// Quản lý HP cho bất kỳ entity nào (Player, Zombie, NPC).
///
/// Có 2 loại event song song:
/// — C# event   : dùng cho script trên cùng GameObject (ZombieBase.Die, ...)
///                Subscribe bằng code: healthSystem.OnDeath += Die;
/// — GameEventSO: dùng cho hệ thống ngoài (HUD, Audio, GameManager...)
///                Kéo thả asset vào Inspector, không cần viết code kết nối.
///
/// — Player: gán PlayerStatsSO để đọc maxHP
/// — Zombie: để _statsSO trống, tự điền maxHP trong Inspector
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Config (Player dùng SO | Zombie điền tay)")]
    [SerializeField] private PlayerStatsSO _statsSO;
    [SerializeField] private float _maxHPOverride = 100f;

    [Header("GameEventSO — kéo asset vào Inspector (cho HUD, Audio...)")]
    [SerializeField] private FloatGameEventSO _soOnDamaged;  // truyền HPPercent (0-1)
    [SerializeField] private FloatGameEventSO _soOnHealed;   // truyền HPPercent (0-1)
    [SerializeField] private GameEventSO _soOnDied;

    [Header("Die Settings")]
    [SerializeField] private float _destroyDelay = 3f;
    [SerializeField] private bool _destroyOnDeath = false;

    // ── C# events — ZombieBase và script cùng GameObject subscribe ──
    public event Action OnDeath;
    public event Action<float, float> OnDamaged;  // (currentHP, maxHP)
    public event Action<GameObject, float, float> OnDamagedByAttacker;
    public event Action<float, float> OnHealed;   // (currentHP, maxHP)

    // ── Runtime state ──────────────────────────────────────
    private float _currentHP;
    private bool _isDead;

    public bool IsDead => _isDead;
    public float MaxHP => _statsSO != null ? _statsSO.maxHP : _maxHPOverride;
    public float CurrentHP => _currentHP;
    public float HPPercent => _currentHP / MaxHP;

    private void Awake()
    {
        _currentHP = MaxHP;
    }

    // ── Public API ─────────────────────────────────────────

    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (DeveloperCheatManager.GodMode && CompareTag("Player"))
            return;

        if (_isDead || damage <= 0)
            return;

        _currentHP = Mathf.Clamp(_currentHP - damage, 0, MaxHP);

        Debug.Log($"[HealthSystem] {gameObject.name} nhận {damage} damage " +
                  $"từ {attacker?.name ?? "Unknown"}. HP: {_currentHP}/{MaxHP}");

        OnDamaged?.Invoke(_currentHP, MaxHP);
        OnDamagedByAttacker?.Invoke(attacker, _currentHP, MaxHP);
        _soOnDamaged?.Raise(HPPercent);

        if (_currentHP <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        if (_isDead || amount <= 0) return;

        _currentHP = Mathf.Clamp(_currentHP + amount, 0, MaxHP);

        OnHealed?.Invoke(_currentHP, MaxHP);
        _soOnHealed?.Raise(HPPercent);
    }

    // ── Public API ─────────────────────────────────────────
    public void ResetHealth()
    {
        _isDead = false;
        _currentHP = MaxHP;
    }

    // ── Internal ───────────────────────────────────────────

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[HealthSystem] {gameObject.name} đã chết!");

        OnDeath?.Invoke();   // C# event → ZombieBase.Die() subscribe trực tiếp
        _soOnDied?.Raise();  // SO event  → GameManager, Audio qua Inspector

        if (_destroyOnDeath)
            Destroy(gameObject, _destroyDelay);
    }

    // ── Gizmos ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Vector3 pos = transform.position + Vector3.up * 2.5f;
        Gizmos.DrawCube(pos, new Vector3(HPPercent, 0.1f, 0.1f));
    }
}