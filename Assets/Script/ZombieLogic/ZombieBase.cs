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

    // ===========================================
    // MỚI THÊM: Line of Sight — chống zombie "thấy xuyên tường"
    // ===========================================
    [Header("Line of Sight")]
    [Tooltip("Chiều cao 'mắt' zombie tính từ chân, dùng làm gốc raycast")]
    public float eyeHeight = 1.6f;
    [Tooltip("Layer của Player + layer vật cản (tường, cửa...). PHẢI bao gồm cả 2 để raycast phân biệt được trúng player hay trúng tường")]
    public LayerMask sightMask;
    [Tooltip("Góc nhìn (độ), 360 = không giới hạn góc, chỉ dựa vào raycast")]
    public float fieldOfViewAngle = 360f;
    [Tooltip("Sau khi mất Line of Sight, zombie vẫn 'nhớ' vị trí cuối và đuổi tới đó trong bao lâu trước khi bỏ cuộc")]
    public float loseSightDuration = 4f;

    // Runtime LOS state
    private bool _hasLineOfSightNow = false;
    private float _lastSeenTime = -999f;
    private Vector3 _lastKnownPlayerPosition;

    protected bool HasLineOfSightNow => _hasLineOfSightNow;
    protected Vector3 LastKnownPlayerPosition => _lastKnownPlayerPosition;
    protected Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

    [System.Serializable]
    public class LootEntry
    {
        [Tooltip("Prefab rớt ra (Prefab này phải gắn script WorldItem của bạn)")]
        public GameObject itemPrefab;

        [Range(0f, 1f)]
        [Tooltip("Tỷ lệ rớt (0.5 = 50%, 0.1 = 10%)")]
        public float dropChance = 0.5f;

        [Tooltip("Số lượng tối thiểu rớt ra")]
        public int minAmount = 1;

        [Tooltip("Số lượng tối đa rớt ra")]
        public int maxAmount = 1;
    }

    [Header("Loot System")]
    [Tooltip("Danh sách các item có thể rớt ra khi zombie chết")]
    public List<LootEntry> lootTable;

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

    // Cờ "buộc đuổi player bất kể khoảng cách" (dùng bởi AlarmSystem)
    private bool _forcedByAlarm = false;

    // Public read-only
    public bool IsDead => _isDead;
    public bool ScreamDone => _screamDone;
    public bool IsInCombat => _mode == ZombieMode.Combat;

    public bool IsPatrolling => _mode == ZombieMode.Patrol;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
        audioController = GetComponent<ZombieAudioController>();

        if (healthSystem == null)
        {
            Debug.LogError($"[ZombieBase] {gameObject.name} thiếu HealthSystem!");
        }
        else
        {
            healthSystem.OnDeath += Die;
            healthSystem.OnDamagedByAttacker += HandleDamagedByAttacker;
        }
    }
    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        _btRoot = BuildBehaviourTree();

        // Ghi nhớ vị trí spawn làm tâm vùng wander
        _wanderOrigin = transform.position;
        SetNewWanderDestination();
    }

    private void OnDestroy()
    {
        if (healthSystem == null) return;

        healthSystem.OnDeath -= Die;
        healthSystem.OnDamagedByAttacker -= HandleDamagedByAttacker;
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

        // MỚI THÊM: cập nhật LOS mỗi frame TRƯỚC khi BT evaluate,
        // vì CanDetectPlayer/ShouldChase đều phụ thuộc vào giá trị này
        UpdateSightTracking();

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

    // ── Line of Sight (MỚI THÊM) ──────────────────────────────────────────────

    /// <summary>
    /// Raycast từ mắt zombie tới player để xác nhận không có tường/vật cản chắn giữa.
    /// sightMask PHẢI chứa cả layer Player lẫn layer vật cản, để phân biệt
    /// "raycast trúng player" (thấy được) với "raycast trúng tường" (bị chắn).
    /// </summary>
    protected bool HasLineOfSightToPlayer(float distance)
    {
        if (player == null) return false;
        if (distance > detectionRange) return false;

        Vector3 eyePos = EyePosition;
        Vector3 targetPos = player.position + Vector3.up * 1.5f;
        Vector3 toTarget = targetPos - eyePos;
        float fullDistance = toTarget.magnitude;
        if (fullDistance <= 0.01f) return true;

        Vector3 dir = toTarget / fullDistance;

        // Kiểm tra góc nhìn (bỏ qua nếu fieldOfViewAngle = 360)
        if (fieldOfViewAngle < 359f)
        {
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > fieldOfViewAngle * 0.5f) return false;
        }

        if (Physics.Raycast(eyePos, dir, out RaycastHit hit, fullDistance, sightMask, QueryTriggerInteraction.Ignore))
        {
            // Trúng player trước → thấy được. Trúng bất kỳ thứ gì khác (tường, cửa) → bị chắn.
            return hit.collider.CompareTag("Player") || hit.transform.root == player;
        }

        // Không trúng gì cả trong tầm fullDistance -> không có vật cản -> thấy được
        return true;
    }

    /// <summary>
    /// Cập nhật trạng thái LOS + vị trí cuối cùng thấy player mỗi frame.
    /// Dùng detectionRange * 1.5 (giống ngưỡng ShouldChase) để vẫn theo dõi
    /// được player khi đang trong Chase (xa hơn detectionRange gốc một chút).
    /// </summary>
    private void UpdateSightTracking()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        float trackingRange = Mathf.Max(detectionRange * 1.5f, attackRange + 2f);

        if (dist <= trackingRange && HasLineOfSightToPlayer(dist))
        {
            _hasLineOfSightNow = true;
            _lastSeenTime = Time.time;
            _lastKnownPlayerPosition = player.position;
        }
        else
        {
            _hasLineOfSightNow = false;
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

            // Nếu player trong tầm chase (đã từng thấy, hoặc vẫn còn "nhớ" vị trí gần đây) → Chase
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
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;

        // MỚI THÊM: bắt buộc phải có Line of Sight thật sự, không chỉ khoảng cách
        return HasLineOfSightToPlayer(dist);
    }

    private bool ShouldChase()
    {
        // Nếu đang bị buộc đuổi bởi AlarmSystem, luôn chase bất kể khoảng cách/LOS
        if (_forcedByAlarm) return true;

        if (!_hasDetectedPlayer || player == null) return false;

        // MỚI THÊM: nếu đang thấy player ngay lúc này -> chắc chắn tiếp tục chase
        if (_hasLineOfSightNow) return true;

        // MỚI THÊM: mất LOS nhưng vẫn còn trong "thời gian nhớ" -> vẫn chase
        // tới vị trí cuối cùng thấy player (ExecuteChase sẽ dùng LastKnownPlayerPosition)
        if (Time.time - _lastSeenTime <= loseSightDuration) return true;

        // Hết thời gian nhớ mà vẫn không thấy lại -> bỏ cuộc, về Patrol
        return false;
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

        // Đã thực sự phát hiện player theo cách bình thường, không cần cờ "buộc đuổi" nữa
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

        // An toàn - đảm bảo cờ buộc đuổi không bị kẹt mãi nếu rơi vào Patrol
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

        // MỚI THÊM: nếu đang thấy player thật sự thì đuổi tới vị trí hiện tại của player,
        // nếu không (đang chạy theo trí nhớ vì vừa mất LOS) thì đuổi tới vị trí cuối cùng thấy được
        Vector3 target = _hasLineOfSightNow || _forcedByAlarm ? player.position : LastKnownPlayerPosition;
        agent.SetDestination(target);

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
                    // Kiểm tra đường đi thực tế có đi được không
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
            // MỚI THÊM: đuổi theo vị trí hiện tại nếu còn LOS, nếu không thì theo trí nhớ
            agent.SetDestination(_hasLineOfSightNow ? player.position : LastKnownPlayerPosition);
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

    /// <summary>
    /// Khi nhận damage từ Player, zombie lập tức biết vị trí Player và chuyển sang Chase,
    /// không phụ thuộc detectionRange hoặc Line of Sight tại thời điểm bị bắn.
    /// </summary>
    private void HandleDamagedByAttacker(
        GameObject attacker,
        float currentHP,
        float maxHP)
    {
        if (_isDead)
            return;

        // Nếu script súng chưa truyền attacker, vẫn tự tìm Player để zombie phản ứng.
        GameObject playerObject = null;

        if (attacker != null)
        {
            Transform attackerRoot = attacker.transform.root;

            if (attacker.CompareTag("Player"))
                playerObject = attacker;
            else if (attackerRoot.CompareTag("Player"))
                playerObject = attackerRoot.gameObject;
            else
                playerObject = attacker.GetComponentInParent<Transform>()?.root.gameObject;
        }

        if (playerObject == null || !playerObject.CompareTag("Player"))
            playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning(
                $"[ZombieBase] {name} bị gây damage nhưng không tìm thấy GameObject có tag Player.");
            return;
        }

        player = playerObject.transform;

        _hasDetectedPlayer = true;
        _screamDone = true;
        _screamPhase = ScreamPhase.None;

        // Bị bắn thì buộc đuổi Player, không phụ thuộc detectionRange hoặc LOS ban đầu.
        _forcedByAlarm = true;

        _lastSeenTime = Time.time;
        _lastKnownPlayerPosition = player.position;
        _hasLineOfSightNow = true;
        _mode = ZombieMode.Chase;

        if (agent != null &&
            agent.isActiveAndEnabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.stoppingDistance = attackRange;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.SetDestination(player.position);
        }

        if (anim != null)
            anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);

        Debug.Log(
            $"[ZombieBase] {name} bị bắn và bắt đầu đuổi theo {playerObject.name}.");
    }

    // ── Public API ───────────────────────────────────────────────────────────
    /// <summary>
    /// Gọi bởi ZombiePool ngay sau khi Warp() agent tới vị trí spawn mới.
    /// QUAN TRỌNG: phải gọi SAU khi agent đã được enable + warp, vì hàm này
    /// lấy transform.position hiện tại làm tâm wander mới (_wanderOrigin).
    /// Subclass có state riêng (Rage Mode, Frenzy, đã drop item...) PHẢI override
    /// và gọi base.ResetForPool() trước, rồi reset thêm biến riêng của nó.
    /// </summary>
    public virtual void ResetForPool()
    {
        // 1. Hồi máu + huỷ cờ chết
        if (healthSystem != null)
            healthSystem.ResetHealth();
        _isDead = false;

        // 2. Bật lại Collider (Die() đã tắt khi chết)
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // 3. Bật lại NavMeshAgent (Die() đã disable) - Pool phải gọi Warp() SAU dòng này
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
            if (agent.hasPath) agent.ResetPath();
        }

        // 4. Xoá sạch trigger Animator còn treo lại (IsDead, Attack, Scream...)
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // 5. Reset toàn bộ state AI về như lúc mới spawn lần đầu
        _mode = ZombieMode.Patrol;
        _hasDetectedPlayer = false;
        _screamDone = false;
        _screamPhase = ScreamPhase.None;
        _inCombat = false;
        _forcedByAlarm = false;

        // 6. Reset wander quanh vị trí spawn MỚI (đã được ZombiePool warp tới ở bước 3)
        _wanderOrigin = transform.position;
        _isWanderIdle = false;
        _wanderDestinationSet = false;
        SetNewWanderDestination();
    }

    public void ForceAlert()
    {
        _hasDetectedPlayer = true;
        _screamDone = true;
        _screamPhase = ScreamPhase.None;
    }

    // Gọi từ AlarmSystem ngay sau khi Instantiate zombie mới.
    // Buộc zombie lao thẳng tới player bất kể khoảng cách/LOS ban đầu,
    // cho tới khi thực sự phát hiện player theo cách bình thường (CanDetectPlayer),
    // lúc đó cơ chế scream/combat gốc sẽ tự tiếp quản.
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
        if (_isDead || healthSystem == null)
            return;

        if (attacker == null)
            attacker = GameObject.FindGameObjectWithTag("Player");

        healthSystem.TakeDamage(damage, attacker);
        audioController?.PlayHurt();
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

        DropLoot();
    }

    public virtual void ResetToNormalCombatState()
    {
    }

    protected virtual void DropLoot()
    {
        if (lootTable == null || lootTable.Count == 0) return;

        // Vị trí rớt từ bụng/ngực zombie
        Vector3 spawnPos = transform.position + Vector3.up * 1.0f;

        foreach (var loot in lootTable)
        {
            if (Random.value <= loot.dropChance)
            {
                int amountToDrop = Random.Range(loot.minAmount, loot.maxAmount + 1);

                for (int i = 0; i < amountToDrop; i++)
                {
                    Vector3 randomDirection = new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(0.5f, 1.5f),
                        Random.Range(-1f, 1f)
                    ).normalized;

                    GameObject droppedObject = Instantiate(loot.itemPrefab, spawnPos, Quaternion.identity);

                    Rigidbody rb = droppedObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        float dropForce = Random.Range(2f, 4f);
                        rb.AddForce(randomDirection * dropForce, ForceMode.Impulse);
                        rb.AddTorque(Random.insideUnitSphere * Random.Range(1f, 3f), ForceMode.Impulse);
                    }
                }
            }
        }
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

        // MỚI THÊM: vẽ tia mắt zombie để dễ debug LOS trong Scene view
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position + Vector3.up * eyeHeight, 0.05f);
    }
}