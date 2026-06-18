using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MutatedBossZombie : ZombieBase
{
    // Định nghĩa các trạng thái Combat nội bộ của riêng Boss
    protected enum BossCombatState
    {
        None,
        P1_Normal, P1_Stomp, P1_Summon,
        Transition,
        P2_Normal, P2_Charge, P2_Leap, P2_Frenzy
    }

    [Header("== BOSS MODELS & PHASES ==")]
    [SerializeField] private GameObject modelV2;
    [SerializeField] private GameObject modelV3;
    [SerializeField] private Transform[] minionSpawnPoints;
    [SerializeField] private GameObject minionPrefab;

    [Header("Phase 1 Cooldowns")]
    [SerializeField] private float stompCooldown = 8f;
    [SerializeField] private float summonCooldown = 20f;

    [Header("Phase 2 Cooldowns")]
    [SerializeField] private float chargeCooldown = 12f;
    [SerializeField] private float leapCooldown = 15f;
    [SerializeField] private float frenzyCooldown = 5f;

    [Header("Phase 2 Stats Boost")]
    [SerializeField] private float enragedRunSpeed = 6f;
    [SerializeField] private float chargeSpeed = 16f;

    // Các biến trạng thái nội bộ
    private BossCombatState _bossState = BossCombatState.None;
    private bool _isPhase2 = false;

    // Bộ đếm thời gian hồi chiêu (Timers)
    private float _stompTimer = 0f;
    private float _summonTimer = 0f;
    private float _chargeTimer = 0f;
    private float _leapTimer = 0f;
    private float _frenzyTimer = 0f;

    private Vector3 _chargeTargetPos;
    private bool _isStunned = false;

    // ── Khởi tạo ─────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start(); // Gọi khởi tạo cấu trúc BT của lớp cha

        // Đảm bảo ban đầu chỉ hiển thị Model dạng bình thường (V2)
        if (modelV2 != null) modelV2.SetActive(true);
        if (modelV3 != null) modelV3.SetActive(false);

        // Đẩy timer lên trước để Boss có thể dùng chiêu ngay khi vào trận nếu đủ điều kiện
        _stompTimer = stompCooldown;
        _summonTimer = summonCooldown - 5f; // Chờ 5s sau khi hú mới gọi đệ
        _chargeTimer = chargeCooldown;
        _leapTimer = leapCooldown;
        _frenzyTimer = frenzyCooldown;
    }

    // ── Override Hooks từ Lớp Cha ────────────────────────────────────────────

    protected override void OnEnterCombat()
    {
        // Khi lớp cha báo hiệu chính thức vào Combat (Scream xong)
        if (!_isPhase2)
            _bossState = BossCombatState.P1_Normal;
        else
            _bossState = BossCombatState.P2_Normal;
    }

    protected override void OnExitCombat()
    {
        // Khi mất dấu người chơi
        _bossState = BossCombatState.None;
    }

    protected override void Update()
    {
        base.Update(); // Chạy BT ở lớp cha để tính toán Mode lớn

        if (_isDead || player == null || _mode != ZombieMode.Combat || !ScreamDone) return;

        // Cập nhật thời gian hồi chiêu theo thời gian thực
        _stompTimer += Time.deltaTime;
        _summonTimer += Time.deltaTime;
        _chargeTimer += Time.deltaTime;
        _leapTimer += Time.deltaTime;
        _frenzyTimer += Time.deltaTime;
    }

    /// <summary>
    /// Hàm cốt lõi: Override logic combat mặc định của lớp cha thành State Machine của Boss
    /// </summary>
    protected override void UpdateCombatBehaviour()
    {
        if (_isStunned || _bossState == BossCombatState.Transition) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (_bossState)
        {
            // ==================== PHASE 1 LOGIC ====================
            case BossCombatState.P1_Normal:
                // Ưu tiên 1: Gọi đệ (Summon)
                if (_summonTimer >= summonCooldown)
                {
                    ExecuteP1_Summon();
                }
                // Ưu tiên 2: Dậm đất khi người chơi lại quá gần (Stomp)
                else if (distanceToPlayer <= 4.5f && _stompTimer >= stompCooldown)
                {
                    ExecuteP1_Stomp();
                }
                // Mặc định: Tiếp cận người chơi bằng tốc độ chạy thông thường
                else
                {
                    ResumeAgent(runSpeed);
                    agent.stoppingDistance = attackRange;
                    agent.SetDestination(player.position);
                    anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
                }
                break;

            // ==================== PHASE 2 LOGIC ====================
            case BossCombatState.P2_Normal:
                // Ưu tiên 1: Nhảy bổ cự ly xa (Leap Smash)
                if (distanceToPlayer >= 14f && _leapTimer >= leapCooldown)
                {
                    ExecuteP2_Leap();
                }
                // Ưu tiên 2: Lao tới đâm húc cự ly tầm trung (Charge)
                else if (distanceToPlayer >= 6f && distanceToPlayer < 14f && _chargeTimer >= chargeCooldown)
                {
                    ExecuteP2_Charge();
                }
                // Ưu tiên 3: Chuỗi cào xé điên cuồng cự ly cận chiến (Frenzy Swipes)
                else if (distanceToPlayer <= attackRange && _frenzyTimer >= frenzyCooldown)
                {
                    ExecuteP2_Frenzy();
                }
                // Mặc định: Áp sát điên cuồng (Enraged Run)
                else
                {
                    ResumeAgent(enragedRunSpeed);
                    agent.stoppingDistance = attackRange;
                    agent.SetDestination(player.position);
                    anim.SetFloat("Speed", 2.5f, 0.1f, Time.deltaTime); // Kích tốc độ chạy animation lên
                }
                break;

            case BossCombatState.P2_Charge:
                // Logic xử lý khi đang trong trạng thái lao tới đâm húc
                UpdateChargeMovement();
                break;
        }
    }

    // ── Xử Lý Các Đòn Đánh Phase 1 ───────────────────────────────────────────

    private void ExecuteP1_Stomp()
    {
        _bossState = BossCombatState.P1_Stomp;
        _stompTimer = 0f;
        StopAgentCompletely();
        FacePlayer(true);
        anim.SetTrigger("StompTrigger"); // Mixamo: "Mutant Stomp"
    }

    private void ExecuteP1_Summon()
    {
        _bossState = BossCombatState.P1_Summon;
        _summonTimer = 0f;
        StopAgentCompletely();
        anim.SetTrigger("SummonTrigger"); // Mixamo: "Mutant Roar" / "Taunt"
    }

    // ── Xử Lý Các Đòn Đánh Phase 2 ───────────────────────────────────────────

    private void ExecuteP2_Leap()
    {
        _bossState = BossCombatState.P2_Leap;
        _leapTimer = 0f;
        StopAgentCompletely();
        FacePlayer(true);

        // Khóa mục tiêu và dịch chuyển NavMeshAgent hoặc bật root motion
        agent.SetDestination(player.position);
        anim.SetTrigger("LeapTrigger"); // Mixamo: "Jumping Smash"
    }

    private void ExecuteP2_Charge()
    {
        _bossState = BossCombatState.P2_Charge;
        _chargeTimer = 0f;

        // Khóa vị trí của người chơi tại thời điểm ra chiêu
        _chargeTargetPos = player.position;

        ResumeAgent(chargeSpeed);
        agent.stoppingDistance = 0f;
        agent.SetDestination(_chargeTargetPos);

        anim.SetTrigger("ChargeTrigger"); // Mixamo: "Shoulder Tackle" hoặc "Mutant Run" tốc độ cao
    }

    private void UpdateChargeMovement()
    {
        // Nếu đã đến gần điểm khóa mục tiêu ban đầu
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            // Kết thúc đòn húc mà không đâm trúng vật cản cứng
            ResetToNormalCombatState();
        }
    }

    private void ExecuteP2_Frenzy()
    {
        _bossState = BossCombatState.P2_Frenzy;
        _frenzyTimer = 0f;
        StopAgentCompletely();
        FacePlayer();
        anim.SetTrigger("FrenzyTrigger"); // Mixamo: "Frenzy Attack" / "Mutant Swiping"
    }

    // ── Đánh Chặn TakeDamage Để Chuyển Pha ────────────────────────────────────
    public override void TakeDamage(float damage, GameObject attacker = null)
    {
        if (_isDead) return;

        // Gọi logic xử lý nhận sát thương cốt lõi từ lớp cha
        base.TakeDamage(damage, attacker);

        // Kiểm tra tỷ lệ máu qua HealthSystem được thừa kế
        if (healthSystem != null && !_isPhase2 && _bossState != BossCombatState.Transition)
        {
            float hpPercent = (float)healthSystem.CurrentHP / healthSystem.MaxHP; // Đảm bảo lớp HealthSystem của bạn có các thuộc tính này
            if (hpPercent <= 0.5f)
            {
                StartCoroutine(TriggerPhaseTransition());
            }
        }
    }

    private IEnumerator TriggerPhaseTransition()
    {
        _bossState = BossCombatState.Transition;
        _isPhase2 = true;
        StopAgentCompletely();

        // Chạy animation gầm thét đau đớn dữ dội
        anim.SetTrigger("RoarTransition"); // Mixamo: "Zombie Scream" dài dốc sức

        // TẠI ĐÂY: Bạn có thể kích hoạt Particle System khói độc/vfx máu bắn ra che mắt người chơi
        yield return new WaitForSeconds(2.0f); // Thời gian chờ thích hợp với clip anim gầm rú

        // HOÁN ĐỔI MODEL PREFAB
        if (modelV2 != null) modelV2.SetActive(false);
        if (modelV3 != null) modelV3.SetActive(true);

        // Tăng tốc độ cơ bản của Animator lên một chút để thể hiện trạng thái điên cuồng
        anim.speed = 1.25f;

        // Đưa boss về trạng thái chạy Phase 2 bình thường
        _bossState = BossCombatState.P2_Normal;
    }

    // ── Xử Lý Va Chạm Khi Lao Tới (Đâm vào Trụ Bệnh Viện) ────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        // Nếu đang trong trạng thái lao tới đâm húc và đâm trúng vật cản có Tag "Pillar" hoặc cấu trúc tường cứng
        if (_bossState == BossCombatState.P2_Charge && collision.gameObject.CompareTag("Pillar"))
        {
            StartCoroutine(TriggerStunRoutine());
        }
    }

    private IEnumerator TriggerStunRoutine()
    {
        _isStunned = true;
        StopAgentCompletely();
        anim.SetTrigger("StunnedTrigger"); // Mixamo: "Bounced Back" hoặc "Hit Head"

        yield return new WaitForSeconds(2.5f); // Bị choáng 2.5 giây cho người chơi xả đạn

        _isStunned = false;
        ResetToNormalCombatState();
    }

    // ── ANIMATION EVENTS (BẮT BUỘC PHẢI GẮN VÀO CÁC TIMELINE CLIP TRÊN UNITY) ──

    /// <summary>
    /// Gắn vào đúng Frame mà chân boss dậm mạnh xuống đất trên clip "Mutant Stomp"
    /// </summary>
    public void Event_TriggerStompShockwave()
    {
        // Quét bán kính xung quanh xem người chơi có đứng trong vùng ảnh hưởng không
        float shockwaveRadius = 6.0f;
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= shockwaveRadius)
        {
            // Gây sát thương và có thể gọi hàm Player.SlowDown() nếu game bạn có cơ chế này
            player.GetComponent<HealthSystem>()?.TakeDamage(attackDamage * 1.2f, gameObject);
            Debug.Log("Người chơi trúng làn sóng chấn động dậm đất!");
        }
        // Có thể Instantiate một hiệu ứng bụi đất hình tròn tại đây
    }

    /// <summary>
    /// Gắn vào Frame miệng boss há to gầm rú trên clip "Mutant Roar"
    /// </summary>
    public void Event_TriggerSummonMinions()
    {
        if (minionPrefab == null || minionSpawnPoints == null) return;

        foreach (Transform t in minionSpawnPoints)
        {
            Instantiate(minionPrefab, t.position, t.rotation);
        }
        Debug.Log("Boss đã triệu hồi lính lác từ phòng bệnh!");
    }

    /// <summary>
    /// Gắn vào TẤT CẢ các Frame cuối cùng của các clip kỹ năng (Stomp, Summon, Leap, Frenzy)
    /// Để giải phóng Boss về trạng thái di chuyển/săn đuổi thông thường.
    /// </summary>
    public void ResetToNormalCombatState()
    {
        if (_isDead) return;

        _bossState = _isPhase2 ? BossCombatState.P2_Normal : BossCombatState.P1_Normal;

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }
}