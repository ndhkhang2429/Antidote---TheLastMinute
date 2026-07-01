using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BulletProjectile : MonoBehaviour
{
    private float _bulletDamage;

    [Header("Impact Effects")]
    [Tooltip("Kéo Prefab Vết đạn hoặc Tia lửa vào đây")]
    public GameObject wallHitPrefab;

    public void SetupBullet(float damageAmount)
    {
        _bulletDamage = damageAmount;
        Destroy(gameObject, 3f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Bullet")) return;

        ContactPoint contact = collision.contacts[0];
        Quaternion hitRotation = Quaternion.LookRotation(-contact.normal);

        // Kiểm tra nếu trúng Zombie (đối tượng có HealthSystem)
        // Kiểm tra nếu trúng Zombie (đối tượng có HealthSystem)
        if (collision.gameObject.TryGetComponent<HealthSystem>(out HealthSystem targetHealth))
        {
            targetHealth.TakeDamage(_bulletDamage);

            ZombieBloodFXHandler bloodFX = collision.gameObject.GetComponentInParent<ZombieBloodFXHandler>();

            if (bloodFX != null)
            {
                // Gọi thẳng hàm mới tạo, truyền vào tọa độ, hướng, và Transform của bộ phận bị trúng đạn
                // Dùng contact.otherCollider.transform để lấy chính xác khúc xương bị bắn trúng
                bloodFX.OnHitProjectile(contact.point, contact.normal, contact.otherCollider.transform);
            }
        }
        else // Trúng tường, môi trường
        {
            if (wallHitPrefab != null)
            {
                Vector3 spawnPosition = contact.point + contact.normal * 0.05f;
                GameObject hole = Instantiate(wallHitPrefab, spawnPosition, hitRotation);
                hole.transform.SetParent(collision.transform);
                Destroy(hole, 10f);
            }
        }

        Destroy(gameObject);
    }
}