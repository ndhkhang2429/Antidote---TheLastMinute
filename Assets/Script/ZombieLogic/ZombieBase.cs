using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieBase : MonoBehaviour
{
    [Header("Components")]
    protected NavMeshAgent agent;
    protected Animator anim;
    protected Transform player;
    protected HealthSystem healthSystem;

    [Header("Stats")]
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;
    public float walkSpeed = 1f;
    public float runSpeed = 3.5f;
    public float detectionRange = 10f;
    public float attackRange = 2f;

    [Header("Patrol")]
    public Transform[] waypoints;
    protected int currentWaypointIndex = 0;

    [Header("Head Look")]
    public float headLookWeight = 0.7f;  // 0 = không nhìn, 1 = nhìn hoàn toàn
    public Vector3 headLookOffset = new Vector3(0, 1.5f, 0); // Nhìn vào ngực player

    [Header("Scream Settings")]
    public float screamDuration = 2f;
    public float turnSpeed = 10f;
    public float turnThreshold = 0.95f;

    // ── Private ─────────────────────────────────────────────
    private Node _root;
    protected float _nextAttackTime = 0f;
    protected bool _isDead = false;

    private bool _hasDetectedPlayer = false;
    private bool _screamDone = false;
    private float _screamTimer = 0f;

    // Giai đoạn scream tách thành 3 bước rõ ràng
    private enum ScreamPhase { None, Turning, Screaming }
    private ScreamPhase _screamPhase = ScreamPhase.None;

    // ────────────────────────────────────────────────────────

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();

        if (healthSystem == null)
            Debug.LogError($"[ZombieBase] {gameObject.name} thiếu HealthSystem!");

        healthSystem.OnDeath += Die;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        _root = BuildTree();
        GoToNextWaypoint();
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnDeath -= Die;
    }

    protected virtual void Update()
    {
        if (_isDead || player == null) return;

        // Đang quay hoặc hét → block BT hoàn toàn
        if (_screamPhase != ScreamPhase.None)
        {
            HandleScreamPhase();
            return;
        }

        _root.Evaluate();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (player == null || _isDead) return;

        // Chỉ ngước nhìn player khi Chase, không khi Patrol
        bool isChasing = _hasDetectedPlayer && _screamDone;

        if (isChasing)
        {
            Vector3 lookTarget = player.position + headLookOffset;

            anim.SetLookAtWeight(headLookWeight, 0.3f, 0.7f);
            anim.SetLookAtPosition(lookTarget);
        }
        else
        {
            // Patrol → nhìn thẳng phía trước
            anim.SetLookAtWeight(0f);
        }
    }

    // ── Xử lý từng giai đoạn Scream ─────────────────────────

    private void HandleScreamPhase()
    {
        // Luôn đứng yên trong suốt quá trình
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        agent.updatePosition = false;
        agent.updateRotation = false;
        anim.SetFloat("Speed", 0f);

        if (_screamPhase == ScreamPhase.Turning)
        {
            // Quay mặt về phía player
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * turnSpeed);

            // Kiểm tra đã quay đủ chưa bằng dot product
            float dot = Vector3.Dot(transform.forward, dir);
            if (dot >= turnThreshold)
            {
                // Quay xong → chuyển sang giai đoạn hét
                _screamPhase = ScreamPhase.Screaming;
                _screamTimer = 0f;
                anim.SetTrigger("Scream");
                Debug.Log("[ZombieBase] Quay xong → Bắt đầu hét!");
            }
        }
        else if (_screamPhase == ScreamPhase.Screaming)
        {
            // Đang hét → đếm thời gian
            _screamTimer += Time.deltaTime;

            if (_screamTimer >= screamDuration)
            {
                // Hét xong → cho BT chạy tiếp
                _screamPhase = ScreamPhase.None;
                _screamDone = true;

                agent.updateRotation = true;
                agent.updatePosition = true;
                OnScreamComplete();
                Debug.Log("[ZombieBase] Hét xong → Chase/Attack!");
            }
        }
    }

    // ── Xây dựng Behaviour Tree ──────────────────────────────

    protected virtual Node BuildTree()
    {
        return new Selector(new List<Node>
        {
            new Sequence(new List<Node>
            {
                new ConditionNode(CanDetectPlayer),
                new ActionNode(Scream),
                new Selector(new List<Node>
                {
                    new Sequence(new List<Node>
                    {
                        new ConditionNode(IsInAttackRange),
                        new ActionNode(Attack)
                    }),
                    new ActionNode(Chase)
                })
            }),
            new ActionNode(Patrol)
        });
    }

    // ── Conditions ───────────────────────────────────────────

    protected bool CanDetectPlayer()
    {
        if (player == null) return false;

        bool inRange = Vector3.Distance(transform.position, player.position)
                       <= detectionRange;

        // Mất dấu → reset để hét lại lần sau
        if (!inRange && _hasDetectedPlayer)
        {
            _hasDetectedPlayer = false;
            _screamDone = false;
            _screamPhase = ScreamPhase.None;
            _screamTimer = 0f;
        }

        return inRange;
    }

    protected virtual bool IsInAttackRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    // ── Actions ──────────────────────────────────────────────

    protected NodeState Patrol()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return NodeState.Running;
        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0f;
        anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);

        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
            GoToNextWaypoint();

        return NodeState.Running;
    }

    protected NodeState Scream()
    {
        // Lần đầu phát hiện → bắt đầu giai đoạn quay mặt
        if (!_hasDetectedPlayer)
        {
            _hasDetectedPlayer = true;
            _screamDone = false;
            _screamPhase = ScreamPhase.Turning; // Quay trước!
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            Debug.Log("[ZombieBase] Phát hiện player → Bắt đầu quay mặt!");
            return NodeState.Running;
        }

        // Đang trong ScreamPhase → chờ HandleScreamPhase() xử lý
        if (_screamPhase != ScreamPhase.None)
            return NodeState.Running;

        // Chưa hét xong
        if (!_screamDone)
            return NodeState.Running;

        // Hét xong → cho BT chạy tiếp Chase/Attack
        return NodeState.Success;
    }

    protected NodeState Chase()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return NodeState.Running;
        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.stoppingDistance = attackRange;
        agent.SetDestination(player.position);
        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);

        if (Vector3.Distance(transform.position, player.position)
            > detectionRange * 1.5f)
        {
            GoToNextWaypoint();
            return NodeState.Failure;
        }

        return NodeState.Running;
    }

    protected virtual NodeState Attack()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return NodeState.Running;
        agent.isStopped = true;
        anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
        FacePlayer();

        if (Time.time >= _nextAttackTime)
        {
            anim.SetTrigger("Attack");
            _nextAttackTime = Time.time + attackCooldown;
        }

        return NodeState.Running;
    }

    // ── Helpers ──────────────────────────────────────────────

    protected void FacePlayer(bool instant = false)
    {
        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0; // Đảm bảo không ngửa lên/cúi xuống trục Y

        if (dir == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);

        if (instant)
        {
            // Xoay ngay lập tức không delay (Thích hợp cho lúc nã đạn)
            transform.rotation = targetRotation;
        }
        else
        {

            // Tăng tốc độ xoay một chút để bớt lờ đờ, hoặc bạn có thể tăng turnSpeed trên Inspector
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * (turnSpeed * 1.5f));
        }
    }

    protected void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        agent.destination = waypoints[currentWaypointIndex].position;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    protected virtual void OnScreamComplete() { }

    // ── Combat ───────────────────────────────────────────────
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

        // Bị đánh → bỏ qua scream, chase thẳng
        _hasDetectedPlayer = true;
        _screamDone = true;
        _screamPhase = ScreamPhase.None;
    }

    public virtual void DealDamageToPlayer()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > attackRange * 1.2f)
            return;

        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        playerHealth?.TakeDamage(attackDamage, gameObject);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}