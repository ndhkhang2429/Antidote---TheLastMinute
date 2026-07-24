using UnityEngine;

/// <summary>
/// Gắn component này trên CÙNG GameObject với WorldItem (thẻ phòng boss, ví dụ Card_GateBlue).
/// Khi item được nhặt: phát tiếng loa báo động trong _alarmDuration giây, sau đó (hoặc trong lúc
/// loa đang kêu) gọi AlarmSystem.SpawnHorde() để kéo cả đàn zombie tới từ các điểm spawn quanh phòng.
/// 
/// Cách nối: WorldItem.onPickedUp (Inspector) -> kéo GameObject này vào -> chọn
/// KeyRoomAlarmTrigger -> TriggerAlarm() trong dropdown function.
/// </summary>
[RequireComponent(typeof(WorldItem))]
public class KeyRoomAlarmTrigger : MonoBehaviour
{
    [Header("Alarm Config")]
    [Tooltip("Các điểm spawn zombie quanh phòng - đặt gần NavMesh, nên rải ở nhiều hướng (cửa, góc phòng, hành lang dẫn vào) để tạo cảm giác bị bao vây")]
    [SerializeField] private Transform[] _spawnPoints;

    [Tooltip("Số lượng zombie spawn trong đợt báo động này")]
    [SerializeField] private int _hordeCount = 6;

    [Header("Loa báo động")]
    [Tooltip("AudioSource gắn trên object cái loa trong scene - kéo vào đây, kéo sẵn audio clip tiếng còi báo động vào AudioSource đó")]
    [SerializeField] private AudioSource _alarmAudioSource;

    [Tooltip("Đèn nhấp nháy đỏ trên loa (nếu có) - object này sẽ được bật lên trong lúc báo động kêu, tắt lại sau khi xong")]
    [SerializeField] private GameObject _alarmLightVisual;

    [Tooltip("Loa kêu trong bao nhiêu giây trước khi zombie ùa vào")]
    [SerializeField] private float _alarmDuration = 3f;

    [Tooltip("Nếu true: zombie spawn NGAY khi loa bắt đầu kêu (ùa vào trong lúc còi vẫn đang kêu). Nếu false: đợi loa kêu xong hết _alarmDuration rồi mới spawn.")]
    [SerializeField] private bool _spawnDuringAlarm = true;

    private bool _hasTriggered = false;

    /// <summary>
    /// Nối hàm này vào WorldItem.onPickedUp (Inspector -> kéo GameObject này vào slot Object,
    /// chọn KeyRoomAlarmTrigger -> TriggerAlarm trong dropdown function).
    /// </summary>
    public void TriggerAlarm()
    {
        if (_hasTriggered) return;
        _hasTriggered = true;

        // Bật đèn báo động
        if (_alarmLightVisual != null)
            _alarmLightVisual.SetActive(true);

        // Phát tiếng loa
        if (_alarmAudioSource != null)
            _alarmAudioSource.Play();

        if (_spawnDuringAlarm)
        {
            // Zombie ùa vào ngay trong lúc loa đang kêu
            SpawnHordeNow();
        }
        else
        {
            // Đợi loa kêu xong hết mới spawn
            Invoke(nameof(SpawnHordeNow), _alarmDuration);
        }

        // Tắt đèn báo động sau khi hết thời lượng (dù zombie spawn lúc nào cũng vậy)
        Invoke(nameof(StopAlarmVisual), _alarmDuration);
    }

    private void SpawnHordeNow()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: chưa gán _spawnPoints, không thể trigger horde.");
            return;
        }

        AlarmSystem.SpawnHorde(_spawnPoints, _hordeCount);
    }

    private void StopAlarmVisual()
    {
        if (_alarmLightVisual != null)
            _alarmLightVisual.SetActive(false);
    }
}