public interface IInteractable
{
    /// <summary>
    /// Trả về prompt hiện trên màn hình khi player nhìn vào.
    /// Trả về null nếu không thể tương tác.
    /// </summary>
    string GetPrompt();

    /// <summary>
    /// Thực hiện tương tác khi player nhấn F.
    /// Trả về true nếu thành công.
    /// </summary>
    bool TryInteract(InventorySystem inv);
}