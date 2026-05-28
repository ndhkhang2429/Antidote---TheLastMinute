using UnityEngine;
using UnityEngine.AI;

public class ZombieSpitter : ZombieBase
{
    [Header("Spitter Settings")]
    public GameObject acidProjectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;

    [Tooltip("Thời gian dự phòng nếu Animation Event không kích hoạt")]
    public float fallbackAttackDuration = 1.5f;

    [Header("Strafe Settings")]
    public float strafeSpeed = 2.5f;
    public float minStrafeTime = 1.5f;
    public float maxStrafeTime = 3.5f;
    public float arcLeadAngle = 25f;

    // ── State Machine ─────────────────────────────────────────
    private enum SpitterState { Aiming, Attacking, Strafing }
    private SpitterState _state = SpitterState.Aiming;

    private int _strafeDir = 1;
    private float _strafeEndTime = 0f;
    private float _attackEndTime = 0f;
    private bool _attackAnimationFinished = false;

    // Timer chống lỗi cập nhật mỗi frame
    private float _lastAttackFrameTime = 0f;
    private float _lastPathUpdateTime = 0f;

    protected override void Start()
    {
        base.Start();
        PickNewStrafeDirection();

        if (anim != null)
            anim.applyRootMotion = false; // Tắt Root motion để tránh lệch anim
    }

    // ── FIX BUG: Nới rộng tầm kiểm tra khi đang chiến đấu (Hysteresis) ──
    protected override bool IsInAttackRange()
    {
        if (player == null) return false;
        float dist = Vector3.Distance(transform.position, player.position);

        // Nếu zombie đang trong quá trình luân chuyển Aim/Attack/Strafe,
        // cho phép nó đi lệch ra khỏi attackRange tới 30% (buffer) mà không bị rớt về node Chase.
        float currentRange = (_state == SpitterState.Strafing) ? attackRange * 1.3f : attackRange;
        return dist <= currentRange;
    }

    protected override NodeState Attack()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return NodeState.Running;

        // Nếu BT vừa thoát ra Chase rồi nhảy lại vào Attack (cách nhau > 2 frame), reset State về Aiming
        if (Time.time - _lastAttackFrameTime > Time.deltaTime * 2f)
        {
            EnterAiming();
        }
        _lastAttackFrameTime = Time.time;

        switch (_state)
        {
            case SpitterState.Aiming: return HandleAiming();
            case SpitterState.Attacking: return HandleAttacking();
            case SpitterState.Strafing: return HandleStrafing();
        }

        return NodeState.Running;
    }

    // ── PHA 1: AIMING ─────────────────────────────────────────
    private void EnterAiming()
    {
        _state = SpitterState.Aiming;
        StopAgentCompletely(); // Chỉ gọi 1 lần khi bắt đầu Aim
    }

    private NodeState HandleAiming()
    {
        anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
        FacePlayer(false);

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;

        // Ngắm trúng mục tiêu -> Chuyển sang Attack
        if (Vector3.Dot(transform.forward, dir) >= 0.95f)
        {
            EnterAttacking();
        }

        return NodeState.Running;
    }

    // ── PHA 2: ATTACKING ──────────────────────────────────────
    private void EnterAttacking()
    {
        _state = SpitterState.Attacking;
        StopAgentCompletely(); // Chỉ gọi 1 lần khi bắt đầu Attack

        _attackEndTime = Time.time + fallbackAttackDuration;
        _attackAnimationFinished = false;
        anim.SetTrigger("Attack");
    }

    private NodeState HandleAttacking()
    {
        anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
        FacePlayer(true);

        // Chờ kết thúc animation (hoặc hết thời gian fallback)
        if (_attackAnimationFinished || Time.time >= _attackEndTime)
        {
            EnterStrafing();
        }

        return NodeState.Running;
    }

    // ── PHA 3: STRAFING ───────────────────────────────────────
    private void EnterStrafing()
    {
        _state = SpitterState.Strafing;

        // Bật lại di chuyển 1 lần duy nhất
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = strafeSpeed;
            agent.stoppingDistance = 0f;
        }

        _strafeEndTime = Time.time + Random.Range(minStrafeTime, maxStrafeTime);
        PickNewStrafeDirection();

        // Ép update path ngay lập tức ở frame đầu tiên
        _lastPathUpdateTime = 0f;
    }

    private NodeState HandleStrafing()
    {
        if (Time.time >= _strafeEndTime)
        {
            EnterAiming();
            return NodeState.Running;
        }

        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
        MoveAlongCircle();

        return NodeState.Running;
    }

    // ── KHOÁ AGENT (Chống trượt) ──────────────────────────────
    private void StopAgentCompletely()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath(); // Xoá lộ trình cũ dập tắt quán tính
                agent.velocity = Vector3.zero;
            }
        }
    }

    // ── Di chuyển theo cung tròn (Tối ưu NavMesh) ──────────────
    private void MoveAlongCircle()
    {
        if (player == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        // CHỐNG GIẬT CỤC: Chỉ tính toán lại đường đi mỗi 0.2 giây thay vì mỗi frame
        if (Time.time - _lastPathUpdateTime < 0.2f) return;
        _lastPathUpdateTime = Time.time;

        Vector3 toZombie = transform.position - player.position;
        toZombie.y = 0f;

        Vector3 idealPos = player.position + toZombie.normalized * attackRange;
        Vector3 tangent = Quaternion.Euler(0, 90f * _strafeDir, 0) * toZombie.normalized;

        // Tính vị trí đích mới
        Vector3 target = idealPos + tangent * (arcLeadAngle * 0.1f);

        agent.SetDestination(target);
    }

    // ── ANIMATION EVENTS ──────────────────────────────────────
    public override void DealDamageToPlayer()
    {
        if (player == null || _isDead) return;
        if (acidProjectilePrefab == null || firePoint == null) return;

        Vector3 targetPos = player.position + Vector3.up * 1.5f;
        Vector3 shootDir = (targetPos - firePoint.position).normalized;

        GameObject acidObj = Instantiate(acidProjectilePrefab, firePoint.position, Quaternion.LookRotation(shootDir));
        acidObj.GetComponent<AcidProjectile>()?.Setup(shootDir, attackDamage, projectileSpeed);
    }

    // Gọi hàm này ở frame cuối cùng của clip Attack bằng Animation Event
    public void OnAttackAnimationFinished()
    {
        _attackAnimationFinished = true;
    }

    private void PickNewStrafeDirection()
    {
        _strafeDir = Random.value > 0.5f ? 1 : -1;
    }
}