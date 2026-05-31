using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ZombieBase – Base class cho tất cả zombie.
///
/// Trách nhiệm của class này CHỈ là:
///   1. Chọn Mode lớn qua BT: Patrol → Chase → Combat
///   2. Thực thi Patrol và Chase (hành vi chung, mọi zombie đều giống nhau)
///   3. Gọi UpdateCombatBehaviour() để subclass tự lo chi tiết combat
///   4. Quản lý Scream (first detection) và Death
///
/// Subclass KHÔNG cần hiểu BT, chỉ cần override:
///   - UpdateCombatBehaviour()  → logic combat riêng (state machine)
///   - OnEnterCombat()          → hook khi bắt đầu vào combat lần đầu
///   - OnExitCombat()           → hook khi mất player
///   - DealDamageToPlayer()     → Animation Event
/// </summary>
public class ZombieBase : MonoBehaviour
{
    // ── Components (protected để subclass truy cập) ──────────────────────────
    protected NavMeshAgent agent;
    protected Animator anim;
    protected Transform player;
    protected HealthSystem healthSystem;

    // ── Stats ────────────────────────────────────────────────────────────────
    [Header("Stats")]
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;
    public float walkSpeed = 1f;
    public float runSpeed = 3.5f;
    public float detectionRange = 10f;
    public float attackRange = 2f;

    [Header("Patrol")]
    public Transform[] waypoints;

    [Header("Head IK")]
    public float headLookWeight = 0.7f;
    public Vector3 headLookOffset = new Vector3(0, 1.5f, 0);

    [Header("Scream")]
    public float screamDuration = 2f;
    public float turnSpeed = 10f;
    public float turnThreshold = 0.95f;

    // ── Mode (BT output) ─────────────────────────────────────────────────────
    // BT chỉ set biến này, không làm gì khác.
    protected enum ZombieMode { Patrol, Chase, Combat }
    protected ZombieMode _mode = ZombieMode.Patrol;

    // ── Private state ────────────────────────────────────────────────────────
    private Node _btRoot;
    protected bool _isDead = false;
    private bool _inCombat = false;   // đã vào combat lần đầu chưa

    // Scream
    private bool _hasDetectedPlayer = false;
    private bool _screamDone = false;
    private float _screamTimer = 0f;
    private enum ScreamPhase { None, Turning, Screaming }
    private ScreamPhase _screamPhase = ScreamPhase.None;

    // Patrol
    private int _waypointIndex = 0;

    // Public read-only để subclass hoặc editor đọc nếu cần
    public bool IsDead => _isDead;
    public bool ScreamDone => _screamDone;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────
    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();

        if (healthSystem == null)
            Debug.LogError($"[ZombieBase] {gameObject.name} thiếu HealthSystem!");
        else
            healthSystem.OnDeath += Die;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        _btRoot = BuildBehaviourTree();
        GoToNextWaypoint();
    }

    private void OnDestroy()
    {
        if (healthSystem != null) healthSystem.OnDeath -= Die;
    }

    protected virtual void Update()
    {
        if (_isDead || player == null) return;

        // Scream block BT hoàn toàn
        if (_screamPhase != ScreamPhase.None)
        {
            HandleScream();
            return;
        }

        // BT chỉ làm 1 việc: set _mode
        _btRoot.Evaluate();

        // Dispatch sang hành vi tương ứng
        switch (_mode)
        {
            case ZombieMode.Patrol: ExecutePatrol(); break;
            case ZombieMode.Chase: ExecuteChase(); break;
            case ZombieMode.Combat: ExecuteCombat(); break;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (player == null || _isDead) return;

        if (_mode == ZombieMode.Combat && _screamDone)
        {
            anim.SetLookAtWeight(headLookWeight, 0.3f, 0.7f);
            anim.SetLookAtPosition(player.position + headLookOffset);
        }
        else
        {
            anim.SetLookAtWeight(0f);
        }
    }

    // ── Behaviour Tree ───────────────────────────────────────────────────────
    // BT CỰC KỲ ĐƠN GIẢN: chỉ set _mode, không gọi gì khác.
    private Node BuildBehaviourTree()
    {
        return new Selector(new List<Node>
        {
            // Nếu detect player → Scream lần đầu → vào Combat
            new Sequence(new List<Node>
            {
                new ConditionNode(CanDetectPlayer),
                new ActionNode(SetCombatMode),       // chỉ set _mode = Combat
            }),

            // Nếu player trong tầm chase (đã từng thấy) → Chase
            new Sequence(new List<Node>
            {
                new ConditionNode(ShouldChase),
                new ActionNode(SetChaseMode),
            }),

            // Fallback → Patrol
            new ActionNode(SetPatrolMode),
        });
    }

    // ── BT Conditions (PURE – không có side effect) ──────────────────────────
    private bool CanDetectPlayer()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= detectionRange;
    }

    private bool ShouldChase()
    {
        // Đã từng thấy player và còn trong tầm chase mở rộng (1.5x)
        if (!_hasDetectedPlayer || player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= detectionRange * 1.5f;
    }

    // ── BT Actions (chỉ set _mode, không làm gì khác) ────────────────────────
    private NodeState SetCombatMode()
    {
        // Lần đầu phát hiện → trigger scream
        if (!_hasDetectedPlayer)
        {
            _hasDetectedPlayer = true;
            _screamDone = false;
            _screamPhase = ScreamPhase.Turning;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        _mode = ZombieMode.Combat;
        return NodeState.Success;
    }

    private NodeState SetChaseMode()
    {
        _mode = ZombieMode.Chase;
        return NodeState.Success;
    }

    private NodeState SetPatrolMode()
    {
        // Mất player → reset để scream lại lần sau
        if (_hasDetectedPlayer)
        {
            _hasDetectedPlayer = false;
            _screamDone = false;
            _screamPhase = ScreamPhase.None;

            if (_inCombat)
            {
                _inCombat = false;
                OnExitCombat();
            }
        }

        _mode = ZombieMode.Patrol;
        return NodeState.Success;
    }

    // ── Execute Mode ─────────────────────────────────────────────────────────
    private void ExecutePatrol()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0f;
        agent.updateRotation = true;
        agent.updatePosition = true;

        anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);

        if (!agent.pathPending && agent.remainingDistance <= 0.3f)
            GoToNextWaypoint();
    }

    private void ExecuteChase()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.stoppingDistance = attackRange;
        agent.updateRotation = true;
        agent.updatePosition = true;
        agent.SetDestination(player.position);

        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
    }

    private void ExecuteCombat()
    {
        // Chờ scream xong mới vào combat
        if (!_screamDone) return;

        // Hook OnEnterCombat chỉ gọi 1 lần
        if (!_inCombat)
        {
            _inCombat = true;
            OnEnterCombat();
        }

        // Subclass tự lo từ đây
        UpdateCombatBehaviour();
    }

    // ── Scream ───────────────────────────────────────────────────────────────
    private void HandleScream()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.updatePosition = false;
        agent.updateRotation = false;
        anim.SetFloat("Speed", 0f);

        if (_screamPhase == ScreamPhase.Turning)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0f;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * turnSpeed);

            if (Vector3.Dot(transform.forward, dir) >= turnThreshold)
            {
                _screamPhase = ScreamPhase.Screaming;
                _screamTimer = 0f;
                anim.SetTrigger("Scream");
            }
        }
        else if (_screamPhase == ScreamPhase.Screaming)
        {
            _screamTimer += Time.deltaTime;
            if (_screamTimer >= screamDuration)
            {
                _screamPhase = ScreamPhase.None;
                _screamDone = true;
                agent.updatePosition = true;
                agent.updateRotation = true;
            }
        }
    }

    // ── Virtual hooks cho subclass ───────────────────────────────────────────

    /// <summary>
    /// Gọi MỖI FRAME khi zombie đang ở mode Combat và scream đã xong.
    /// Subclass override hàm này để implement state machine combat riêng.
    /// </summary>
    protected virtual void UpdateCombatBehaviour()
    {
        // Default: melee đơn giản
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            // Đứng yên, đánh
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
            FacePlayer();

            if (Time.time >= _nextAttackTime)
            {
                anim.SetTrigger("Attack");
                _nextAttackTime = Time.time + attackCooldown;
            }
        }
        else
        {
            // Chase thêm trong combat (player vừa bước ra)
            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.stoppingDistance = attackRange;
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.SetDestination(player.position);
            anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
        }
    }

    /// <summary>Gọi 1 lần khi zombie lần đầu tiên bước vào trạng thái Combat (sau scream).</summary>
    protected virtual void OnEnterCombat() { }

    /// <summary>Gọi 1 lần khi zombie mất dấu player và về Patrol.</summary>
    protected virtual void OnExitCombat() { }

    // ── Combat helpers (subclass dùng tự do) ─────────────────────────────────
    protected float _nextAttackTime = 0f;

    protected void FacePlayer(bool instant = false)
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = instant
            ? target
            : Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed * 1.5f);
    }

    protected void StopAgentCompletely()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.updateRotation = false;
        agent.updatePosition = true;
        if (agent.hasPath) agent.ResetPath();
    }

    protected void ResumeAgent(float speed)
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        agent.speed = speed;
        agent.updateRotation = true;
        agent.updatePosition = true;
        agent.stoppingDistance = 0f;
    }

    protected static Vector3 FlatDir(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
    }

    protected void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        agent.destination = waypoints[_waypointIndex].position;
        _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void ForceAlert()
    {
        _hasDetectedPlayer = true;
        _screamDone = true;
        _screamPhase = ScreamPhase.None;
    }

    public virtual void TakeDamage(float damage, GameObject attacker = null)
    {
        if (_isDead) return;
        healthSystem.TakeDamage(damage, attacker);
        // Bị đánh → bỏ qua scream
        _hasDetectedPlayer = true;
        _screamDone = true;
        _screamPhase = ScreamPhase.None;
    }

    /// <summary>Gọi từ Animation Event của clip Attack.</summary>
    public virtual void DealDamageToPlayer()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > attackRange * 1.2f) return;
        player.GetComponent<HealthSystem>()?.TakeDamage(attackDamage, gameObject);
    }

    protected virtual void Die()
    {
        _isDead = true;
        agent.isStopped = true;
        agent.enabled = false;
        anim.SetTrigger("IsDead");
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}