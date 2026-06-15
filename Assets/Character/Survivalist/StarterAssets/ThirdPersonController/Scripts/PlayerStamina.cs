using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Thông số Năng lượng")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float drainRate = 15f; // Tốc độ tụt khi chạy
    public float regenRate = 10f; // Tốc độ hồi khi đi bộ hoặc đứng yên

    // Trạng thái kiệt sức: Ngăn việc người chơi spam nút chạy khi thể lực vừa nhích lên 1%
    public bool isExhausted { get; private set; }
    public bool CanRun => !isExhausted && currentStamina > 0;

    private void Start()
    {
        currentStamina = maxStamina;
        isExhausted = false;
    }

    // Hàm này sẽ được ThirdPersonController gọi mỗi frame
    public void HandleStamina(bool isRunning)
    {
        if (isRunning && !isExhausted)
        {
            // Tụt thể lực
            currentStamina -= drainRate * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true; // Đánh dấu kiệt sức
            }
        }
        else
        {
            // Hồi thể lực
            if (currentStamina < maxStamina)
            {
                currentStamina += regenRate * Time.deltaTime;
            }

            // Thoát khỏi trạng thái kiệt sức khi hồi được 20%
            if (isExhausted && currentStamina >= maxStamina * 0.2f)
            {
                isExhausted = false;
            }
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    // Hàm mới để nhận lượng hồi phục từ ConsumableDataSO
    public void RestoreStamina(float amount)
    {
        currentStamina += amount;

        // Thoát khỏi trạng thái kiệt sức nếu đã hồi đủ 20%
        if (isExhausted && currentStamina >= maxStamina * 0.2f)
        {
            isExhausted = false;
        }

        // Chặn không cho vượt giới hạn
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    // Gọi hàm này từ Inventory khi dùng vật phẩm Nước
    public void DrinkWater()
    {
        currentStamina = maxStamina;
        isExhausted = false;
        Debug.Log("Đã uống nước! Thể lực đầy 100%.");
    }
}