// Không phải MonoBehaviour nhé
public interface IEquippable
{
    void OnEquip();   // Tự động gọi khi sinh ra trên tay
    void OnUnequip(); // Tự động gọi trước khi cất đi/xóa đi
}