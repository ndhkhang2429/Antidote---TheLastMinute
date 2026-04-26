using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ZombieBase : MonoBehaviour
{
    [Header("Components")]
    protected NavMeshAgent agent;
    protected Animator anim;
    protected Transform player;

    [Header("Base Stats")]
    public float maxHealth = 100f;
    protected float currentHealth;
    public float attackDamage = 15f;

    [Header("AI Settings")]
    public float walkSpeed = 1f;
    public float runSpeed = 3.5f;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public Transform[] waypoints;
    public float attackCooldown = 1.5f;
    protected float nextAttackTime = 0f;
    protected int currentWaypointIndex = 0;

    // Máy trạng thái bảo vệ (protected) để class con có thể đọc được
    protected enum ZombieState { Patrol, Scream, Chase, Attack, Dead }
    protected ZombieState currentState = ZombieState.Patrol;

    protected bool isScreaming = false;

    // virtual: Cho phép class con (Boss/Elite) ghi đè (thay đổi) nội dung hàm này nếu cần
    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        GoToNextWaypoint();
    }

    protected virtual void Update()
    {
        // Nếu đã chết thì ngừng suy nghĩ
        if (currentState == ZombieState.Dead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case ZombieState.Patrol:
                PatrolLogic(distanceToPlayer);
                break;
            case ZombieState.Scream:
                ScreamLogic();
                break;
            case ZombieState.Chase:
                ChaseLogic(distanceToPlayer);
                break;
            case ZombieState.Attack:
                AttackLogic(distanceToPlayer);
                break;
        }
    }

    protected virtual void PatrolLogic(float dist)
    {
        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0f;
        anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);

        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            GoToNextWaypoint();
        }

        if (dist < detectionRange)
        {
            currentState = ZombieState.Scream;
        }
    }

    protected virtual void ScreamLogic()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Tránh lỗi zombie bị ngửa mặt lên trời nếu bạn đứng trên cao
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        if (!isScreaming)
        {
            isScreaming = true;
            agent.isStopped = true;
            anim.SetFloat("Speed", 0f);
            anim.SetTrigger("Scream");

            // Chờ 2 giây (thời gian hét) rồi chuyển sang rượt
            Invoke("StartChasing", 2f);
        }
    }

    protected virtual void StartChasing()
    {
        currentState = ZombieState.Chase;
    }

    protected virtual void ChaseLogic(float dist)
    {
        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.stoppingDistance = attackRange;

        agent.SetDestination(player.position);
        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
        
        if (dist <= attackRange)
        {
            currentState = ZombieState.Attack;
        }
        else if (dist > detectionRange * 1.5f) // Mất dấu
        {
            isScreaming = false;
            currentState = ZombieState.Patrol;
            GoToNextWaypoint();
        }
    }

    protected virtual void AttackLogic(float dist)
    {
        agent.isStopped = true;
        anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);

        // Xoay mặt về phía người chơi
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);

        if (Time.time >= nextAttackTime)
        {
            anim.SetTrigger("Attack");
            nextAttackTime = Time.time + attackCooldown; // Hẹn giờ cho cú đánh tiếp theo
        }

        if (dist > attackRange)
        {
            currentState = ZombieState.Chase;
        }
    }

    protected void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        agent.destination = waypoints[currentWaypointIndex].position;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    // --- HỆ THỐNG CHIẾN ĐẤU ---

    // Hàm nhận sát thương chung cho mọi Zombie
    public virtual void TakeDamage(float damage)
    {
        if (currentState == ZombieState.Dead) return;

        currentHealth -= damage;

        // Bị bắn trúng thì tự động nhận diện và rượt luôn, bỏ qua tiếng hét
        if (currentState == ZombieState.Patrol)
        {
            currentState = ZombieState.Chase;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        currentState = ZombieState.Dead;
        agent.isStopped = true;
        anim.SetTrigger("IsDead"); // Đảm bảo bạn có Parameter Trigger "IsDead" trong Animator

        // Tắt va chạm để người chơi không đi xuyên qua hoặc bắn trúng xác chết
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        agent.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}