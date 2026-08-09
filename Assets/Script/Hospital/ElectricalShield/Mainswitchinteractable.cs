using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn vào GameObject của cần gạt (Main Switch).
/// Đã tích hợp IQuestRequirement và NotificationUI
/// để hiển thị cảnh báo thông minh.
/// </summary>
public class MainSwitchInteractable :
    MonoBehaviour,
    IQuestRequirement
{
    [Header("References")]
    public LightingManager lightingManager;
    public FusePanelManager fusePanelManager;

    [Header("Switch Animation")]
    public Vector3 rotationAxis = Vector3.right;
    public float offAngle = 40f;
    public float onAngle = -40f;

    [Min(0.01f)]
    public float animDuration = 0.3f;

    [Header("Feedback")]
    [Tooltip("Tiếng cơ học khi cần cầu dao được gạt.")]
    public AudioClip switchFlipSound;

    [Tooltip("Âm thanh khi nguồn điện được kích hoạt thành công.")]
    public AudioClip soundSuccess;

    [Tooltip("Âm thanh khi bảng điện chưa sẵn sàng.")]
    public AudioClip soundFail;

    [Header("Audio Volume")]
    [Range(0f, 1f)]
    public float switchFlipVolume = 0.75f;

    [Range(0f, 1f)]
    public float successVolume = 0.8f;

    [Range(0f, 1f)]
    public float failVolume = 0.6f;

    [Header("Pitch Variation")]
    public Vector2 pitchRange =
        new Vector2(0.97f, 1.03f);

    private AudioSource _audio;
    private bool isOn;
    private bool isAnimating;

    private void Start()
    {
        transform.localRotation =
            Quaternion.Euler(
                rotationAxis.normalized * offAngle
            );

        _audio = GetComponent<AudioSource>();
    }

    // Cần gạt không yêu cầu người chơi
    // phải cầm vật phẩm cụ thể trên tay.
    public ItemDataSO GetRequiredItem()
    {
        return null;
    }

    public string GetPrompt()
    {
        if (isAnimating)
        {
            return null;
        }

        return isOn
            ? "[F] Ngắt nguồn điện tổng"
            : "[F] Gạt cần kích hoạt nguồn điện tổng";
    }

    public bool IsCompleted()
    {
        return isOn;
    }

    /// <summary>
    /// Xử lý khi người chơi nhìn vào cần gạt và nhấn F.
    /// </summary>
    public bool TryUseItem(InventorySystem inventory)
    {
        if (isAnimating)
        {
            return false;
        }

        // Điện đang bật: cho phép gạt tắt tự do.
        if (isOn)
        {
            isOn = false;

            if (lightingManager != null)
            {
                lightingManager.SetPower(false);
            }

            PlaySound(
                switchFlipSound,
                switchFlipVolume
            );

            StartCoroutine(
                AnimateSwitch(offAngle)
            );

            Debug.Log("[MainSwitch] Power: OFF");

            return true;
        }

        // Điện đang tắt: kiểm tra điều kiện để bật.
        if (fusePanelManager != null)
        {
            bool missingFuse = false;

            foreach (FuseSlot slot in
                     fusePanelManager.allSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                if (slot.requiresFuse &&
                    !slot.HasFuse)
                {
                    missingFuse = true;
                    break;
                }
            }

            if (missingFuse)
            {
                PlaySound(
                    soundFail,
                    failVolume
                );

                NotificationUI.Instance
                    ?.ShowNotification(
                        "Not all required fuses have been installed!"
                    );

                return false;
            }

            if (!fusePanelManager.CheckAllFuses())
            {
                PlaySound(
                    soundFail,
                    failVolume
                );

                NotificationUI.Instance
                    ?.ShowNotification(
                        "The fuse positions do not match the wiring diagram!"
                    );

                return false;
            }

            if (!fusePanelManager.CheckAllSwitches())
            {
                PlaySound(
                    soundFail,
                    failVolume
                );

                NotificationUI.Instance
                    ?.ShowNotification(
                        "The auxiliary switch configuration is incorrect!"
                    );

                return false;
            }
        }

        // Tất cả điều kiện đều hợp lệ:
        // kích hoạt nguồn điện.
        isOn = true;

        if (lightingManager != null)
        {
            lightingManager.SetPower(true);
        }
        else
        {
            Debug.LogWarning(
                "[MainSwitch] Chưa gán LightingManager!"
            );
        }

        // Tiếng cơ học của cần gạt.
        PlaySound(
            switchFlipSound,
            switchFlipVolume
        );

        // Tiếng điện khởi động thành công.
        PlaySound(
            soundSuccess,
            successVolume
        );

        StartCoroutine(
            AnimateSwitch(onAngle)
        );

        NotificationUI.Instance
            ?.ShowNotification(
                "Power has been restored throughout the hospital!"
            );

        Debug.Log("[MainSwitch] Power: ON");

        return true;
    }

    /// <summary>
    /// Giữ lại hàm Interact cũ để không làm gãy
    /// các hệ thống đang gọi trực tiếp hàm này.
    /// </summary>
    public void Interact()
    {
        TryUseItem(
            InventorySystem.Instance
        );
    }

    public void CheatTurnOnPower()
    {
        if (isOn)
        {
            Debug.Log(
                "[MainSwitch Cheat] " +
                "Nguồn điện đã được bật trước đó."
            );

            return;
        }

        StopAllCoroutines();

        isAnimating = false;
        isOn = true;

        if (lightingManager == null)
        {
            lightingManager =
                LightingManager.Instance;
        }

        if (lightingManager != null)
        {
            lightingManager.SetPower(true);
        }
        else
        {
            Debug.LogWarning(
                "[MainSwitch Cheat] " +
                "Không tìm thấy LightingManager."
            );
        }

        PlaySound(
            switchFlipSound,
            switchFlipVolume
        );

        PlaySound(
            soundSuccess,
            successVolume
        );

        StartCoroutine(
            AnimateSwitch(onAngle)
        );

        NotificationUI.Instance
            ?.ShowNotification(
                "Cheat: Hospital power has been restored!"
            );

        Debug.Log(
            "[MainSwitch Cheat] Power: ON"
        );
    }

    private IEnumerator AnimateSwitch(
        float targetAngle)
    {
        isAnimating = true;

        Quaternion startRotation =
            transform.localRotation;

        Quaternion targetRotation =
            Quaternion.Euler(
                rotationAxis.normalized * targetAngle
            );

        float elapsed = 0f;
        float safeDuration =
            Mathf.Max(0.01f, animDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / safeDuration
            );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            transform.localRotation =
                Quaternion.Lerp(
                    startRotation,
                    targetRotation,
                    smoothProgress
                );

            yield return null;
        }

        transform.localRotation =
            targetRotation;

        isAnimating = false;
    }

    private void PlaySound(
        AudioClip clip,
        float volume)
    {
        if (_audio == null || clip == null)
        {
            return;
        }

        _audio.pitch = Random.Range(
            pitchRange.x,
            pitchRange.y
        );

        _audio.PlayOneShot(
            clip,
            volume
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            2.5f
        );
    }
}