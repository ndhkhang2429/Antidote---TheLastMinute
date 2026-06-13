using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Header("Item Info")]
    public ItemDataSO itemData;
    public int quantity = 1;

    // Đã xóa hoàn toàn Start, Update, pickupRadius và pickupKey
    // Vì mọi thao tác tương tác, bắt phím F và khoảng cách đều đã được PlayerInteraction kiểm soát qua Raycast!
}