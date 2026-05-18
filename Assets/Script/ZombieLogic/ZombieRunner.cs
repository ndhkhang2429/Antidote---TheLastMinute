using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zombie Runner: nhanh, gầy, tấn công liên tục.
/// Cơ chế đặc biệt:
/// - Leap Attack: nhảy vồ theo arc khi cách player 4-8m
/// - Frenzy: tăng tốc + nhấp nháy đỏ khi HP < 30%
/// - Alert: hét kéo zombie xung quanh khi phát hiện player
/// </summary>
public class ZombieRunner : ZombieBase
{
    [Header("Runner Stats")]
    public float frenzySpeedMultiplier = 1.5f;
    public float frenzyThreshold = 0.3f;
    public float alertRadius = 15f;

    [Header("Leap Attack")]
    public float leapMinRange = 4f;
    public float leapMaxRange = 8f;
    public float leapCooldown = 5f;
    public float leapDamage = 30f;
    public float leapKnockback = 5f;
    public float leapDuration = 0.6f;
    public float leapArcHeight = 3f;  // Chiều cao arc khi nhảy

    // ── Private ─────────────────────────────────────────────
    private bool _isFrenzy = false;
    private bool _isLeaping = false;
    private float _nextLeapTime = 0f;
    private float _originalRunSpeed;

    // ────────────────────────────────────────────────────────

    protected override void Start()
    {
        base.Start();

        attackDamage = 12f;
        attackCooldown = 0.8f;
        walkSpeed = 2f;
        runSpeed = 6f;
        detectionRange = 15f;
        attackRange = 1.8f;

        _originalRunSpeed = runSpeed;
    }

    // ── Update: check Frenzy trước khi tick BT ──────────────

    protected override void Update()
    {
        if (!_isDead)
            CheckFrenzy();

        base.Update();
    }

    // ── Override BuildTree: thêm nhánh Leap ─────────────────

    protected override Node BuildTree()
    {
        return new Selector(new List<Node>
        {
            // Nhánh 1: Thấy player
            new Sequence(new List<Node>
            {
                new ConditionNode(CanDetectPlayer),
                new ActionNode(Scream),
                new Selector(new List<Node>
                {
                    // Ưu tiên 1: Leap nếu đủ điều kiện
                    new Sequence(new List<Node>
                    {
                        new ConditionNode(CanLeap),
                        new ActionNode(LeapAttack)
                    }),

                    // Ưu tiên 2: Tấn công thường
                    new Sequence(new List<Node>
                    {
                        new ConditionNode(IsInAttackRange),
                        new ActionNode(Attack)
                    }),

                    // Ưu tiên 3: Đuổi theo
                    new ActionNode(Chase)
                })
            }),

            // Nhánh 2: Tuần tra
            new ActionNode(Patrol)
        });
    }

    // ── Frenzy ───────────────────────────────────────────────

    private void CheckFrenzy()
    {
        if (_isFrenzy || healthSystem == null) return;
        if (healthSystem.HPPercent >= frenzyThreshold) return;

        _isFrenzy = true;
        runSpeed = _originalRunSpeed * frenzySpeedMultiplier;
        attackCooldown *= 0.5f;

        StartCoroutine(FrenzyVisualEffect());
        Debug.Log($"[ZombieRunner] {gameObject.name} FRENZY MODE!");
    }

    private IEnumerator FrenzyVisualEffect()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        while (_isFrenzy && !_isDead)
        {
            float pulse = (Mathf.Sin(Time.time * 10f) + 1f) / 2f;
            Color frenzyColor = Color.Lerp(Color.white, Color.red, pulse * 0.4f);

            foreach (var r in renderers)
                foreach (var mat in r.materials)
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", frenzyColor);

            yield return null;
        }

        // Reset màu khi chết
        foreach (var r in renderers)
            foreach (var mat in r.materials)
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.white);
    }

    // ── Leap Attack ──────────────────────────────────────────

    private bool CanLeap()
    {
        if (_isLeaping) return false;
        if (Time.time < _nextLeapTime) return false;
        if (player == null) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        return dist >= leapMinRange && dist <= leapMaxRange;
    }

    private NodeState LeapAttack()
    {
        if (!_isLeaping)
        {
            _isLeaping = true;
            _nextLeapTime = Time.time + leapCooldown;
            StartCoroutine(PerformLeap());
        }

        return NodeState.Running;
    }

    private IEnumerator PerformLeap()
    {
        // Dừng NavMesh
        agent.isStopped = true;
        agent.enabled = false;
        anim.SetTrigger("Leap");

        // Chờ animation Leap thực sự bắt đầu
        yield return null; // Đợi 1 frame
        yield return null; // Đợi thêm 1 frame nữa cho chắc

        // Chờ animator chuyển sang state Leap
        float waitTime = 0f;
        while (waitTime < 0.2f &&
               !anim.GetCurrentAnimatorStateInfo(0).IsName("Jump Attack"))
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position;
        float elapsed = 0f;
        bool hitPlayer = false;

        // Tính arc height theo khoảng cách
        float dist = Vector3.Distance(startPos, targetPos);
        float arcHeight = Mathf.Clamp(dist * 0.4f, 1.5f, leapArcHeight);

        // Xoay mặt về player trước khi nhảy
        Vector3 lookDir = (targetPos - startPos).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // Bay theo arc
        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / leapDuration);

            // Vị trí ngang: Lerp từ start → target
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);

            // Vị trí dọc: Sin tạo arc bay lên rồi xuống
            newPos.y += arcHeight * Mathf.Sin(t * Mathf.PI);

            transform.position = newPos;

            // Check hit player trong lúc bay
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer < 1.5f && !hitPlayer)
            {
                hitPlayer = true;
                HitPlayerOnLand();
            }

            yield return null;
        }

        // Snap xuống NavMesh gần nhất
        if (NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit navHit,
            3f,
            NavMesh.AllAreas))
        {
            transform.position = navHit.position;
        }

        // Đợi physics ổn định
        yield return new WaitForSeconds(0.1f);

        // Bật lại NavMesh
        if (!_isDead && gameObject.activeInHierarchy)
        {
            agent.enabled = true;
            yield return null; // Đợi 1 frame

            if (agent.isOnNavMesh)
                agent.isStopped = false;
        }

        _isLeaping = false;
        Debug.Log("[ZombieRunner] Leap xong!");
    }

    private void HitPlayerOnLand()
    {
        if (player == null) return;

        // Gây damage
        HealthSystem playerHealth = player.GetComponent<HealthSystem>()
            ?? player.GetComponentInChildren<HealthSystem>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(leapDamage, gameObject);
            Debug.Log($"[ZombieRunner] Leap trúng! Damage: {leapDamage}");
        }

        // Knockback bằng CharacterController (player không có Rigidbody)
        // Implement sau nếu cần
        Debug.Log("[ZombieRunner] Knockback player!");
    }

    // ── Alert đồng bọn khi Scream xong ──────────────────────

    protected override void OnScreamComplete()
    {
        AlertNearbyZombies();
    }

    private void AlertNearbyZombies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius);
        int alerted = 0;

        foreach (Collider hit in hits)
        {
            ZombieBase zombie = hit.GetComponent<ZombieBase>()
                ?? hit.GetComponentInParent<ZombieBase>();

            if (zombie != null && zombie != this)
            {
                // Force zombie khác alert mà không gây damage
                zombie.ForceAlert();
                alerted++;
            }
        }

        Debug.Log($"[ZombieRunner] Alert {alerted} zombie xung quanh!");
    }

    // ── Gizmos ───────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, leapMinRange);
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, leapMaxRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}