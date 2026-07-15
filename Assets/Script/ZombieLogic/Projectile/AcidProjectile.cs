using UnityEngine;

public class AcidProjectile : MonoBehaviour
{
    private float speed;
    private float damage;
    private Vector3 direction;

    [Header("Effects")]
    public GameObject impactEffectPrefab; // Hiệu ứng vỡ/nổ khi chạm

    [Header("Sound")]
    [SerializeField] private AudioClip[] impactSfx;
    [SerializeField][Range(0f, 1f)] private float impactVolume = 0.8f;

    // Hàm để SpitterZombie gọi khi khởi tạo viên đạn
    public void Setup(Vector3 moveDirection, float attackDamage, float projectileSpeed)
    {
        direction = moveDirection.normalized;
        damage = attackDamage;
        speed = projectileSpeed;

        // Tự hủy sau 5 giây để dọn dẹp bộ nhớ nếu bay trượt ra ngoài map
        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        // Di chuyển viên axit
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem có trúng Player không
        if (other.CompareTag("Player"))
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, null);
            }
        }

        // Tạo hiệu ứng vỡ (nếu có)
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        // Phát tiếng va chạm (dùng PlayClipAtPoint vì object sắp bị Destroy ngay)
        if (impactSfx != null && impactSfx.Length > 0)
        {
            AudioClip clip = impactSfx[Random.Range(0, impactSfx.Length)];
            AudioSource.PlayClipAtPoint(clip, transform.position, impactVolume);
        }

        // Hủy viên axit ngay lập tức khi chạm bất cứ thứ gì (Player, Tường, Đất...)
        Destroy(gameObject);
    }
}