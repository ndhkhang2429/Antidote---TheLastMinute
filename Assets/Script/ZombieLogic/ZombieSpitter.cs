using UnityEngine;

/// <summary>
/// ZombieSpitter – Zombie bắn acid từ xa.
///
/// Chỉ override UpdateCombatBehaviour() — không đụng đến BT.
/// State machine nội bộ đơn giản, đứng tại chỗ ném:
///
///   Aiming → Attacking (đứng yên, phóng projectile)
///          → Cooldown  (đứng yên chờ, KHÔNG strafe)
///          → Aiming → ...
///
/// Nếu player ra khỏi attackRange ở bất kỳ pha nào → tự động chase lại gần.
/// </summary>
public class ZombieSpitter : ZombieBase
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Projectile")]
    public GameObject acidProjectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;

    [Header("Cooldown (đứng yên chờ giữa 2 lần ném)")]
    public float minCooldown = 1.5f;
    public float maxCooldown = 3.0f;

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

    // ── Init ─────────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();
        if (anim != null) anim.applyRootMotion = false;
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
        // Player ra ngoài attackRange (dù đang ở pha nào) → chase lại gần
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackRange)
        {
            ResumeAgent(runSpeed);
            agent.SetDestination(player.position);
            agent.stoppingDistance = attackRange;
            anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);

            // Nếu đang giữa chừng ngắm/chờ mà player chạy ra xa, reset về Aiming
            // để khi vào lại tầm sẽ ngắm lại đàng hoàng thay vì bắn ngay lập tức
            if (_state != SpitterState.Attacking) _state = SpitterState.Aiming;
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

    // ── Pha 3: Cooldown (đứng yên chờ, KHÔNG di chuyển) ─────────────────────
    private void EnterCooldown()
    {
        _state = SpitterState.Cooldown;
        _cooldownEndTime = Time.time + Random.Range(minCooldown, maxCooldown);
        StopAgentCompletely();
    }

    private void HandleCooldown()
    {
        // Đứng yên tại chỗ, chỉ nhìn theo player chờ hết giờ rồi ngắm lại
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
        FacePlayer();

        if (Time.time >= _cooldownEndTime)
            _state = SpitterState.Aiming;
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

        audioController?.PlayAttack();
    }
}