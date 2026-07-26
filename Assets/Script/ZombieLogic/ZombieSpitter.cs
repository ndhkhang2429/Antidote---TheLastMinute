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
/// Nếu player ra khỏi attackRange, hoặc bị tường che (mất Line of Sight),
/// ở bất kỳ pha nào → tự động huỷ ngắm/bắn và chase lại gần / tìm góc bắn mới.
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
        float dist = Vector3.Distance(transform.position, player.position);

        // MỚI THÊM: điều kiện để được đứng yên bắn giờ là "trong tầm VÀ có Line of Sight",
        // không chỉ dựa vào khoảng cách như trước (đó là lý do spitter từng ném xuyên tường)
        bool canAttackFromHere = dist <= attackRange && HasLineOfSightNow;

        if (!canAttackFromHere)
        {
            // Nếu đang giữa chừng ngắm/chờ mà mất điều kiện bắn, huỷ về Aiming
            // để khi đủ điều kiện trở lại sẽ ngắm lại đàng hoàng thay vì bắn ngay lập tức
            if (_state != SpitterState.Attacking) _state = SpitterState.Aiming;

            ResumeAgent(runSpeed);
            agent.stoppingDistance = attackRange;
            // Đuổi tới vị trí đang thấy player, hoặc vị trí cuối cùng nhớ được nếu vừa mất LOS
            // (giúp spitter tự đi vòng ra góc khác thay vì đứng yên ném xuyên tường)
            agent.SetDestination(HasLineOfSightNow ? player.position : LastKnownPlayerPosition);
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

        // Ngắm xong VÀ vẫn còn Line of Sight lúc chuyển pha → chuyển sang Attack
        // (MỚI THÊM: check lại HasLineOfSightNow ở đây, vì player có thể vừa
        // núp sau tường ngay trong lúc zombie đang xoay người ngắm)
        if (Vector3.Dot(transform.forward, dir) >= aimThreshold && HasLineOfSightNow)
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

        // MỚI THÊM: nếu player núp mất giữa lúc animation đang chạy (trước khi
        // Animation Event bắn ra), huỷ luôn phát bắn thay vì bắn xuyên tường theo quán tính
        if (!HasLineOfSightNow)
        {
            _state = SpitterState.Aiming;
            return;
        }

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

        // MỚI THÊM: chốt chặn cuối cùng — nếu vì lý do gì đó (animation event
        // bắn ra đúng frame player vừa núp) mà mất LOS, huỷ phát bắn hoàn toàn
        if (!HasLineOfSightNow)
        {
            _shotFired = true; // vẫn coi như đã "dùng" lượt animation này để không kẹt state
            return;
        }

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

    public override void ResetForPool()
    {
        base.ResetForPool();

        // Reset state machine riêng của Spitter về trạng thái ban đầu
        _state = SpitterState.Aiming;
        _shotFired = false;
        _attackEndTime = 0f;
        _cooldownEndTime = 0f;
    }
}