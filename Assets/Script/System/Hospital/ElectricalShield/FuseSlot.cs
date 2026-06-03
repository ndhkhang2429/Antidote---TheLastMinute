using UnityEngine;

/// <summary>
/// Gắn vào từng Switch001-014 trên panel.
/// Click chuột để gắn fuse vào (chỉ khi đang trong panel mode).
/// </summary>
public class FuseSlot : MonoBehaviour
{
    [Header("Slot Config")]
    public int slotIndex = 0;
    public bool requiresFuse = false;
    public string correctFuseID = "FUSE_01";

    [Header("Visual")]
    [Tooltip("Kéo Knob tương ứng vào đây để ẩn/hiện")]
    public GameObject fuseVisual;

    [Header("References")]
    [Tooltip("Kéo PanelInteractZone vào để check panel mode")]
    public PanelInteractZone panelZone;
    [Tooltip("Kéo FusePanelManager vào")]
    public FusePanelManager fusePanelManager;

    // ── State ──────────────────────────────────────────────
    public bool HasFuse { get; private set; } = false;
    public bool IsCorrect => HasFuse;

    void Start()
    {
        if (!requiresFuse)
        {
            HasFuse = true;
            SetVisual(true);
        }
        else
        {
            HasFuse = false;
            SetVisual(false);
        }
    }

    // Click chuột vào slot khi đang trong panel mode
    void OnMouseDown()
    {
        if (panelZone == null || !panelZone.IsInPanelMode) return;
        if (!requiresFuse || HasFuse) return;

        // Thử gắn fuse đang cầm
        if (fusePanelManager != null && fusePanelManager.HasFuseInHand)
        {
            bool success = fusePanelManager.TryInsertHeldFuse(this);

            if (success)
                InteractionUIManager.Instance?.ShowPrompt("✓ Gắn cầu chì thành công!");
            else
                InteractionUIManager.Instance?.ShowPrompt("✗ Sai cầu chì cho slot này!");
        }
        else
        {
            InteractionUIManager.Instance?.ShowPrompt("Không có cầu chì trong tay!");
        }
    }

    // Gọi từ FusePanelManager
    public bool TryInsertFuse(string fuseID)
    {
        if (!requiresFuse || HasFuse) return false;

        if (fuseID == correctFuseID)
        {
            HasFuse = true;
            SetVisual(true);
            Debug.Log($"[FuseSlot {slotIndex}] Gắn đúng fuse {fuseID}!");
            return true;
        }

        Debug.Log($"[FuseSlot {slotIndex}] Sai fuse! Cần {correctFuseID}, nhận {fuseID}.");
        return false;
    }

    void SetVisual(bool show)
    {
        if (fuseVisual != null)
            fuseVisual.SetActive(show);
    }

    void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.1f,
            $"Slot {slotIndex}\n{(requiresFuse ? $"Cần: {correctFuseID}" : "Sẵn có")}");
    }
}