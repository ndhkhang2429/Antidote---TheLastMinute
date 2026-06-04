using UnityEngine;

public static class ItemUser
{
    public static void Use(ItemDataSO item, GameObject user)
    {
        switch (item.category)
        {
            case ItemCategory.Consumable:
                UseConsumable(item, user);
                break;
            case ItemCategory.Grenade:
                ThrowGrenade(item, user);
                break;
        }
    }

    static void UseConsumable(ItemDataSO item, GameObject user)
    {
        var health = user.GetComponent<HealthSystem>();
        if (health == null) return;
        // dùng itemName hoặc thêm field healAmount vào ItemDataSO
        // ví dụ đơn giản:
        if (item.itemName.Contains("Băng")) health.Heal(30);
        else if (item.itemName.Contains("Hộp y tế")) health.Heal(100);
        Debug.Log($"Dùng {item.itemName}");
    }

    static void ThrowGrenade(ItemDataSO item, GameObject user)
    {
        // Spawn prefab lựu đạn về phía camera nhìn
        var cam = Camera.main;
        if (cam == null || item.worldPrefab == null) return;
        var grenade = Object.Instantiate(item.worldPrefab,
            cam.transform.position + cam.transform.forward,
            cam.transform.rotation);
        if (grenade.TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce(cam.transform.forward * 15f, ForceMode.Impulse);
    }
}