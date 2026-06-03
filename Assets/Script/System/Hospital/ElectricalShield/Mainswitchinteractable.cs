using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn vào GameObject của cần gạt (Main Switch).
/// Cập nhật: hỏi FusePanelManager trước khi cho phép gạt.
/// </summary>
public class MainSwitchInteractable : MonoBehaviour
{
    [Header("References")]
    public LightingManager lightingManager;
    public FusePanelManager fusePanelManager;

    [Header("Switch Animation")]
    public Vector3 rotationAxis = Vector3.right;
    public float offAngle = 40f;
    public float onAngle = -40f;
    public float animDuration = 0.3f;

    [Header("Feedback")]
    [Tooltip("Sound khi gạt thành công (tuỳ chọn)")]
    public AudioClip soundSuccess;
    [Tooltip("Sound khi panel chưa sẵn sàng")]
    public AudioClip soundFail;

    private AudioSource _audio;
    private bool isOn = false;
    private bool isAnimating = false;

    void Start()
    {
        transform.localRotation = Quaternion.Euler(rotationAxis * offAngle);
        _audio = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Gọi từ PlayerInteraction khi player nhấn F nhìn vào cần gạt.
    /// </summary>
    public void Interact()
    {
        if (isAnimating) return;

        // Kiểm tra panel đã đủ fuse chưa
        if (fusePanelManager != null && !fusePanelManager.IsPanelReady)
        {
            Debug.Log("[MainSwitch] Panel chưa sẵn sàng! Cần gắn đủ fuse.");
            PlaySound(soundFail);

            // Hiện thông báo cho player
            if (InteractionUIManager.Instance != null)
                InteractionUIManager.Instance.ShowPrompt("⚠ Thiếu cầu chì! Kiểm tra bảng hướng dẫn.");

            return;
        }

        // Panel OK → cho gạt
        isOn = !isOn;

        if (lightingManager != null)
            lightingManager.SetPower(isOn);
        else
            Debug.LogWarning("[MainSwitch] Chưa gán LightingManager!");

        PlaySound(soundSuccess);
        StartCoroutine(AnimateSwitch(isOn ? onAngle : offAngle));

        Debug.Log($"[MainSwitch] Power: {(isOn ? "ON" : "OFF")}");
    }

    IEnumerator AnimateSwitch(float targetAngle)
    {
        isAnimating = true;

        Quaternion startRot = transform.localRotation;
        Quaternion endRot = Quaternion.Euler(rotationAxis * targetAngle);
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        transform.localRotation = endRot;
        isAnimating = false;
    }

    void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2.5f);
    }
}