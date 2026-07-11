using UnityEngine;
using NavKeypad;

/// <summary>
/// Gắn script này lên CÙNG GameObject với PanelInteractZone (khu vực bàn phím).
/// Tự động bật KeypadInteractionFPV khi vào Panel Mode, tắt khi thoát ra,
/// để chuột không vô tình bấm trúng nút số lúc đang chơi bình thường.
/// </summary>
public class KeypadPanelBinder : MonoBehaviour
{
    [SerializeField] private PanelInteractZone panelZone;
    [SerializeField] private KeypadInteractionFPV keypadInteraction;

    private void OnEnable()
    {
        if (panelZone == null) return;
        panelZone.OnEnterPanelMode += HandleEnter;
        panelZone.OnExitPanelMode += HandleExit;
    }

    private void OnDisable()
    {
        if (panelZone == null) return;
        panelZone.OnEnterPanelMode -= HandleEnter;
        panelZone.OnExitPanelMode -= HandleExit;
    }

    private void HandleEnter()
    {
        if (keypadInteraction != null)
            keypadInteraction.enabled = true;
    }

    private void HandleExit()
    {
        if (keypadInteraction != null)
            keypadInteraction.enabled = false;
    }
}