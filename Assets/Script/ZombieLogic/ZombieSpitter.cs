using UnityEngine;
using UnityEngine.AI;

public class ZombieSpitter : ZombieBase
{
    [Header("Spitter Settings")]
    public GameObject acidProjectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;

    [Tooltip("Thời gian đứng yên hoàn toàn để phát hoạt ảnh bắn")]
    public float attackAnimDuration = 1.5f;

    [Header("Circular Movement Settings")]
    public float strafeSpeed = 2f;
    public float minAttackCooldown = 1.5f;
    public float maxAttackCooldown = 3.5f;
    public float arcLeadAngle = 20f;

    // ── Biến trạng thái nội bộ ────────────────────────────────
    private int _strafeDirection = 1;
    private bool _isAiming = false;
    private bool _isAttacking = false;
    private float _attackEndTime = 0f;

    protected override void Start()
    {
        base.Start();
        SetRandomStrafeData();
    }

    public override void DealDamageToPlayer()
    {
        if (player == null || _isDead) return;

        Vector3 targetPosition = player.position + new Vector3(0, 1.5f, 0);
        Vector3 shootDirection = targetPosition - firePoint.position;

        GameObject acidObj = Instantiate(acidProjectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

        AcidProjectile projectile = acidObj.GetComponent<AcidProjectile>();
        if (projectile != null)
        {
            projectile.Setup(shootDirection, attackDamage, projectileSpeed);
        }
    }

    protected override NodeState Attack()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return NodeState.Running;

        // ── TRẠNG THÁI 1: ĐANG PHÁT ANIMATION BẮN (ĐỨNG YÊN) ──
        if (_isAttacking)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
            FacePlayer(); // Tuỳ chọn: Vẫn khóa mục tiêu trong lúc khạc độc

            // Nếu đã hết thời gian phát animation bắn
            if (Time.time >= _attackEndTime)
            {
                _isAttacking = false;

                // Bắn XONG thì mới bắt đầu tính Cooldown và Random hướng đi
                SetRandomStrafeData();
            }

            return NodeState.Running; // Khóa BT lại ở đây, không chạy xuống dưới
        }

        // ── TRẠNG THÁI 2: HẾT COOLDOWN -> DỪNG LẠI NGẮM BẮN ──
        if (Time.time >= _nextAttackTime || _isAiming)
        {
            if (!_isAiming)
            {
                _isAiming = true;
                agent.isStopped = true;
                agent.updateRotation = false;
            }

            anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
            FacePlayer();

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0;
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

            // Xoay mặt chuẩn -> CHUYỂN SANG TRẠNG THÁI BẮN
            if (angleToPlayer < 5f)
            {
                anim.SetTrigger("Attack");

                _isAiming = false;
                _isAttacking = true; // Bật cờ khóa di chuyển

                // Hẹn giờ kết thúc animation (Bạn có thể tinh chỉnh thông số attackAnimDuration trên Inspector)
                _attackEndTime = Time.time + attackAnimDuration;
            }
        }
        // ── TRẠNG THÁI 3: TRONG COOLDOWN -> DI CHUYỂN VÒNG TRÒN ──
        else
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = strafeSpeed;
            agent.stoppingDistance = 0f;

            anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);

            MoveInCircle();
        }

        return NodeState.Running;
    }

    private void MoveInCircle()
    {
        Vector3 dirFromPlayerToZombie = (transform.position - player.position).normalized;
        dirFromPlayerToZombie.y = 0;

        Vector3 tangentDirection = Quaternion.Euler(0, arcLeadAngle * _strafeDirection, 0) * dirFromPlayerToZombie;
        Vector3 targetPosition = player.position + tangentDirection * attackRange;

        agent.SetDestination(targetPosition);
    }

    private void SetRandomStrafeData()
    {
        // Cooldown bắt đầu được đếm TỪ LÚC BẮN XONG
        _nextAttackTime = Time.time + Random.Range(minAttackCooldown, maxAttackCooldown);
        _strafeDirection = Random.value > 0.5f ? 1 : -1;
    }
}