using System.Collections;
using UnityEngine;

public class SwitchSlider : MonoBehaviour
{
    [Header("Config")]
    public bool isOn = false;

    [Tooltip("Trục và khoảng cách dịch chuyển từ vị trí gốc khi ON.\n" +
             "Ví dụ: (0, 0, 0.03) = dịch 3cm theo trục Z khi ON\n" +
             "Khi OFF sẽ ở vị trí gốc ban đầu.")]
    public Vector3 onOffset = new Vector3(0f, 0f, 0.03f);

    public float slideDuration = 0.15f;

    [Header("References")]
    public PanelInteractZone panelZone;
    public FusePanelManager fusePanelManager;

    // ── State ──────────────────────────────────────────────
    private Vector3 _originPosition; // Vị trí gốc lúc Start
    private bool _isAnimating = false;

    void Start()
    {
        // Ghi nhớ vị trí gốc
        _originPosition = transform.localPosition;

        // Set trạng thái ban đầu
        if (isOn)
            transform.localPosition = _originPosition + onOffset;
    }

    void OnMouseDown()
    {
        if (panelZone == null || !panelZone.IsInPanelMode) return;
        if (_isAnimating) return;
        Toggle();
    }

    public void Toggle()
    {
        isOn = !isOn;
        Vector3 target = isOn ? _originPosition + onOffset : _originPosition;
        StartCoroutine(AnimateSlide(target));
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

    void OnDrawGizmosSelected()
    {
        // Preview vị trí ON trong editor
        Vector3 origin = transform.localPosition;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.parent != null
            ? transform.parent.TransformPoint(origin + onOffset)
            : transform.position + onOffset, 0.005f);
    }
}