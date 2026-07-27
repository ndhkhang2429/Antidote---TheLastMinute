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
///   - Rage Dash+Slam : khi vào Rage, lao đến player 1 lần rồi chém mạnh
///   - Rage Mode    : sau khi mất X% HP, tăng tốc + giảm cooldown
///
/// MỚI THÊM (attack-while-moving): đòn chém thường (Swing/Recover) không còn
/// dừng agent lại nữa — zombie tiếp tục đuổi theo player trong lúc chém, vì
/// animation AxeSwing giờ chạy trên layer riêng (UpperBody_Attack, Avatar Mask
/// chỉ ảnh hưởng thân trên) trong Animator Controller. Rage Dash/Slam vẫn giữ
/// nguyên cơ chế dừng lại (đây là animation full-body lunge riêng, không dùng
/// layer trên).
/// </summary>
public class ZombieAxe : ZombieBase
{
    // ── Axe-specific Stats ───────────────────────────────────────────────────
    [Header("Axe Combat")]
    [Tooltip("Tầm chém rìu (nên dài hơn attackRange base ~2-2.5f)")]
    public float axeSwingRange = 2.2f;

    [Tooltip("Knockback đẩy player ra sau khi trúng đòn")]
    public float knockbackForce = 4f;

    [Header("Rage Dash & Slam")]
    [Tooltip("Tốc độ chạy khi Rage Dash")]
    public float rageDashSpeed = 8f;

    [Tooltip("Tầm tối đa Rage Dash — nếu player ra ngoài thì hủy")]
    public float rageDashRange = 12f;

    [Tooltip("Sát thương nhát chém Rage Slam (cao hơn đòn thường)")]
    public float rageSlamDamage = 60f;

    [Tooltip("Knockback mạnh hơn khi Rage Slam")]
    public float rageSlamKnockback = 8f;

    [Header("Axe Prop — Gắn rìu vào tay")]
    [Tooltip("Tên GameObject điểm gắn rìu trong Hierarchy (AxePoint, fireaxePivotPoint,...)")]
    public string axeAttachPointName = "AxePoint";

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
    private enum AxeCombatState { Approach, Swing, Recover, RageDash, RageSlam }
    private AxeCombatState _combatState = AxeCombatState.Approach;

    // Timers / flags
    private float _recoverTimer = 0f;
    private float _recoverDuration = 0.5f;   // cooldown giữa các đòn   // thời gian hồi phục sau đòn đánh
    private bool _isRaging = false;
    private bool _rageDashDone = false;       // Rage Dash + Slam chỉ 1 lần duy nhất
    private bool _rageSlamHit = false;        // tránh slam 2 lần
    private bool _damageApplied = false;      // tránh gây sát thương 2 lần trong 1 swing

    // Cache
    private HealthSystem _playerHealth;
    private GameObject _equippedAxe;   // instance rìu đang cầm tay
    private float _baseAttackCooldown;

    // ── Override Start ───────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();

        // Ưu tiên dùng axeSwingRange thay attackRange gốc
        attackRange = axeSwingRange;

        if (player != null)
            _playerHealth = player.GetComponent<HealthSystem>();

        EquipAxe();
        _baseAttackCooldown = attackCooldown;
    }

    // ── OnEnterCombat / OnExitCombat ─────────────────────────────────────────
    protected override void OnEnterCombat()
    {
        _combatState = AxeCombatState.Approach;
    }

    protected override void OnExitCombat()
    {
        _combatState = AxeCombatState.Approach;
        _isRaging = false;
        _rageDashDone = false;
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
            case AxeCombatState.Swing: HandleSwing(dist); break;
            case AxeCombatState.Recover: HandleRecover(); break;
            case AxeCombatState.RageDash: HandleRageDash(dist); break;
            case AxeCombatState.RageSlam: HandleRageSlam(dist); break;
        }
    }

    // ── Movement helper (MỚI THÊM) ───────────────────────────────────────────
    /// <summary>
    /// Giữ agent tiếp tục đuổi theo player + cập nhật Speed animator theo vận tốc
    /// thực tế. Dùng chung cho Approach/Swing/Recover — 3 phase giờ đều cho phép
    /// chân tiếp tục di chuyển trong khi animation chém chạy trên layer riêng.
    /// </summary>
    private void ContinueApproachMovement(float speed)
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.speed = speed;
        agent.stoppingDistance = axeSwingRange * 0.85f;
        agent.updateRotation = true;
        agent.updatePosition = true;
        if (player != null) agent.SetDestination(player.position);

        float normalizedSpeed = speed > 0f ? agent.velocity.magnitude / speed : 0f;
        anim.SetFloat("Speed", normalizedSpeed * 2f, 0.1f, Time.deltaTime);
    }

    // ── State Handlers ───────────────────────────────────────────────────────

    /// <summary>Áp sát player. Nếu đủ gần + cooldown xong → Swing.</summary>
    private void HandleApproach(float dist)
    {
        float speed = _isRaging ? runSpeed * rageSpeedMultiplier : runSpeed;
        ContinueApproachMovement(speed);

        // Đủ tầm + cooldown xong → Swing thẳng
        if (dist <= axeSwingRange && Time.time >= _nextAttackTime)
        {
            EnterSwing();
        }
    }

    /// <summary>
    /// Đang trong animation chém (chạy trên layer UpperBody_Attack).
    /// Không dừng agent nữa — chân tiếp tục đuổi theo player bình thường.
    /// Sát thương được gây qua Animation Event.
    /// </summary>
    private void HandleSwing(float dist)
    {
        float speed = _isRaging ? runSpeed * rageSpeedMultiplier : runSpeed;
        ContinueApproachMovement(speed);

        // Chờ animation event DealDamageToPlayer() gọi
        // Sau khoảng attackCooldown * 0.4f → sang Recover
        // (Thực tế transition được điều khiển bởi Animator, ở đây ta dùng timer backup)
        if (Time.time >= _nextAttackTime)
        {
            _recoverTimer = 0f;
            _combatState = AxeCombatState.Recover;
        }
    }

    /// <summary>Hồi phục ngắn sau đòn đánh → quay về Approach. Vẫn tiếp tục di chuyển.</summary>
    private void HandleRecover()
    {
        float speed = _isRaging ? runSpeed * rageSpeedMultiplier : runSpeed;
        ContinueApproachMovement(speed);

        _recoverTimer += Time.deltaTime;
        if (_recoverTimer >= _recoverDuration)
        {
            _combatState = AxeCombatState.Approach;
        }
    }

    /// <summary>Lao thẳng vào player — dùng NavMesh tốc độ cao.</summary>
    /// <summary>Rage Dash — chạy nhanh về phía player bằng NavMesh. Nếu player ra ngoài rageDashRange → hủy.</summary>
    private void HandleRageDash(float dist)
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        // Player chạy ra ngoài tầm Dash → hủy, về Attack bình thường
        if (dist > rageDashRange)
        {
            _rageDashDone = true;   // coi như xong, không dash nữa
            ResumeAgent(runSpeed * rageSpeedMultiplier);
            _combatState = AxeCombatState.Approach;
            anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
            return;
        }

        // Chạy về phía player tốc độ cao
        agent.isStopped = false;
        agent.speed = rageDashSpeed;
        agent.stoppingDistance = axeSwingRange * 0.8f;
        agent.updateRotation = true;
        agent.updatePosition = true;
        agent.SetDestination(player.position);
        anim.SetFloat("Speed", 2f, 0.05f, Time.deltaTime);

        // Đến tầm → vào RageSlam
        if (dist <= axeSwingRange)
        {
            StopAgentCompletely();
            _rageSlamHit = false;
            _combatState = AxeCombatState.RageSlam;
            anim.SetTrigger("AxeLunge");   // clip chém mạnh — full-body, KHÔNG dùng layer trên
        }
    }

    /// <summary>
    /// Rage Slam — nhát chém mạnh sau Dash, damage cao + knockback lớn.
    /// Giữ nguyên: dừng lại hoàn toàn, vì đây là animation full-body lunge riêng biệt.
    /// </summary>
    private void HandleRageSlam(float dist)
    {
        StopAgentCompletely();
        FacePlayer();
        anim.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);

        // Sát thương gây qua Animation Event RageSlamDamage()
        // Backup timer nếu không có Animation Event
        if (!_rageSlamHit && Time.time >= _nextAttackTime)
        {
            RageSlamDamage();
        }

        // Sau khi slam xong → về Attack bình thường
        if (_rageSlamHit && Time.time >= _nextAttackTime)
        {
            _rageDashDone = true;
            _recoverTimer = 0f;
            _combatState = AxeCombatState.Recover;
        }
    }

    /// <summary>Gọi từ Animation Event của clip RageSlam tại frame chém trúng.</summary>
    public void RageSlamDamage()
    {
        if (_rageSlamHit) return;
        _rageSlamHit = true;

        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > axeSwingRange * 1.4f) return;

        _playerHealth ??= player.GetComponent<HealthSystem>();
        _playerHealth?.TakeDamage(rageSlamDamage, gameObject);

        // Knockback mạnh hơn đòn thường
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0.3f;
            rb.AddForce(dir * rageSlamKnockback, ForceMode.Impulse);
        }

        _nextAttackTime = Time.time + attackCooldown * 1.5f;   // recovery dài hơn sau slam
        Debug.Log($"[ZombieAxe] RAGE SLAM! {rageSlamDamage} damage!");
    }

    // ── Transitions vào state ─────────────────────────────────────────────────
    private void EnterSwing()
    {
        _combatState = AxeCombatState.Swing;
        _damageApplied = false;
        anim.SetTrigger("AxeSwing"); // Trigger này giờ chạy trên layer UpperBody_Attack
    }

    private void EnterRageDash()
    {
        _combatState = AxeCombatState.RageDash;
        _rageSlamHit = false;
        anim.applyRootMotion = false;

        // Đảm bảo NavMesh sẵn sàng di chuyển
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.speed = rageDashSpeed;
            agent.stoppingDistance = axeSwingRange * 0.8f;
            if (player != null) agent.SetDestination(player.position);
        }

        anim.SetFloat("Speed", 2f);
        Debug.Log("[ZombieAxe] RAGE DASH START!");
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
            _rageDashDone = false;
            attackCooldown *= 0.6f;
            anim.SetTrigger("EnterRage");
            Debug.Log($"[ZombieAxe] {gameObject.name} RAGE MODE! Cooldown → {attackCooldown:F2}s");

            // Chỉ Rage Dash nếu chưa từng dash
            if (!_rageDashDone)
                EnterRageDash();
        }
    }

    // ── Nhận damage: skip scream, alert ngay ─────────────────────────────────
    public override void TakeDamage(float damage, GameObject attacker = null)
    {
        base.TakeDamage(damage, attacker);

        // Nếu đang Recover → rút ngắn để phản ứng nhanh hơn
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

    public override void ResetForPool()
    {
        base.ResetForPool();

        // Reset combat state machine riêng của Axe
        _combatState = AxeCombatState.Approach;
        _recoverTimer = 0f;
        _isRaging = false;
        _rageDashDone = false;
        _rageSlamHit = false;
        _damageApplied = false;

        // Trả cooldown về giá trị gốc (CheckRageMode có thể đã nhân 0.6f ở vòng đời trước)
        attackCooldown = _baseAttackCooldown;

        // Rìu đã bị Destroy trong DropAxe() lúc chết -> gắn lại rìu mới
        if (_equippedAxe == null)
            EquipAxe();
    }

    // ── Gizmos mở rộng ───────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Tầm rìu
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, axeSwingRange);

        // Tầm Rage Dash tối đa
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, rageDashRange);
    }
}