using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn vào từng Switch001-014.
/// Chỉ cho click khi đang trong panel mode.
/// Click → toggle ON/OFF + animate trượt.
/// </summary>
public class SwitchSlider : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Trạng thái mặc định ban đầu")]
    public bool isOn = false;

    [Tooltip("Vị trí local của fuse khi OFF")]
    public Vector3 offPosition = new Vector3(-0.03f, 0f, 0f);

    [Tooltip("Vị trí local của fuse khi ON")]
    public Vector3 onPosition = new Vector3(0.03f, 0f, 0f);

    [Tooltip("Thời gian trượt (giây)")]
    public float slideDuration = 0.15f;

    [Header("References")]
    [Tooltip("Kéo PanelInteractZone của thùng điện vào")]
    public PanelInteractZone panelZone;

    [Tooltip("Kéo FusePanelManager vào để trigger kiểm tra sau khi toggle")]
    public FusePanelManager fusePanelManager;

    // ── State ──────────────────────────────────────────────
    private bool _isAnimating = false;

    void Start()
    {
        // Set vị trí ban đầu theo isOn
        transform.localPosition = isOn ? onPosition : offPosition;
    }

    void OnMouseDown()
    {
        // Chỉ cho tương tác khi đang trong panel mode
        if (panelZone == null || !panelZone.IsInPanelMode) return;
        if (_isAnimating) return;

        Toggle();
    }

    public void Toggle()
    {
        isOn = !isOn;
        StartCoroutine(AnimateSlide(isOn ? onPosition : offPosition));

        // Báo cho FusePanelManager kiểm tra lại trạng thái tổng
        fusePanelManager?.UpdatePanelState();

        Debug.Log($"[Switch {gameObject.name}] → {(isOn ? "ON" : "OFF")}");
    }

    IEnumerator AnimateSlide(Vector3 targetPos)
    {
        _isAnimating = true;

        Vector3 startPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.localPosition = targetPos;
        _isAnimating = false;
    }

    // Gizmo: preview vị trí ON/OFF trong editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.parent != null
            ? transform.parent.TransformPoint(onPosition)
            : transform.position + onPosition, 0.005f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.parent != null
            ? transform.parent.TransformPoint(offPosition)
            : transform.position + offPosition, 0.005f);
    }
}