using System.Collections;
using UnityEngine;

/// <summary>
/// Cần gạt nguồn điện chính của bệnh viện.
/// Kiểm tra bảng cầu chì, điều khiển LightingManager,
/// phát âm thanh và cập nhật Objective System.
/// </summary>
public class MainSwitchInteractable :
    MonoBehaviour,
    IQuestRequirement
{
    private const string RestorePowerObjectiveID =
        "restore_power";

    private const string RestorePowerDescription =
        "Restore power to the hospital";

    private const string FindRooftopRouteObjectiveID =
        "find_rooftop_route";

    private const string FindRooftopRouteDescription =
        "Find a route to the rooftop";

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

    [Header("Objective Settings")]
    [Tooltip(
        "Thời gian chờ trước khi hiện nhiệm vụ tìm đường lên sân thượng."
    )]
    [Min(0f)]
    [SerializeField] private float nextObjectiveDelay = 2f;

    private AudioSource _audio;

    private bool isOn;
    private bool isAnimating;
    private bool hasStartedRooftopObjective;

    private void Start()
    {
        transform.localRotation =
            Quaternion.Euler(
                rotationAxis.normalized * offAngle
            );

        _audio = GetComponent<AudioSource>();

        if (lightingManager == null)
        {
            lightingManager = LightingManager.Instance;
        }
    }

    /// <summary>
    /// Cần gạt không yêu cầu player cầm vật phẩm cụ thể.
    /// </summary>
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
            ? "Turn off the main power supply"
            : "Flip the switch to activate the main power supply";
    }

    public bool IsCompleted()
    {
        return isOn;
    }

    /// <summary>
    /// Được gọi khi player nhìn vào cần gạt và nhấn nút tương tác.
    /// </summary>
    public bool TryUseItem(InventorySystem inventory)
    {
        if (isAnimating)
        {
            return false;
        }

        if (isOn)
        {
            TurnPowerOff();
            return true;
        }

        if (!CanTurnPowerOn())
        {
            return false;
        }

        TurnPowerOn(false);
        return true;
    }

    /// <summary>
    /// Kiểm tra toàn bộ điều kiện của bảng cầu chì.
    /// </summary>
    private bool CanTurnPowerOn()
    {
        if (fusePanelManager == null)
        {
            return true;
        }

        foreach (FuseSlot slot in fusePanelManager.allSlots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.requiresFuse && !slot.HasFuse)
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

        return true;
    }

    /// <summary>
    /// Bật nguồn điện bệnh viện.
    /// </summary>
    private void TurnPowerOn(bool activatedByCheat)
    {
        isOn = true;

        if (lightingManager == null)
        {
            lightingManager = LightingManager.Instance;
        }

        if (lightingManager != null)
        {
            lightingManager.SetPower(true);
        }
        else
        {
            Debug.LogWarning(
                "[MainSwitch] Không tìm thấy LightingManager."
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

        string notificationMessage =
            activatedByCheat
                ? "Cheat: Hospital power has been restored!"
                : "Power has been restored throughout the hospital!";

        NotificationUI.Instance
            ?.ShowNotification(notificationMessage);

        CompletePowerObjective();

        Debug.Log(
            activatedByCheat
                ? "[MainSwitch Cheat] Power: ON"
                : "[MainSwitch] Power: ON"
        );
    }

    /// <summary>
    /// Tắt nguồn điện bệnh viện.
    /// Việc tắt lại điện không hoàn tác objective đã hoàn thành.
    /// </summary>
    private void TurnPowerOff()
    {
        isOn = false;

        if (lightingManager == null)
        {
            lightingManager = LightingManager.Instance;
        }

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
    }

    /// <summary>
    /// Hoàn thành nhiệm vụ mở điện và chuẩn bị objective tiếp theo.
    /// </summary>
    private void CompletePowerObjective()
    {
        if (ObjectiveManager.Instance == null)
        {
            Debug.LogWarning(
                "[MainSwitch] Không tìm thấy ObjectiveManager."
            );

            return;
        }

        /*
         * Trường hợp player hoặc cheat tới phòng điện
         * mà chưa đi qua trigger nhận nhiệm vụ.
         */
        if (!ObjectiveManager.Instance.HasObjective(
                RestorePowerObjectiveID))
        {
            ObjectiveManager.Instance.AddObjective(
                RestorePowerObjectiveID,
                RestorePowerDescription
            );
        }

        if (!ObjectiveManager.Instance.IsObjectiveCompleted(
                RestorePowerObjectiveID))
        {
            ObjectiveManager.Instance.CompleteObjective(
                RestorePowerObjectiveID
            );
        }

        if (!hasStartedRooftopObjective)
        {
            hasStartedRooftopObjective = true;

            StartCoroutine(
                GiveRooftopRouteObjective()
            );
        }
    }

    /// <summary>
    /// Hiện nhiệm vụ tìm đường lên sân thượng
    /// sau khi dòng nhiệm vụ mở điện biến mất.
    /// </summary>
    private IEnumerator GiveRooftopRouteObjective()
    {
        yield return new WaitForSecondsRealtime(
            nextObjectiveDelay
        );

        if (ObjectiveManager.Instance == null)
        {
            Debug.LogWarning(
                "[MainSwitch] ObjectiveManager đã bị mất."
            );

            yield break;
        }

        if (!ObjectiveManager.Instance.HasObjective(
                FindRooftopRouteObjectiveID))
        {
            ObjectiveManager.Instance.AddObjective(
                FindRooftopRouteObjectiveID,
                FindRooftopRouteDescription
            );
        }
    }

    /// <summary>
    /// Giữ lại để không làm hỏng những hệ thống cũ
    /// đang gọi trực tiếp Interact().
    /// </summary>
    public void Interact()
    {
        TryUseItem(
            InventorySystem.Instance
        );
    }

    /// <summary>
    /// Bật điện bằng Developer Cheat.
    /// Vẫn cập nhật đầy đủ Objective System.
    /// </summary>
    public void CheatTurnOnPower()
    {
        if (isOn)
        {
            Debug.Log(
                "[MainSwitch Cheat] Nguồn điện đã được bật trước đó."
            );

            return;
        }

        StopAllCoroutines();

        isAnimating = false;

        TurnPowerOn(true);
    }

    private IEnumerator AnimateSwitch(float targetAngle)
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

            float progress =
                Mathf.Clamp01(
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

        _audio.pitch =
            Random.Range(
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