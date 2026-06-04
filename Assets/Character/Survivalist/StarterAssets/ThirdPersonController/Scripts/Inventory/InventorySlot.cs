using System;

[Serializable]
public class InventorySlot 
{
    public ItemDataSO item;
    public int quantity;        

    public bool IsEmpty => item == null;
    public bool IsFull => item != null && quantity >= item.maxStack;

    public int Add(int amount)
    {
        if (item == null) return amount; // không thể thêm vào ô rỗng không có item type
        int canAdd = item.maxStack - quantity;
        int actualAdd = Math.Min(canAdd, amount);
        quantity += actualAdd;
        return amount - actualAdd; // trả về phần dư
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }

    public void Set(ItemDataSO newItem, int qty)
    {
        item = newItem;
        quantity = qty;
    }
}