using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIController : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public Transform[] waypoints;

    public float detectionRange = 10f; // Khoảng cách phát hiện người chơi
    private int currentWaypointIndex = 0;
    private bool isChasing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Tự động tìm Player theo Tag
        player = GameObject.FindGameObjectWithTag("Player").transform;

        GoToNextWaypoint();
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < detectionRange)
        {
            isChasing = true;
            agent.SetDestination(player.position); // Đuổi theo Player
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
                GoToNextWaypoint();
            }
            Patrol();
        }
    }

    void Patrol()
    {
        // Nếu đã đến gần điểm tuần tra hiện tại, chuyển sang điểm tiếp theo
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        agent.destination = waypoints[currentWaypointIndex].position;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    // Vẽ vòng tròn để bạn dễ quan sát tầm nhìn của AI trong Scene
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
