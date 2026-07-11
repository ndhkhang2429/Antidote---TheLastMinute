using System.Collections;
using UnityEngine;

/// <summary>
/// Component tiện ích: gắn vào bất kỳ object nào cần "gọi cả đàn zombie tới"
/// (gạt sai cầu chì, mở tủ cấp cứu, bẫy...). Gọi TriggerAlarm() từ code có sẵn của object đó,
/// hoặc nối vào UnityEvent nếu object đó có (VD "On Access Denied" của Keypad).
/// </summary>
public class AlarmTriggerZone : MonoBehaviour
{
    [Header("Cấu hình Spawn")]
    [Tooltip("Các điểm zombie có thể xuất hiện quanh khu vực này")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Số lượng zombie spawn mỗi lần báo động")]
    [SerializeField] private int zombieCount = 3;

    [Header("Hiệu ứng (tùy chọn)")]
    [SerializeField] private AudioSource alarmAudioSource;
    [SerializeField] private AudioClip alarmSfx;
    [SerializeField] private Light alarmLight;
    [SerializeField] private float lightFlashDuration = 3f;

    /// <summary>
    /// Gọi hàm này (từ code hoặc UnityEvent) để phát báo động - spawn zombieCount zombie
    /// ngẫu nhiên loại tại các spawnPoints, tất cả lao thẳng tới player.
    /// </summary>
    public void TriggerAlarm()
    {
        AlarmSystem.SpawnHorde(spawnPoints, zombieCount);

        if (alarmAudioSource != null && alarmSfx != null)
            alarmAudioSource.PlayOneShot(alarmSfx);

        if (alarmLight != null)
            StartCoroutine(FlashLight());
    }

    private IEnumerator FlashLight()
    {
        float elapsed = 0f;
        alarmLight.enabled = true;
        while (elapsed < lightFlashDuration)
        {
            alarmLight.enabled = !alarmLight.enabled;
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }
        alarmLight.enabled = false;
    }
}