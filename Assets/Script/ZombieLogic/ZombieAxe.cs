using UnityEngine;

/// <summary>
/// ZombieAxe – Zombie cầm rìu, kế thừa ZombieBase.
///
/// Combat gồm 3 phase tuần hoàn:
///   1. APPROACH  → áp sát player đến tầm rìu
///   2. WINDUP    → dừng lại, lấy đà (animation telegraph)
///   3. SWING     → chém, nếu trúng gây sát thương + knockback
///
/// Ngoài ra có:
///   - Lunge Attack : lao thẳng vào player nếu player bỏ chạy xa
///   - Rage Mode    : sau khi mất X% HP, tăng tốc + giảm cooldown
/// </summary>
public class ZombieAxe : ZombieBase
{
    // ── Axe-specific Stats ───────────────────────────────────────────────────
    [Header("Axe Combat")]
    [Tooltip("Tầm chém rìu (nên dài hơn attackRange base ~2-2.5f)")]
    public float axeSwingRange = 2.2f;

    [Tooltip("Thời gian lấy đà trước khi chém (giây)")]
    public float windupDuration = 0.6f;

    [Tooltip("Knockback đẩy player ra sau khi trúng đòn")]
    public float knockbackForce = 4f;

    [Header("Lunge Attack")]
    [Tooltip("Khoảng cách player bỏ chạy để kích hoạt Lunge")]
    public float lungeRange = 6f;

    [Tooltip("Tốc độ lao người (Lunge)")]
    public float lungeSpeed = 8f;

    [Tooltip("Cooldown Lunge (giây)")]
    public float lungeCooldown = 5f;

    [Header("Axe Prop — Gắn rìu vào tay")]
    [Tooltip("Tên GameObject điểm gắn rìu trong Hierarchy (AxePoint, fireaxePivotPoint,...)")]
    public string axeAttachPointName = "fireaxePivotPoint";

    [Tooltip("Prefab rìu dùng làm prop cầm tay (cùng prefab với pickup cũng được)")]
    public GameObject axePropPrefab;

    [Tooltip("Offset vị trí rìu so với bone tay phải (điều chỉnh trong Play Mode)")]
    public Vector3 axePropOffset = Vector3.zero;

    [Tooltip("Offset rotation rìu so với bone tay phải")]
    public Vector3 axePropRotation = Vector3.zero;

    [Header("Axe Drop")]
    [Tooltip("Prefab rìu sẽ rơi xuống khi zombie chết — có thể dùng cùng prefab với Prop")]
    public GameObject axePickupPrefab;

    [Tooltip("Offset vị trí spawn so với zombie (để rìu không chui xuống đất)")]
    public Vector3 axeDropOffset = new Vector3(0.3f, 0.5f, 0f);

    [Tooltip("Lực ném rìu ra khi drop (0 = rơi thẳng xuống)")]
    public float axeDropForce = 2.5f;

    [Header("Rage Mode")]
    [Tooltip("% HP còn lại để vào Rage (0-1)")]
    [Range(0f, 1f)]
    public float rageHpThreshold = 0.35f;

    [Tooltip("Hệ số nhân tốc độ khi Rage")]
    public float rageSpeedMultiplier = 1.4f;

    [Tooltip("Hệ số nhân sát thương khi Rage")]
    public float rageDamageMultiplier = 1.3f;

    // ── Combat State Machine ─────────────────────────────────────────────────
    private enum AxeCombatState { Approach, Windup, Swing, Lunge, Recover }
    private AxeCombatState _combatState = AxeCombatState.Approach;

    // Timers / flags
    private float _windupTimer = 0f;
    private float _recoverTimer = 0f;
    private float _recoverDuration = 0.5f;   // thời gian hồi phục sau đòn đánh
    private float _nextLungeTime = 0f;
    private bool _lungeHit = false;
    private bool _isRaging = false;
    private bool _damageApplied = false;      // tránh gây sát thương 2 lần trong 1 swing

    // Cache
    private HealthSystem _playerHealth;
    private GameObject _equippedAxe;   // instance rìu đang cầm tay

    // ── Override Start ───────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();

        // Ưu tiên dùng axeSwingRange thay attackRange gốc
        attackRange = axeSwingRange;

        if (player != null)
            _playerHealth = player.GetComponent<HealthSystem>();

        EquipAxe();
    }

    // ── OnEnterCombat / OnExitCombat ─────────────────────────────────────────
    protected override void OnEnterCombat()
    {
        _combatState = AxeCombatState.Approach;
        _nextLungeTime = Time.time + lungeCooldown * 0.5f; // lần đầu lunge nhanh hơn
    }

    protected override void OnExitCombat()
    {
        // Reset về Approach khi mất player để lần sau sẵn sàng
        _combatState = AxeCombatState.Approach;
        _isRaging = false;
    }

    // ── Core: UpdateCombatBehaviour ──────────────────────────────────────────
    protected override void UpdateCombatBehaviour()
    {
        if (player == null) return;

        CheckRageMode();

        float dist = Vector3.Distance(transform.position, player.position);

        switch (_combatState)
        {
            case AxeCombatState.Approach: HandleApproach(dist); break;
            case AxeCombatState.Windup: HandleWindup(dist); break;
            case AxeCombatState.Swing: HandleSwing(dist); break;
            case AxeCombatState.Lunge: HandleLunge(dist); break;
            case AxeCombatState.Recover: HandleRecover(); break;
        }
    }

    // ── State Handlers ───────────────────────────────────────────────────────

    /// <summary>Áp sát player. Nếu đủ gần → Windup. Nếu xa + cooldown → Lunge.</summary>
    private void HandleApproach(float dist)
    {
        // Lunge nếu player bỏ chạy xa và cooldown xong
        if (dist >= lungeRange && Time.time >= _nextLungeTime)
        {
            EnterLunge();
            return;
        }

        // Di chuyển về phía player
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float speed = _isRaging ? runSpeed * rageSpeedMultiplier : runSpeed;
        agent.isStopped = false;
        agent.speed = speed;
        agent.stoppingDistance = axeSwingRange * 0.85f;
        agent.updateRotation = true;
        agent.updatePosition = true;
        agent.SetDestination(player.position);

        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);

        // Đủ tầm → vào Windup
        if (dist <= axeSwingRange)
        {
            EnterWindup();
        }
    }

    /// <summary>Dừng lại, lấy đà — đây là "telegraph" để player có cơ hội né.</summary>
    private void HandleWindup(float dist)
    {
        StopAgentCompletely();
        FacePlayer();
        anim.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);

        _windupTimer += Time.deltaTime;

        // Nếu player bước ra khỏi tầm trong lúc lấy đà → hủy, Approach lại
        if (dist > axeSwingRange * 1.4f)
        {
            _combatState = AxeCombatState.Approach;
            anim.ResetTrigger("AxeWindup");
            return;
        }

        if (_windupTimer >= windupDuration)
        {
            _combatState = AxeCombatState.Swing;
            _damageApplied = false;
            anim.SetTrigger("AxeSwing");   // Animator trigger → clip chém rìu
        }
    }

    /// <summary>Đang trong animation chém. Sát thương được gây qua Animation Event.</summary>
    private void HandleSwing(float dist)
    {
        StopAgentCompletely();
        FacePlayer();
        anim.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);

        // Chờ animation event DealDamageToPlayer() gọi
        // Sau khoảng attackCooldown * 0.4f → sang Recover
        // (Thực tế transition được điều khiển bởi Animator, ở đây ta dùng timer backup)
        if (Time.time >= _nextAttackTime)
        {
            _recoverTimer = 0f;
            _combatState = AxeCombatState.Recover;
        }
    }

    /// <summary>Hồi phục ngắn sau đòn đánh → quay về Approach.</summary>
    private void HandleRecover()
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);

        _recoverTimer += Time.deltaTime;
        if (_recoverTimer >= _recoverDuration)
        {
            _combatState = AxeCombatState.Approach;
        }
    }

    /// <summary>Lao thẳng vào player — dùng NavMesh tốc độ cao.</summary>
    private void HandleLunge(float dist)
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float speed = _isRaging ? lungeSpeed * rageSpeedMultiplier : lungeSpeed;
        agent.isStopped = false;
        agent.speed = speed;
        agent.stoppingDistance = axeSwingRange * 0.7f;
        agent.updateRotation = true;
        agent.updatePosition = true;
        agent.SetDestination(player.position);

        anim.SetFloat("Speed", 3f, 0.1f, Time.deltaTime);  // blend tree: run nhanh

        // Đến nơi → đánh ngay
        if (dist <= axeSwingRange)
        {
            if (!_lungeHit)
            {
                _lungeHit = true;
                anim.SetTrigger("AxeSwing");
                ApplyDamageAndKnockback();
            }

            _nextLungeTime = Time.time + lungeCooldown;
            _nextAttackTime = Time.time + attackCooldown;
            _recoverTimer = 0f;
            _combatState = AxeCombatState.Recover;
        }
    }

    // ── Transitions vào state ─────────────────────────────────────────────────
    private void EnterWindup()
    {
        _combatState = AxeCombatState.Windup;
        _windupTimer = 0f;
        anim.SetTrigger("AxeWindup");   // clip lấy đà / raise axe
    }

    private void EnterLunge()
    {
        _combatState = AxeCombatState.Lunge;
        _lungeHit = false;
        anim.SetTrigger("AxeLunge");    // clip chạy lao người
    }

    // ── Damage & Knockback ────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ Animation Event tại frame chém trúng trong clip AxeSwing.
    /// Tên hàm phải khớp với Animation Event trong Unity Editor.
    /// </summary>
    public override void DealDamageToPlayer()
    {
        if (_damageApplied) return;
        _damageApplied = true;

        ApplyDamageAndKnockback();

        // Set cooldown để HandleSwing biết khi nào xong
        _nextAttackTime = Time.time + attackCooldown;
    }

    private void ApplyDamageAndKnockback()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > axeSwingRange * 1.3f) return;

        float dmg = _isRaging ? attackDamage * rageDamageMultiplier : attackDamage;

        _playerHealth ??= player.GetComponent<HealthSystem>();
        _playerHealth?.TakeDamage(dmg, gameObject);

        // Knockback: đẩy player ra sau theo hướng từ zombie → player
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0.2f;
            rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
        }
    }

    // ── Rage Mode ─────────────────────────────────────────────────────────────
    private void CheckRageMode()
    {
        if (_isRaging || healthSystem == null) return;

        float hpRatio = healthSystem.CurrentHP / healthSystem.MaxHP;
        if (hpRatio <= rageHpThreshold)
        {
            _isRaging = true;
            anim.SetTrigger("EnterRage");   // optional: flash effect hoặc roar animation
            Debug.Log($"[ZombieAxe] {gameObject.name} entered RAGE MODE!");
        }
    }

    // ── Nhận damage: skip scream, alert ngay ─────────────────────────────────
    public override void TakeDamage(float damage, GameObject attacker = null)
    {
        base.TakeDamage(damage, attacker);

        // Nếu đang Windup mà bị đánh → không hủy (zombie tức giận vẫn đánh tiếp)
        // Nhưng nếu đang Recover → rút ngắn recover
        if (_combatState == AxeCombatState.Recover)
            _recoverTimer = _recoverDuration * 0.8f;
    }

    // ── Death override ────────────────────────────────────────────────────────
    protected override void Die()
    {
        base.Die();
        DropAxe();
    }

    private void EquipAxe()
    {
        if (axePropPrefab == null) return;

        // Ưu tiên dùng AxePoint (điểm gắn sẵn của tác giả model)
        Transform attachPoint = FindDeepChild(transform, axeAttachPointName);

        // Fallback về RightHand bone nếu không tìm thấy AxePoint
        if (attachPoint == null)
        {
            Debug.LogWarning($"[ZombieAxe] Không tìm thấy '{axeAttachPointName}', fallback về RightHand bone.");
            attachPoint = anim.GetBoneTransform(HumanBodyBones.RightHand);
        }

        if (attachPoint == null)
        {
            Debug.LogError("[ZombieAxe] Không tìm thấy điểm gắn rìu nào!");
            return;
        }

        _equippedAxe = Instantiate(axePropPrefab, attachPoint);
        _equippedAxe.transform.localPosition = axePropOffset;
        _equippedAxe.transform.localEulerAngles = axePropRotation;

        // Tắt physics khi đang cầm tay
        if (_equippedAxe.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        // Tắt collider pickup khi đang cầm tay
        if (_equippedAxe.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        Debug.Log($"[ZombieAxe] Equipped axe to '{attachPoint.name}'.");
    }

    /// <summary>Tìm child theo tên trong toàn bộ cây hierarchy (recursive).</summary>
    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void DropAxe()
    {
        // Destroy prop rìu trên tay
        if (_equippedAxe != null)
            Destroy(_equippedAxe);

        if (axePickupPrefab == null) return;

        // Spawn pickup tại vị trí zombie + offset
        Vector3 spawnPos = transform.position + transform.TransformDirection(axeDropOffset);
        GameObject dropped = Instantiate(axePickupPrefab, spawnPos, Random.rotation);

        // Nếu prefab có Rigidbody → ném ra theo hướng ngẫu nhiên nhẹ để tự nhiên hơn
        if (dropped.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            Vector3 randomDir = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(0.3f, 0.6f),   // bay lên nhẹ trước khi rơi xuống
                Random.Range(-0.5f, 0.5f)
            ).normalized;
            rb.AddForce(randomDir * axeDropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * axeDropForce, ForceMode.Impulse);
        }

        Debug.Log($"[ZombieAxe] {gameObject.name} dropped axe at {spawnPos}");
    }

    // ── Gizmos mở rộng ───────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Tầm rìu
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, axeSwingRange);

        // Tầm Lunge
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, lungeRange);
    }
}