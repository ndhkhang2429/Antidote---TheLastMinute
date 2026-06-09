public interface IQuestRequirement
{
    // Cần item gì?
    ItemDataSO GetRequiredItem();

    // Chuỗi chữ hiện lên khi Raycast chĩa vào (Ví dụ: "[F] Mở tủ điện")
    string GetPrompt();

    // Đã hoàn thành/Mở khóa chưa?
    bool IsCompleted();

    // Hàm xử lý tương tác khi bấm F
    bool TryUseItem(InventorySystem inv);
}