using UnityEngine;

[CreateAssetMenu(fileName = "NewBulletData", menuName = "Weapons/Bullet Data")]
public class BulletData : ScriptableObject
{
    [Header("Basic Stats")]
    public float speed = 20f;
    public int damage = 10;
    public float lifeTime = 3f;

    [Header("Gun Stats")]
    public float fireRate = 0.2f;
    public int magSize = 30;
    public float reloadTime = 1.5f;

    [Header("Player Cost")]
    public float manaCost = 10f;          // Lượng mana tốn khi bắn viên này
    [Range(0f, 1f)]
    public float missHealthPenaltyPercent = 0.05f; // Bị trừ bao nhiêu % máu nếu hụt (0.05 = 5%)
}