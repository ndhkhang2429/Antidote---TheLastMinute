using UnityEngine;
using System.Collections; // BẮT BUỘC để dùng IEnumerator

public interface IUsable
{
    void Use(GameObject player);
}

[CreateAssetMenu(menuName = "Inventory/Consumable/MedKit")]
public class MedKitData : ItemData, IUsable
{
    public int healAmount;
    public float useTime;

    public void Use(GameObject player)
    {
        var health = player.GetComponent<HealthSystem>();
        if (health != null)
            health.StartCoroutine(HealCoroutine(health));
    }

    // SỬA: Đổi PlayerController thành HealthSystem cho khớp với tham số được truyền vào
    private IEnumerator HealCoroutine(HealthSystem health)
    {
        yield return new WaitForSeconds(useTime);
        // Giả sử HealthSystem của bạn có hàm Restore hoặc Heal
    }
}