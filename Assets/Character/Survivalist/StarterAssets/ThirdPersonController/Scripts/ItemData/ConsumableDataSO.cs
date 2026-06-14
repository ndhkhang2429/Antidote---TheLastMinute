using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable Data")]
public class ConsumableDataSO : ItemDataSO // Kế thừa từ class gốc của bạn
{
    [Header("Consumable Effects")]
    public float healthRestore = 50f;  // Lượng máu hồi phục
    public float thirstRestore = 20f;  // Lượng nước hồi phục (nếu game có cơ chế khát)

    [Header("Action Settings")]
    [Tooltip("Thời gian cần thiết để sử dụng vật phẩm này (giây)")]
    public float useTime = 3f;         // <--- BIẾN MỚI THÊM VÀO ĐÂY

    public AudioClip consumeSound;
}