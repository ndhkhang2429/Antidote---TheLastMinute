using UnityEngine;

/// <summary>
/// ZombieWardenOne — THE RUSHER
///
/// Combo Phase 1:
/// Attack1 -> Attack2 -> Attack3
///
/// Combo Phase 2:
/// Attack1 -> Attack2 -> Attack5 hoặc Attack3
///
/// Attack4:
/// Đòn punish nếu người chơi đứng gần như yên sau combo.
///
/// Damage:
/// Hoàn toàn sử dụng Animation Event "WardenOnHit".
/// Mỗi Animation Event sẽ gây sát thương một lần.
/// Vì vậy một clip có hai event sẽ gây sát thương hai lần.
/// </summary>
public class ZombieWardenOne : ZombieBase
{
    [Header("Warden I — Rusher Settings")]
    [SerializeField] private float comboStartRange = 2.5f;

    [Header("Enrage")]
    [Range(0f, 1f)]
    [SerializeField] private float enrageThreshold = 0.5f;

    [SerializeField] private float enrageSpeedMultiplier = 1.25f;

    [Range(0f, 1f)]
    [SerializeField] private float enrageFinisherChance = 0.6f;

    [Header("Combo Timing")]
    [SerializeField] private float comboCooldown = 2f;
    [SerializeField] private float recoverDuration = 0.8f;

    [Tooltip("Animation đạt giá trị này thì được xem là hoàn thành.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float exitThreshold = 0.85f;

    [Tooltip("Thời gian tối đa chờ Animator vào state attack.")]
    [SerializeField] private float enterAnimationTimeout = 1f;

    [Header("Punish")]
    [Range(0f, 1f)]
    [SerializeField] private float punishChance = 0.5f;

    [Tooltip("Khoảng di chuyển tối đa để bị xem là đứng yên.")]
    [SerializeField] private float punishMovementThreshold = 1.5f;

    [Header("Damage Multipliers")]
    [SerializeField] private float attack3DamageMultiplier = 1.2f;
    [SerializeField] private float punishDamageMultiplier = 1.5f;
    [SerializeField] private float enrageDamageMultiplier = 1.8f;

    [Header("Attack Range Multipliers")]
    [SerializeField] private float normalHitRangeMultiplier = 1.3f;
    [SerializeField] private float finisherHitRangeMultiplier = 1.6f;

    private enum CombatState
    {
        Approach,
        WaitingEnterAnim,
        WaitingFinishAnim,
        Recover,
        CooldownWait
    }

    private CombatState currentState = CombatState.Approach;

    // Combo
    private int comboStep;
    private bool isEnraged;
    private bool isFinisher;
    private bool isPunish;

    // Animator state hiện đang chờ
    private string waitingStateName = string.Empty;
    private int waitingStateHash;

    // Damage của animation hiện tại
    private float currentDamageMultiplier = 1f;

    // Punish
    private Vector3 playerPositionSnapshot;
    private bool pendingPunishCheck;

    // Timer
    private float stateTimer;
    private float cooldownTimer;

    // FX
    private ZombieBloodFXHandler bloodFX;

    // Animator parameters
    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int Attack1TriggerHash =
        Animator.StringToHash("attack1");

    private static readonly int Attack2TriggerHash =
        Animator.StringToHash("attack2");

    private static readonly int Attack3TriggerHash =
        Animator.StringToHash("attack3");

    private static readonly int Attack4TriggerHash =
        Animator.StringToHash("attack4");

    private static readonly int Attack5TriggerHash =
        Animator.StringToHash("attack5");

    protected override void Start()
    {
        base.Start();

        bloodFX = GetComponent<ZombieBloodFXHandler>();

        if (bloodFX == null)
        {
            bloodFX = GetComponentInChildren<ZombieBloodFXHandler>();
        }
    }

    protected override void OnEnterCombat()
    {
        ResetCombatState();
    }

    protected override void OnExitCombat()
    {
        ResetCombatState();

        if (agent != null &&
            agent.isActiveAndEnabled &&
            agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }

    private void ResetCombatState()
    {
        currentState = CombatState.Approach;

        comboStep = 0;
        isEnraged = false;
        isFinisher = false;
        isPunish = false;

        pendingPunishCheck = false;

        waitingStateName = string.Empty;
        waitingStateHash = 0;

        currentDamageMultiplier = 1f;

        stateTimer = 0f;
        cooldownTimer = 0f;
    }

    protected override void UpdateCombatBehaviour()
    {
        if (player == null ||
            agent == null ||
            anim == null)
        {
            return;
        }

        if (!agent.isActiveAndEnabled ||
            !agent.isOnNavMesh)
        {
            return;
        }

        CheckEnrage();

        float distanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        float currentRunSpeed = isEnraged
            ? runSpeed * enrageSpeedMultiplier
            : runSpeed;

        switch (currentState)
        {
            case CombatState.Approach:
                HandleApproach(distanceToPlayer, currentRunSpeed);
                break;

            case CombatState.WaitingEnterAnim:
                HandleWaitingEnterAnimation();
                break;

            case CombatState.WaitingFinishAnim:
                HandleWaitingFinishAnimation();
                break;

            case CombatState.Recover:
                HandleRecover();
                break;

            case CombatState.CooldownWait:
                HandleCooldownWait(distanceToPlayer, currentRunSpeed);
                break;
        }
    }

    // =========================================================
    // APPROACH
    // =========================================================

    private void HandleApproach(float distanceToPlayer, float speed)
    {
        ResumeAgent(speed);

        agent.stoppingDistance =
            Mathf.Max(0.1f, comboStartRange - 0.3f);

        agent.SetDestination(player.position);

        anim.SetFloat(
            SpeedHash,
            2f,
            0.1f,
            Time.deltaTime
        );

        if (distanceToPlayer <= comboStartRange)
        {
            StartCombo();
        }
    }

    // =========================================================
    // WAITING FOR ANIMATION
    // =========================================================

    private void HandleWaitingEnterAnimation()
    {
        StopAgentCompletely();

        anim.SetFloat(
            SpeedHash,
            0f,
            0.05f,
            Time.deltaTime
        );

        FacePlayer();

        AnimatorStateInfo stateInfo =
            anim.GetCurrentAnimatorStateInfo(0);

        if (IsWaitingAttackState(stateInfo))
        {
            currentState = CombatState.WaitingFinishAnim;
            stateTimer = 0f;
            return;
        }

        stateTimer += Time.deltaTime;

        if (stateTimer < enterAnimationTimeout)
        {
            return;
        }

        Debug.LogWarning(
            $"[Warden I] Không vào được state " +
            $"'{waitingStateName}'. Đang thử trigger lại.",
            gameObject
        );

        ResetAllAttackTriggers();
        SetAttackTrigger(waitingStateName);

        stateTimer = 0f;
    }

    private void HandleWaitingFinishAnimation()
    {
        StopAgentCompletely();

        anim.SetFloat(
            SpeedHash,
            0f,
            0.05f,
            Time.deltaTime
        );

        FacePlayer();

        AnimatorStateInfo stateInfo =
            anim.GetCurrentAnimatorStateInfo(0);

        if (!IsWaitingAttackState(stateInfo))
        {
            return;
        }

        float normalizedTime = stateInfo.normalizedTime;

        // Không gây damage ở đây.
        // Damage chỉ được gây bởi Animation Event WardenOnHit.

        if (normalizedTime >= exitThreshold)
        {
            OnCurrentAnimationFinished();
        }
    }

    private bool IsWaitingAttackState(AnimatorStateInfo stateInfo)
    {
        if (waitingStateHash == 0)
        {
            return false;
        }

        return stateInfo.shortNameHash == waitingStateHash;
    }

    // =========================================================
    // COMBO
    // =========================================================

    private void StartCombo()
    {
        comboStep = 1;

        isFinisher = false;
        isPunish = false;

        currentDamageMultiplier = 1f;

        TriggerAttack("Attack1");
    }

    private void OnCurrentAnimationFinished()
    {
        if (isPunish)
        {
            FinishPunish();
            return;
        }

        switch (comboStep)
        {
            case 1:
                StartSecondAttack();
                break;

            case 2:
                StartFinalAttack();
                break;

            default:
                StartRecovery();
                break;
        }
    }

    private void StartSecondAttack()
    {
        comboStep = 2;

        isFinisher = false;
        currentDamageMultiplier = 1f;

        TriggerAttack("Attack2");
    }

    private void StartFinalAttack()
    {
        comboStep = 3;

        playerPositionSnapshot = player.position;
        pendingPunishCheck = true;

        bool useEnrageFinisher =
            isEnraged &&
            Random.value < enrageFinisherChance;

        if (useEnrageFinisher)
        {
            isFinisher = true;

            currentDamageMultiplier =
                enrageDamageMultiplier;

            TriggerAttack("Attack5");
        }
        else
        {
            isFinisher = false;

            currentDamageMultiplier =
                attack3DamageMultiplier;

            TriggerAttack("Attack3");
        }
    }

    private void StartRecovery()
    {
        currentState = CombatState.Recover;
        stateTimer = 0f;
    }

    // =========================================================
    // RECOVER AND PUNISH
    // =========================================================

    private void HandleRecover()
    {
        StopAgentCompletely();

        anim.SetFloat(
            SpeedHash,
            0f,
            0.2f,
            Time.deltaTime
        );

        FacePlayer();

        stateTimer += Time.deltaTime;

        if (stateTimer < recoverDuration)
        {
            return;
        }

        if (ShouldPerformPunish())
        {
            pendingPunishCheck = false;
            DoPunish();
            return;
        }

        pendingPunishCheck = false;
        EnterCooldown();
    }

    private bool ShouldPerformPunish()
    {
        if (!pendingPunishCheck || player == null)
        {
            return false;
        }

        if (Random.value >= punishChance)
        {
            return false;
        }

        float playerMovedDistance =
            Vector3.Distance(
                player.position,
                playerPositionSnapshot
            );

        return playerMovedDistance <
               punishMovementThreshold;
    }

    private void DoPunish()
    {
        isPunish = true;
        isFinisher = false;

        currentDamageMultiplier =
            punishDamageMultiplier;

        TriggerAttack("Attack4");
    }

    private void FinishPunish()
    {
        isPunish = false;
        isFinisher = false;

        comboStep = 0;
        currentDamageMultiplier = 1f;

        EnterCooldown();
    }

    // =========================================================
    // COOLDOWN
    // =========================================================

    private void EnterCooldown()
    {
        currentState = CombatState.CooldownWait;

        cooldownTimer = 0f;
        comboStep = 0;

        isPunish = false;
        isFinisher = false;

        currentDamageMultiplier = 1f;
    }

    private void HandleCooldownWait(
        float distanceToPlayer,
        float currentRunSpeed)
    {
        cooldownTimer += Time.deltaTime;

        float approachSpeed =
            isEnraged
                ? currentRunSpeed
                : walkSpeed;

        ResumeAgent(approachSpeed);

        agent.stoppingDistance =
            Mathf.Max(0.1f, comboStartRange - 0.3f);

        agent.SetDestination(player.position);

        anim.SetFloat(
            SpeedHash,
            isEnraged ? 2f : 1f,
            0.15f,
            Time.deltaTime
        );

        float actualCooldown =
            isEnraged
                ? comboCooldown * 0.7f
                : comboCooldown;

        if (cooldownTimer >= actualCooldown)
        {
            currentState = CombatState.Approach;
        }
    }

    // =========================================================
    // ANIMATOR TRIGGERS
    // =========================================================

    private void TriggerAttack(string stateName)
    {
        waitingStateName = stateName;
        waitingStateHash =
            Animator.StringToHash(stateName);

        stateTimer = 0f;
        currentState = CombatState.WaitingEnterAnim;

        StopAgentCompletely();

        ResetAllAttackTriggers();
        SetAttackTrigger(stateName);
    }

    private void SetAttackTrigger(string stateName)
    {
        switch (stateName)
        {
            case "Attack1":
                anim.SetTrigger(Attack1TriggerHash);
                break;

            case "Attack2":
                anim.SetTrigger(Attack2TriggerHash);
                break;

            case "Attack3":
                anim.SetTrigger(Attack3TriggerHash);
                break;

            case "Attack4":
                anim.SetTrigger(Attack4TriggerHash);
                break;

            case "Attack5":
                anim.SetTrigger(Attack5TriggerHash);
                break;

            default:
                Debug.LogError(
                    $"[Warden I] Không tồn tại attack state: " +
                    $"{stateName}",
                    gameObject
                );
                break;
        }
    }

    private void ResetAllAttackTriggers()
    {
        anim.ResetTrigger(Attack1TriggerHash);
        anim.ResetTrigger(Attack2TriggerHash);
        anim.ResetTrigger(Attack3TriggerHash);
        anim.ResetTrigger(Attack4TriggerHash);
        anim.ResetTrigger(Attack5TriggerHash);
    }

    // =========================================================
    // ANIMATION EVENTS
    // =========================================================

    /// <summary>
    /// Hàm này được gọi bởi Animation Event.
    ///
    /// Một event = một lần damage.
    /// Hai event trong một clip = hai lần damage.
    ///
    /// Tên Function trong Animation Event phải chính xác:
    /// WardenOnHit
    /// </summary>
    public void WardenOnHit()
    {
        if (player == null)
        {
            return;
        }

        // Chỉ nhận event trong lúc đang thực hiện attack.
        if (currentState != CombatState.WaitingEnterAnim &&
            currentState != CombatState.WaitingFinishAnim)
        {
            return;
        }

        AnimatorStateInfo stateInfo =
            anim.GetCurrentAnimatorStateInfo(0);

        // Ngăn event cũ từ một state khác gây damage.
        if (!IsWaitingAttackState(stateInfo))
        {
            return;
        }

        DealHitDamage();
    }

    private void DealHitDamage()
    {
        if (player == null)
        {
            return;
        }

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        float rangeMultiplier =
            isFinisher
                ? finisherHitRangeMultiplier
                : normalHitRangeMultiplier;

        float currentHitRange =
            attackRange * rangeMultiplier;

        if (distanceToPlayer > currentHitRange)
        {
            return;
        }

        HealthSystem playerHealth =
            player.GetComponent<HealthSystem>();

        if (playerHealth == null)
        {
            playerHealth =
                player.GetComponentInParent<HealthSystem>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning(
                "[Warden I] Player không có HealthSystem.",
                player.gameObject
            );

            return;
        }

        float damage =
            attackDamage * currentDamageMultiplier;

        playerHealth.TakeDamage(
            damage,
            gameObject
        );

        SpawnPlayerBloodEffect();

        Debug.Log(
            $"[Warden I] {waitingStateName} gây " +
            $"{damage} damage.",
            gameObject
        );
    }

    private void SpawnPlayerBloodEffect()
    {
        if (bloodFX == null || player == null)
        {
            return;
        }

        Vector3 hitPoint =
            player.position + Vector3.up;

        Vector3 hitNormal =
            player.position - transform.position;

        hitNormal.y = 0f;

        if (hitNormal.sqrMagnitude > 0.001f)
        {
            hitNormal.Normalize();
        }
        else
        {
            hitNormal = transform.forward;
        }

        bloodFX.OnHitMelee(
            hitPoint,
            hitNormal
        );
    }

    /// <summary>
    /// ZombieBase yêu cầu override hàm này.
    /// Warden sử dụng WardenOnHit từ Animation Event.
    /// </summary>
    public override void DealDamageToPlayer()
    {
        // Không xử lý damage ở đây.
    }

    // =========================================================
    // ENRAGE
    // =========================================================

    private void CheckEnrage()
    {
        if (isEnraged || healthSystem == null)
        {
            return;
        }

        if (healthSystem.MaxHP <= 0f)
        {
            return;
        }

        float healthRatio =
            healthSystem.CurrentHP /
            healthSystem.MaxHP;

        if (healthRatio > enrageThreshold)
        {
            return;
        }

        isEnraged = true;

        Debug.Log(
            "[Warden I] ENRAGE — Phase 2!",
            gameObject
        );
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(
            transform.position,
            comboStartRange
        );

        Gizmos.color = Color.red;

        float previewAttackRange =
            attackRange *
            normalHitRangeMultiplier;

        Gizmos.DrawWireSphere(
            transform.position,
            previewAttackRange
        );
    }
}