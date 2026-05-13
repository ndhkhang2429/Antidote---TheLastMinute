using UnityEngine;

// Kế thừa từ ZombieBase thay vì MonoBehaviour
public class ZombieNormal : ZombieBase
{
    // override: Ghi đè hàm Start của Base để thiết lập chỉ số riêng cho con này
    protected override void Start()
    {
        // Vẫn gọi base.Start() để nó lấy NavMesh, Animator và tìm Player
        base.Start();

        attackDamage = 15f;
        attackCooldown = 1.5f;
        walkSpeed = 1f;
        runSpeed = 2.5f;
        detectionRange = 10f;
        attackRange = 2f;
    }
}