using UnityEngine;

public class FuseSlot : MonoBehaviour
{
    [Header("Slot Config")]
    public int slotIndex = 0;
    public bool requiresFuse = false;
    public string correctFuseID = "FUSE_01";

    [Header("Fuse Item SO")]
    public FuseItemDataSO requiredFuseSO;

    [Header("Visual")]
    public GameObject fuseVisual;

    [Header("References")]
    public PanelInteractZone panelZone;
    public FusePanelManager fusePanelManager;

    public bool HasFuse { get; private set; } = false;
    public bool IsCorrect => HasFuse;

    void Start()
    {
        if (!requiresFuse) { HasFuse = true; SetVisual(true); }
        else { HasFuse = false; SetVisual(false); }
    }

    public void InteractWithSlot()
    {
        if (panelZone == null || !panelZone.IsInPanelMode) return;
        if (!requiresFuse || HasFuse) return;

        var inv = InventorySystem.Instance;
        if (inv == null) return;

        // BẮT BUỘC phải đang cầm đúng fuse ở slot 5
        if (!inv.IsHoldingFuse(correctFuseID))
        {
            if (HasFuseInInventory(correctFuseID))
                InteractionUIManager.Instance?.ShowPrompt(
                    $"Nhấn [5] để cầm {correctFuseID} trước!");
            else
                InteractionUIManager.Instance?.ShowPrompt(
                    $"Cần {correctFuseID} trong inventory!");
            return;
        }

        // Đang cầm đúng → gắn vào
        if (fusePanelManager != null)
        {
            bool success = fusePanelManager.TryInsertHeldFuse(this);
            if (success)
            {
                InteractionUIManager.Instance?.ShowPrompt(
                    $"Gắn {correctFuseID} thành công!");
                inv.DeselectAll();
                if (requiredFuseSO != null && !inv.HasItem(requiredFuseSO))
                    inv.ClearItemSlot();
            }
            else
            {
                InteractionUIManager.Instance?.ShowPrompt("✗ Gắn thất bại!");
            }
        }
    }

    public bool TryInsertFuse(string fuseID)
    {
        if (!requiresFuse || HasFuse) return false;
        if (fuseID != correctFuseID) return false;

        HasFuse = true;
        SetVisual(true);
        Debug.Log($"[FuseSlot {slotIndex}] Gắn {fuseID} thành công!");
        return true;
    }

    void SetVisual(bool show)
    {
        if (fuseVisual != null) fuseVisual.SetActive(show);
    }

    bool HasFuseInInventory(string id)
    {
        if (InventorySystem.Instance == null) return false;
        foreach (var slot in InventorySystem.Instance.GetItemSlots())
        {
            if (slot.IsEmpty) continue;
            if (slot.item is FuseItemDataSO f && f.fuseID == id)
                return true;
        }
        return false;
    }

    // Bọc trong #if để tránh lỗi khi build
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.1f,
            $"Slot {slotIndex}\n{(requiresFuse ? $"Cần: {correctFuseID}" : "Sẵn có")}");
    }
#endif
}