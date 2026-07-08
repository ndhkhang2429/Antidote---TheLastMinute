using UnityEngine;

public class FusePanelManager : MonoBehaviour
{
    [Header("References")]
    public FuseSlot[] allSlots;
    public SwitchSlider[] allSwitches;
    public bool[] correctSwitchStates;

    [Header("Indicators - Renderers")]
    public Renderer powerOKLight;   // Đèn Xanh
    public Renderer lineBreakLight; // Đèn Đỏ

    [Header("Indicators - Materials Đèn Xanh")]
    public Material matGreenOn;
    public Material matGreenOff;

    [Header("Indicators - Materials Đèn Đỏ")]
    public Material matRedOn;
    public Material matRedOff;

    public bool IsPanelReady { get; private set; } = false;

    public bool HasFuseInHand => HasAnyFuseInInventory();

    void Start() => UpdatePanelState();

    bool HasAnyFuseInInventory()
    {
        if (InventorySystem.Instance == null) return false;
        foreach (var slot in InventorySystem.Instance.GetItemSlots())
            if (!slot.IsEmpty && slot.item is FuseItemDataSO) return true;
        return false;
    }

    public bool TryInsertHeldFuse(FuseSlot slot)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return false;

        var heldItem = inv.GetHeldItem();

        bool isHoldingCorrectFuse = heldItem != null
                                 && heldItem is FuseItemDataSO fuse
                                 && fuse.fuseID == slot.correctFuseID;

        if (!isHoldingCorrectFuse)
        {
            if (NotificationUI.Instance != null)
            {
                NotificationUI.Instance.ShowNotification($"Cần cầm cầu chì loại [{slot.correctFuseID}] trên tay!");
            }
            return false;
        }

        bool success = slot.TryInsertFuse(slot.correctFuseID);
        if (success)
        {
            inv.RemoveItem(heldItem, 1);
            UpdatePanelState();
        }
        return success;
    }

    public void UpdatePanelState()
    {
        bool fusesOK = CheckAllFuses();
        bool switchesOK = CheckAllSwitches();
        IsPanelReady = fusesOK && switchesOK;
        UpdateIndicatorLights();
    }

    public bool CheckAllFuses()
    {
        foreach (var slot in allSlots)
            if (slot.requiresFuse && !slot.IsCorrect) return false;
        return true;
    }

    public bool CheckAllSwitches()
    {
        if (correctSwitchStates == null || correctSwitchStates.Length == 0) return true;
        for (int i = 0; i < allSwitches.Length; i++)
        {
            if (i >= correctSwitchStates.Length) break;
            if (allSwitches[i].isOn != correctSwitchStates[i]) return false;
        }
        return true;
    }

    void UpdateIndicatorLights()
    {
        // Xử lý Đèn Xanh (Power OK): Sẵn sàng thì ON, chưa thì OFF
        if (powerOKLight != null)
            powerOKLight.material = IsPanelReady ? matGreenOn : matGreenOff;

        // Xử lý Đèn Đỏ (Line Break): Sẵn sàng thì OFF, chưa thì ON
        if (lineBreakLight != null)
            lineBreakLight.material = IsPanelReady ? matRedOff : matRedOn;
    }
}