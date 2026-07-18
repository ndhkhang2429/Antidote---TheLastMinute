using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ZombieRunner - Phiên bản tối giản.
/// Cơ chế: Tuần tra (Base) -> Phát hiện Player -> Hét -> Chạy cực nhanh -> Đánh nhanh/liên tục.
/// Không sử dụng Pounce hay Frenzy.
/// </summary>
public class ZombieRunner : ZombieBase
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Runner Configuration")]
    public float runnerScreamDuration = 0.8f;   // Thời gian đứng hét trước khi chạy
    public float alertRadius = 15f;             // Bán kính gọi đồng bọn khi hét

    // ── Init ─────────────────────────────────────────────────────────────────
    protected override void Start()
    {
        // Cấu hình chỉ số cho Runner: Tốc độ bứt tốc cao, đánh cực nhanh
        attackDamage = 15f;      // Đổi sang sát thương đòn đánh tay
        attackCooldown = 0.4f;   // Đánh cực nhanh (thay vì 0.8s như trước)
        walkSpeed = 1.5f;        // Tốc độ lúc Patrol
        runSpeed = 8.5f;         // Tốc độ Chase cực cao (tăng từ 6f lên 8.5f)
        detectionRange = 15f;
        attackRange = 1.8f;
        screamDuration = runnerScreamDuration; // Để ZombieBase xử lý thời gian đứng hét

        base.Start();

        // Tắt Root Motion để tránh NavMeshAgent bị khựng khi chuyển animation đột ngột
        if (anim != null) anim.applyRootMotion = false;
    }

    // ── Hook: Phát hiện Player -> Hét và gọi bầy ─────────────────────────────
    protected override void OnEnterCombat()
    {
        // ZombieBase sẽ tự lo việc dừng lại và chạy Animation Hét (dựa vào screamDuration).
        // Ở đây chúng ta chỉ cần phát tín hiệu đánh thức các zombie xung quanh.
        AlertNearbyZombies();
    }

    private void AlertNearbyZombies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius);
        int alerted = 0;
        foreach (Collider hit in hits)
        {
            ZombieBase z = hit.GetComponent<ZombieBase>() ?? hit.GetComponentInParent<ZombieBase>();
            if (z != null && z != this)
            {
                z.ForceAlert();
                alerted++;
            }
        }
        if (alerted > 0)
            Debug.Log($"[ZombieRunner] Đã gầm lên và đánh thức {alerted} đồng bọn!");
    }

    // ── Combat State Machine ─────────────────────────────────────────────────
    // Hàm này chỉ chạy sau khi Zombie đã hét xong (quản lý bởi ZombieBase)
    protected override void UpdateCombatBehaviour()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        // Trạng thái 1: Tấn công nhanh (Fast Melee)
        if (dist <= attackRange)
        {
            StopAgentCompletely();
            anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
            FacePlayer();

            if (Time.time >= _nextAttackTime)
            {
                anim.SetTrigger("Attack"); // Cần gán Animation vung tay đánh nhanh trong Animator
                _nextAttackTime = Time.time + attackCooldown;
            }
            return;
        }

        // Trạng thái 2: Truy đuổi tốc độ cao (High-speed Chase)
        ResumeAgent(runSpeed);
        agent.SetDestination(player.position);
        agent.stoppingDistance = attackRange;

        // Truyền param Speed vào Animator để chạy animation Sprint
        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
    }

    // ── Animation Event ──────────────────────────────────────────────────────
    // Hàm này phải được gọi thông qua Animation Event tại đúng frame tay đập trúng Player
    public override void DealDamageToPlayer()
    {
        if (player == null) return;

        // Kiểm tra lại khoảng cách phòng trường hợp Player đã né được
        if (Vector3.Distance(transform.position, player.position) > attackRange * 1.2f) return;

        HealthSystem ph = player.GetComponent<HealthSystem>() ?? player.GetComponentInParent<HealthSystem>();
        ph?.TakeDamage(attackDamage, gameObject);

        Debug.Log($"[ZombieRunner] Cào trúng đích! Sát thương: {attackDamage}");
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}