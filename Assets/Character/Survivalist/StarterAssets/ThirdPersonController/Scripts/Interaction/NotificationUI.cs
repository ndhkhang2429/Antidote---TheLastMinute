using UnityEngine;
using TMPro;

public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI _textLabel;

    [Header("Settings")]
    [SerializeField] private float _displayDuration = 2.5f; // Hiện trong 2.5 giây rồi tắt

    private float _hideTime;

    void Awake()
    {
        // Khởi tạo Singleton để mọi script khác đều gọi được
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_textLabel == null) _textLabel = GetComponent<TextMeshProUGUI>();

        // Vào game tự động ẩn chữ đi
        _textLabel.gameObject.SetActive(false);
    }

    // Hàm gọi loa phóng thanh hiện chữ
    public void ShowNotification(string message)
    {
        if (_textLabel == null) return;

        _textLabel.text = message;
        _textLabel.gameObject.SetActive(true);
        _hideTime = Time.time + _displayDuration; // Đặt lịch tắt
    }

    void Update()
    {
        // Hết thời gian tự động ẩn
        if (_textLabel.gameObject.activeSelf && Time.time >= _hideTime)
        {
            _textLabel.gameObject.SetActive(false);
        }
    }
}