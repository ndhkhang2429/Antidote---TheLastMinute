using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ZombieBase – Base class cho tất cả zombie.
///
/// Trách nhiệm của class này CHỈ là:
///   1. Chọn Mode lớn qua BT: Patrol → Chase → Combat
///   2. Thực thi Patrol (Wander) và Chase (hành vi chung, mọi zombie đều giống nhau)
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
    protected ZombieAudioController audioController;

    // ── Stats ────────────────────────────────────────────────────────────────
    [Header("Stats")]
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;
    public float walkSpeed = 1f;
    public float runSpeed = 3.5f;
    public float detectionRange = 10f;
    public float attackRange = 2f;

    [Header("Patrol / Wander")]
    [Tooltip("Bán kính lang thang tính từ vị trí spawn")]
    public float wanderRadius = 8f;
    [Tooltip("Thời gian dừng nghỉ tối thiểu tại mỗi điểm")]
    public float minIdleTime = 2f;
    [Tooltip("Thời gian dừng nghỉ tối đa tại mỗi điểm")]
    public float maxIdleTime = 5f;
    [Tooltip("Khoảng cách tối thiểu để sample điểm đến mới (tránh đứng yên)")]
    public float minMoveDistance = 2f;
    [Tooltip("Các điểm đặc biệt trong phòng: giường, tủ, cửa... (tuỳ chọn)")]
    public Transform[] interestPoints;
    [Range(0f, 1f)]
    [Tooltip("Xác suất zombie đi đến interest point thay vì điểm ngẫu nhiên")]
    public float interestPointChance = 0.3f;

    [Header("Head IK")]
    public float headLookWeight = 0.7f;
    public Vector3 headLookOffset = new Vector3(0, 1.5f, 0);

    [Header("Scream")]
    public float screamDuration = 2f;
    public float turnSpeed = 10f;
    public float turnThreshold = 0.95f;

    // ── Mode (BT output) ─────────────────────────────────────────────────────
    protected enum ZombieMode { Patrol, Chase, Combat }
    protected ZombieMode _mode = ZombieMode.Patrol;

    // ── Private state ────────────────────────────────────────────────────────
    private Node _btRoot;
    protected bool _isDead = false;
    private bool _inCombat = false;

    // Scream
    private bool _hasDetectedPlayer = false;
    private bool _screamDone = false;
    private float _screamTimer = 0f;
    private enum ScreamPhase { None, Turning, Screaming }
    private ScreamPhase _screamPhase = ScreamPhase.None;

    // Wander
    private Vector3 _wanderOrigin;           // tâm vùng lang thang = vị trí spawn
    private bool _isWanderIdle = false;      // đang trong phase dừng nghỉ
    private float _idleTimer = 0f;
    private float _idleDuration = 0f;
    private bool _wanderDestinationSet = false;

    // ===========================================
    // MỚI THÊM: cờ "buộc đuổi player bất kể khoảng cách"
    // Dùng cho zombie vừa được AlarmSystem spawn ra, cần lao thẳng tới player
    // dù đang ở xa hơn detectionRange * 1.5 (điều kiện ShouldChase bình thường).
    // Tự tắt khi zombie đã thực sự phát hiện player theo cách thông thường (CanDetectPlayer).
    // ===========================================
    private bool _forcedByAlarm = false;

    // Public read-only
    public bool IsDead => _isDead;
    public bool ScreamDone => _screamDone;

    public bool IsInCombat => _mode == ZombieMode.Combat;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────
    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
        audioController = GetComponent<ZombieAudioController>();

        if (healthSystem == null)
            Debug.LogError($"[ZombieBase] {gameObject.name} thiếu HealthSystem!");
        else
            healthSystem.OnDeath += Die;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        _btRoot = BuildBehaviourTree();

        // Ghi nhớ vị trí spawn làm tâm vùng wander
        _wanderOrigin = transform.position;
        SetNewWanderDestination();
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
    private Node BuildBehaviourTree()
    {
        return new Selector(new List<Node>
        {
            // Nếu detect player → Scream lần đầu → vào Combat
            new Sequence(new List<Node>
            {
                new ConditionNode(CanDetectPlayer),
                new ActionNode(SetCombatMode),
            }),

            // Nếu player trong tầm chase (đã từng thấy) → Chase
            new Sequence(new List<Node>
            {
                new ConditionNode(ShouldChase),
                new ActionNode(SetChaseMode),
            }),

            // Fallback → Patrol (Wander)
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
        // MỚI THÊM: nếu đang bị buộc đuổi bởi AlarmSystem, luôn chase bất kể khoảng cách
        if (_forcedByAlarm) return true;

        if (!_hasDetectedPlayer || player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= detectionRange * 1.5f;
    }

    // ── BT Actions (chỉ set _mode) ────────────────────────────────────────────
    private NodeState SetCombatMode()
    {
        if (!_hasDetectedPlayer)
        {
            _hasDetectedPlayer = true;
            _screamDone = false;
            _screamPhase = ScreamPhase.Turning;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // MỚI THÊM: đã thực sự phát hiện player theo cách bình thường,
        // không cần cờ "buộc đuổi" nữa từ giờ trở đi
        _forcedByAlarm = false;

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

            // Reset wander để tiếp tục lang thang bình thường
            _isWanderIdle = false;
            SetNewWanderDestination();
        }

        // MỚI THÊM: an toàn - đảm bảo cờ buộc đuổi không bị kẹt mãi nếu rơi vào Patrol
        _forcedByAlarm = false;

        _mode = ZombieMode.Patrol;
        return NodeState.Success;
    }

    // ── Execute Mode ─────────────────────────────────────────────────────────
    private void ExecutePatrol()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0.5f;
        agent.updateRotation = true;
        agent.updatePosition = true;

        // --- Phase IDLE: đứng dừng nghỉ ---
        if (_isWanderIdle)
        {
            agent.isStopped = true;
            anim.SetFloat("Speed", 0f, 0.2f, Time.deltaTime);

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleDuration)
            {
                _isWanderIdle = false;
                SetNewWanderDestination();
            }
            return;
        }

        // --- Phase MOVE: di chuyển đến điểm wander ---
        anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);

        bool arrived = !agent.pathPending
                       && agent.remainingDistance <= agent.stoppingDistance;

        if (arrived && _wanderDestinationSet)
        {
            _wanderDestinationSet = false;
            _isWanderIdle = true;
            _idleTimer = 0f;
            _idleDuration = Random.Range(minIdleTime, maxIdleTime);
        }
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
        if (!_screamDone) return;

        if (!_inCombat)
        {
            _inCombat = true;
            OnEnterCombat();
        }

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
                audioController?.PlayAlert();
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

    // ── Wander Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Chọn điểm đến tiếp theo cho wander:
    /// 30% → interest point (giường, bàn, cửa...)
    /// 70% → điểm ngẫu nhiên trên NavMesh trong wanderRadius
    /// </summary>
    private void SetNewWanderDestination()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        Vector3 destination;

        if (interestPoints != null && interestPoints.Length > 0
            && Random.value < interestPointChance)
        {
            destination = interestPoints[Random.Range(0, interestPoints.Length)].position;
        }
        else
        {
            destination = GetRandomNavMeshPoint(_wanderOrigin, wanderRadius);
        }

        agent.SetDestination(destination);
        _wanderDestinationSet = true;
    }

    /// <summary>
    /// Sample điểm ngẫu nhiên trên NavMesh trong bán kính radius quanh center.
    /// Thử tối đa 10 lần, fallback về vị trí hiện tại nếu không tìm được.
    /// </summary>
    private Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 candidate = center + Random.insideUnitSphere * radius;
            candidate.y = center.y;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, transform.position) > minMoveDistance)
                {
                    // THÊM: kiểm tra đường đi thực tế có đi được không
                    // và path length không quá dài so với straight-line distance
                    NavMeshPath path = new NavMeshPath();
                    if (agent.CalculatePath(hit.position, path)
                        && path.status == NavMeshPathStatus.PathComplete)
                    {
                        float pathLength = GetPathLength(path);
                        float straightLine = Vector3.Distance(transform.position, hit.position);

                        // Nếu path dài hơn 2.5x đường thẳng → điểm đó nằm qua phòng khác
                        if (pathLength < straightLine * 2.5f)
                            return hit.position;
                    }
                }
            }
        }
        return transform.position;
    }

    private float GetPathLength(NavMeshPath path)
    {
        float length = 0f;
        Vector3[] corners = path.corners;
        for (int i = 1; i < corners.Length; i++)
            length += Vector3.Distance(corners[i - 1], corners[i]);
        return length;
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

    // ── Public API ───────────────────────────────────────────────────────────
    public void ForceAlert()
    {
        _hasDetectedPlayer = true;
        _screamDone = true;
        _screamPhase = ScreamPhase.None;
    }

    // ===========================================
    // MỚI THÊM: gọi từ AlarmSystem ngay sau khi Instantiate zombie mới.
    // Buộc zombie lao thẳng tới player bất kể khoảng cách ban đầu bao xa,
    // cho tới khi thực sự phát hiện player theo cách bình thường (CanDetectPlayer),
    // lúc đó cơ chế scream/combat gốc sẽ tự tiếp quản.
    // ===========================================
    public void ForceChasePlayer()
    {
        if (_isDead) return;
        // LƯU Ý: KHÔNG set _hasDetectedPlayer = true ở đây.
        // ShouldChase() đã tự trả true khi thấy _forcedByAlarm, không cần _hasDetectedPlayer.
        // Nếu set _hasDetectedPlayer = true sớm, khi CanDetectPlayer() thành true sau này,
        // SetCombatMode() sẽ tưởng đây KHÔNG PHẢI lần đầu phát hiện (vì check "if (!_hasDetectedPlayer)"),
        // nên bỏ qua toàn bộ chuỗi Scream -> _screamDone mãi mãi false -> combat không bao giờ chạy.
        _forcedByAlarm = true;
    }

    public virtual void TakeDamage(float damage, GameObject attacker = null)
    {
        if (_isDead) return;
        healthSystem.TakeDamage(damage, attacker);
        audioController?.PlayHurt();
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

    public virtual void PlayAttackSound()
    {
        audioController?.PlayAttack();
    }

    protected virtual void Die()
    {
        _isDead = true;
        agent.isStopped = true;
        agent.enabled = false;
        anim.SetTrigger("IsDead");
        audioController?.PlayDeath();
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public virtual void ResetToNormalCombatState()
    {
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Detection & attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Wander radius (tính từ wanderOrigin khi play, hoặc vị trí hiện tại khi edit)
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector3 origin = Application.isPlaying ? _wanderOrigin : transform.position;
        Gizmos.DrawWireSphere(origin, wanderRadius);
    }
}