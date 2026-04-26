using UnityEngine;

// Kế thừa từ ZombieBase thay vì MonoBehaviour
public class ZombieNormal : ZombieBase
{
    // override: Ghi đè hàm Start của Base để thiết lập chỉ số riêng cho con này
    protected override void Start()
    {
        // Vẫn gọi base.Start() để nó lấy NavMesh, Animator và tìm Player
        base.Start();

        // Thiết lập chỉ số riêng cho con Zombie thường
        maxHealth = 100f;
        currentHealth = maxHealth;
        attackDamage = 15f;

        // Tốc độ di chuyển chậm lờ đờ
        agent.speed = 2.5f;
    }

    // Con này đánh bình thường, không có skill gì đặc biệt nên không cần ghi đè các hàm AttackLogic hay Die
}