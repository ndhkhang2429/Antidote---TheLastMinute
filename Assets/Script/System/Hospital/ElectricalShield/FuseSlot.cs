using UnityEngine;

public class FuseSlot : MonoBehaviour
{
    [Header("Slot Config")]
    public int slotIndex = 0;
    public bool requiresFuse = false;
    public string correctFuseID = "FUSE_01";

    [Header("Fuse Item SO — kéo FuseItemDataSO tương ứng vào")]
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

    void OnMouseDown()
    {
        if (panelZone == null || !panelZone.IsInPanelMode) return;
        if (!requiresFuse || HasFuse) return;

        if (fusePanelManager != null)
        {
            bool success = fusePanelManager.TryInsertHeldFuse(this);
            InteractionUIManager.Instance?.ShowPrompt(
                success ? $"✓ Gắn {correctFuseID} thành công!"
                        : $"✗ Không có {correctFuseID} trong inventory!");
        }
    }

    public bool TryInsertFuse(string fuseID)
    {
        if (!requiresFuse || HasFuse) return false;
        if (fuseID != correctFuseID) return false;

        HasFuse = true;
        SetVisual(true);
        Debug.Log($"[FuseSlot {slotIndex}] Gắn đúng {fuseID}!");
        return true;
    }

    void SetVisual(bool show)
    {
        if (fuseVisual != null) fuseVisual.SetActive(show);
    }

    void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.1f,
            $"Slot {slotIndex}\n{(requiresFuse ? $"Cần: {correctFuseID}" : "Sẵn có")}");
    }
}