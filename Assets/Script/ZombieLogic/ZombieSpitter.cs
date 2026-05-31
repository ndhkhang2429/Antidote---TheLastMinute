using UnityEngine;

/// <summary>
/// ZombieSpitter – Zombie bắn acid từ xa.
///
/// Chỉ override UpdateCombatBehaviour() — không đụng đến BT.
/// State machine nội bộ hoàn toàn tách biệt:
///
///   Aiming → Attacking (đứng yên, phóng projectile)
///          → Cooldown  (strafe trái/phải trên vòng tròn r=attackRange, tâm=player)
///          → Aiming → ...
/// </summary>
public class ZombieSpitter : ZombieBase
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Projectile")]
    public GameObject acidProjectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;

    [Header("Cooldown Strafe")]
    public float minCooldown = 1.5f;
    public float maxCooldown = 3.0f;
    public float strafeSpeed = 2.5f;

    [Header("Aiming")]
    public float aimTurnSpeed = 8f;
    public float aimThreshold = 0.97f;  // dot product để coi là "đã ngắm xong"

    [Header("Attack Fallback")]
    [Tooltip("Nếu không dùng Animation Event, tự động kết thúc attack sau thời gian này")]
    public float attackFallbackDuration = 1.5f;

    // ── State Machine ────────────────────────────────────────────────────────
    private enum SpitterState { Aiming, Attacking, Cooldown }
    private SpitterState _state = SpitterState.Aiming;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private float _attackEndTime = 0f;
    private float _cooldownEndTime = 0f;
    private bool _shotFired = false;
    private int _strafeDir = 1;       // +1 = phải, -1 = trái

    // Path throttle (tránh giật cục khi SetDestination mỗi frame)
    private float _lastPathTime = 0f;
    private const float PATH_INTERVAL = 0.15f;

    // ── Init ─────────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();
        if (anim != null) anim.applyRootMotion = false;
        PickStrafeDir();
    }

    // ── Hooks từ ZombieBase ───────────────────────────────────────────────────
    protected override void OnEnterCombat()
    {
        // Reset về Aiming mỗi khi bắt đầu combat (kể cả sau khi mất player rồi thấy lại)
        _state = SpitterState.Aiming;
    }

    protected override void OnExitCombat()
    {
        // Dọn dẹp khi mất player
        _state = SpitterState.Aiming;
        StopAgentCompletely();
    }

    // ── Combat State Machine (gọi mỗi frame bởi ZombieBase.ExecuteCombat) ───
    protected override void UpdateCombatBehaviour()
    {
        // Chase thêm nếu player ra xa hơn attackRange (Spitter cần giữ khoảng cách)
        float dist = Vector3.Distance(transform.position, player.position);
        if (_state == SpitterState.Aiming && dist > attackRange)
        {
            // Chưa vào tầm → chase lại
            ResumeAgent(runSpeed);
            agent.SetDestination(player.position);
            agent.stoppingDistance = attackRange;
            anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
            return;
        }

        switch (_state)
        {
            case SpitterState.Aiming: HandleAiming(); break;
            case SpitterState.Attacking: HandleAttacking(); break;
            case SpitterState.Cooldown: HandleCooldown(); break;
        }
    }

    // ── Pha 1: Aiming ────────────────────────────────────────────────────────
    private void HandleAiming()
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);

        // Xoay mặt về player
        Vector3 dir = FlatDir(player.position - transform.position);
        if (dir == Vector3.zero) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * aimTurnSpeed);

        // Ngắm xong → chuyển sang Attack
        if (Vector3.Dot(transform.forward, dir) >= aimThreshold)
            EnterAttacking();
    }

    // ── Pha 2: Attacking ─────────────────────────────────────────────────────
    private void EnterAttacking()
    {
        _state = SpitterState.Attacking;
        _shotFired = false;
        _attackEndTime = Time.time + attackFallbackDuration;

        StopAgentCompletely();      // lock vị trí trước khi trigger
        anim.SetTrigger("Attack");
    }

    private void HandleAttacking()
    {
        // Lock vị trí MỖI FRAME — không để bất kỳ thứ gì dịch chuyển zombie
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
        FacePlayer(instant: true);  // nhìn theo player instant, không lag

        // Chờ AnimEvent hoặc fallback timer
        if (_shotFired || Time.time >= _attackEndTime)
            EnterCooldown();
    }

    // ── Pha 3: Cooldown (Strafe) ──────────────────────────────────────────────
    private void EnterCooldown()
    {
        _state = SpitterState.Cooldown;
        _cooldownEndTime = Time.time + Random.Range(minCooldown, maxCooldown);
        _lastPathTime = 0f;

        PickStrafeDir();
        ResumeAgent(strafeSpeed);
    }

    private void HandleCooldown()
    {
        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
        StrafeOnCircle();

        if (Time.time >= _cooldownEndTime)
            _state = SpitterState.Aiming;
    }

    // ── Circle Strafe ────────────────────────────────────────────────────────
    /// <summary>
    /// Di chuyển trên vòng tròn bán kính attackRange, tâm = player.
    /// Throttle PATH_INTERVAL để tránh giật cục.
    /// </summary>
    private void StrafeOnCircle()
    {
        if (player == null) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        if (Time.time - _lastPathTime < PATH_INTERVAL) return;
        _lastPathTime = Time.time;

        // Radial: từ player → zombie (normalized, XZ)
        Vector3 radial = FlatDir(transform.position - player.position);
        if (radial == Vector3.zero) radial = transform.forward;

        // Tangent: xoay radial 90° theo _strafeDir
        Vector3 tangent = Quaternion.Euler(0f, 90f * _strafeDir, 0f) * radial;

        // Điểm đích = vị trí lý tưởng trên vòng tròn + dịch theo tangent
        Vector3 circlePos = player.position + radial * attackRange;
        Vector3 destination = circlePos + tangent * (strafeSpeed * PATH_INTERVAL * 4f);

        agent.SetDestination(destination);
    }

    // ── Animation Event ──────────────────────────────────────────────────────
    /// <summary>
    /// Đặt Animation Event này ở frame "release" của clip Attack trong Animator.
    /// </summary>
    public override void DealDamageToPlayer()
    {
        if (IsDead || player == null) return;
        if (acidProjectilePrefab == null || firePoint == null) return;

        _shotFired = true;  // báo HandleAttacking kết thúc

        Vector3 targetPos = player.position + Vector3.up * 1.5f;
        Vector3 shootDir = (targetPos - firePoint.position).normalized;

        GameObject acidObj = Instantiate(
            acidProjectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(shootDir));

        acidObj.GetComponent<AcidProjectile>()?.Setup(shootDir, attackDamage, projectileSpeed);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void PickStrafeDir() => _strafeDir = Random.value > 0.5f ? 1 : -1;
}