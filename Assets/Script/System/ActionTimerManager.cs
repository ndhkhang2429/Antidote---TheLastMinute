using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionTimerManager : MonoBehaviour
{
    public static ActionTimerManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject timerPanel;      // Kéo object cha ActionTimerUI vào đây
    public Image fillCircle;           // Kéo FillCircle vào đây
    public TextMeshProUGUI actionText; // Kéo Text chữ vào đây

    private Coroutine _currentActionCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        timerPanel.SetActive(false); // Ẩn đi khi mới vào game
    }

    // Hàm này sẽ được gọi khi bạn bấm hồi máu hoặc nạp đạn
    public void StartAction(string actionName, float duration, Action onComplete)
    {
        // Nếu đang làm dở một hành động khác thì hủy nó đi
        if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);

        _currentActionCoroutine = StartCoroutine(ActionRoutine(actionName, duration, onComplete));
    }

    private IEnumerator ActionRoutine(string actionName, float duration, Action onComplete)
    {
        timerPanel.SetActive(true);
        actionText.text = actionName;
        fillCircle.fillAmount = 0f;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fillCircle.fillAmount = timer / duration; // Cập nhật vòng tròn UI
            yield return null;
        }

        // Chạy xong 100%
        timerPanel.SetActive(false);
        _currentActionCoroutine = null;

        // THỰC THI HÀNH ĐỘNG (Ví dụ: Hồi máu, cộng đạn vào súng)
        onComplete?.Invoke();
    }

    // (Tùy chọn) Hàm để hủy hành động giữa chừng nếu bị Zombie đánh
    public void CancelAction()
    {
        if (_currentActionCoroutine != null)
        {
            StopCoroutine(_currentActionCoroutine);
            _currentActionCoroutine = null;
            timerPanel.SetActive(false);
            Debug.Log("Hành động bị gián đoạn!");
        }
    }
}