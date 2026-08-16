using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class OpeningCutsceneManager : MonoBehaviour
{
    [Header("Slideshow")]
    [SerializeField] private DeadRoofIntroSlideshow introSlideshow;

    [Header("Wake Up Cutscene")]
    [SerializeField] private PlayableDirector wakeUpDirector;
    [SerializeField] private GameObject wakeUpFadeCanvas;

    [Header("Wake Up Skip")]
    [SerializeField] private KeyCode skipWakeUpKey = KeyCode.Space;
    [SerializeField] private GameObject wakeUpSkipText;
    [SerializeField] private string wakeUpSkipMessage = "Press SPACE to skip";

    [Header("Radio Subtitle")]
    [Tooltip("Script chạy subtitle của radio trong Wake Up Timeline.")]
    [SerializeField] private RadioSubtitleTypewriter radioSubtitle;

    [Header("Cameras")]
    [SerializeField] private Camera wakeUpCamera;
    [SerializeField] private Camera gameplayCamera;

    [Header("Player")]
    [SerializeField] private MonoBehaviour[] playerScriptsToDisable;
    [SerializeField] private GameObject playerArmature;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayUI;

    [Header("Zombie Spawn")]
    [SerializeField] private GameObject zombieSpawnRoot;

    [Header("Timeline Safety")]
    [Tooltip("Khoảng sai số dùng để nhận biết Timeline đã đến cuối.")]
    [SerializeField] private double finishTolerance = 0.05d;

    [Tooltip("Thời gian chờ thêm tối đa nếu Timeline gặp lỗi.")]
    [SerializeField] private float safetyTimeoutExtra = 2f;

    [Header("Opening Tutorial")]
    [Tooltip("Tutorial zombie được đặt thủ công ngoài hành lang.")]
    [SerializeField] private GameObject tutorialZombie;

    [Tooltip("Zone 1 phải bị khóa cho tới khi player nhặt được pistol.")]
    [SerializeField] private GameObject firstZombieZone;

    private bool openingFinished;
    private bool wakeUpPlaying;
    private bool wakeUpSkipRequested;
    private bool wakeUpSkipInputArmed;

    private IEnumerator Start()
    {
        BeginOpening();

        if (introSlideshow != null)
        {
            yield return StartCoroutine(introSlideshow.PlaySlideshow());
        }
        else
        {
            Debug.LogError("OpeningCutsceneManager: Chưa gán Intro Slideshow.");
        }

        if (!openingFinished)
            yield return StartCoroutine(PlayWakeUpCutscene());

        EndOpening();
    }

    private void Update()
    {
        if (!wakeUpPlaying || openingFinished)
            return;

        // Sau slide cuối, bắt buộc phải thả Space trước khi được skip Timeline.
        if (!wakeUpSkipInputArmed)
        {
            if (!Input.GetKey(skipWakeUpKey))
                wakeUpSkipInputArmed = true;

            return;
        }

        if (Input.GetKeyDown(skipWakeUpKey))
            wakeUpSkipRequested = true;
    }

    private void BeginOpening()
    {
        openingFinished = false;
        wakeUpPlaying = false;
        wakeUpSkipRequested = false;
        wakeUpSkipInputArmed = false;

        SetWakeUpSkipTextVisible(false);
        ConfigureWakeUpSkipText();
        SetPlayerScriptsEnabled(false);

        if (playerArmature != null) playerArmature.SetActive(false);
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (gameplayCamera != null) gameplayCamera.enabled = false;
        if (wakeUpCamera != null) wakeUpCamera.enabled = false;
        if (zombieSpawnRoot != null) zombieSpawnRoot.SetActive(false);
        if (tutorialZombie != null) tutorialZombie.SetActive(false);
        if (firstZombieZone != null) firstZombieZone.SetActive(false);
        if (wakeUpFadeCanvas != null) wakeUpFadeCanvas.SetActive(false);

        if (wakeUpDirector != null)
        {
            wakeUpDirector.Stop();
            wakeUpDirector.time = 0d;
            wakeUpDirector.extrapolationMode = DirectorWrapMode.None;
            wakeUpDirector.Evaluate();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator PlayWakeUpCutscene()
    {
        if (wakeUpDirector == null)
        {
            Debug.LogError("OpeningCutsceneManager: Chưa gán Wake Up Director.");
            yield break;
        }

        if (wakeUpFadeCanvas != null) wakeUpFadeCanvas.SetActive(true);
        if (wakeUpCamera != null) wakeUpCamera.enabled = true;

        wakeUpDirector.Stop();
        wakeUpDirector.time = 0d;
        wakeUpDirector.extrapolationMode = DirectorWrapMode.None;
        wakeUpDirector.Evaluate();

        double timelineDuration = wakeUpDirector.duration;
        if (timelineDuration <= 0d ||
            double.IsInfinity(timelineDuration) ||
            double.IsNaN(timelineDuration))
        {
            Debug.LogError($"WakeUpTimeline có duration không hợp lệ: {timelineDuration}");
            yield break;
        }

        wakeUpSkipRequested = false;
        wakeUpSkipInputArmed = !Input.GetKey(skipWakeUpKey);
        wakeUpPlaying = true;
        SetWakeUpSkipTextVisible(true);
        wakeUpDirector.Play();

        float elapsedRealtime = 0f;
        float safetyTimeout = (float)timelineDuration + safetyTimeoutExtra;

        while (!wakeUpSkipRequested)
        {
            bool reachedEnd =
                wakeUpDirector.time >= timelineDuration - finishTolerance;

            bool stoppedNaturally =
                wakeUpDirector.state != PlayState.Playing;

            if (reachedEnd || stoppedNaturally)
                break;

            elapsedRealtime += Time.unscaledDeltaTime;
            if (elapsedRealtime >= safetyTimeout)
            {
                Debug.LogWarning(
                    "WakeUpTimeline vượt quá thời gian dự kiến. " +
                    "Buộc kết thúc để tránh khóa player."
                );
                break;
            }

            yield return null;
        }

        bool wasSkipped = wakeUpSkipRequested;
        wakeUpPlaying = false;
        wakeUpSkipRequested = false;
        SetWakeUpSkipTextVisible(false);
        wakeUpDirector.Stop();

        // Subtitle được chạy bằng coroutine riêng nên phải dừng thủ công.
        StopRadioSubtitle();

        Debug.Log(wasSkipped
            ? "WakeUpTimeline đã được bỏ qua bằng phím Space."
            : $"WakeUpTimeline kết thúc. Duration: {timelineDuration:F3}");
    }

    private void EndOpening()
    {
        if (openingFinished)
            return;

        openingFinished = true;
        wakeUpPlaying = false;
        wakeUpSkipRequested = false;
        SetWakeUpSkipTextVisible(false);

        // Bảo đảm subtitle không tồn tại sau mọi đường kết thúc Opening.
        StopRadioSubtitle();

        if (wakeUpDirector != null) wakeUpDirector.Stop();
        if (wakeUpCamera != null) wakeUpCamera.enabled = false;
        if (gameplayCamera != null) gameplayCamera.enabled = true;
        if (wakeUpFadeCanvas != null) wakeUpFadeCanvas.SetActive(false);

        SetPlayerScriptsEnabled(true);

        if (playerArmature != null) playerArmature.SetActive(true);
        if (gameplayUI != null) gameplayUI.SetActive(true);
        if (zombieSpawnRoot != null) zombieSpawnRoot.SetActive(true);

        // Zone 1 vẫn khóa cho tới khi player nhặt pistol.
        if (firstZombieZone != null) firstZombieZone.SetActive(false);

        // Tutorial zombie được bật riêng sau intro.
        if (tutorialZombie != null) tutorialZombie.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log(
            "Opening cutscene hoàn tất. " +
            "Camera và quyền điều khiển đã được trả cho player."
        );
    }

    private void ConfigureWakeUpSkipText()
    {
        if (wakeUpSkipText == null)
            return;

        TextMeshProUGUI label =
            wakeUpSkipText.GetComponent<TextMeshProUGUI>();

        if (label == null)
            label = wakeUpSkipText.GetComponentInChildren<TextMeshProUGUI>(true);

        if (label != null)
            label.text = wakeUpSkipMessage;
    }

    private void SetWakeUpSkipTextVisible(bool visible)
    {
        if (wakeUpSkipText != null)
            wakeUpSkipText.SetActive(visible);
    }

    private void StopRadioSubtitle()
    {
        if (radioSubtitle != null)
            radioSubtitle.StopImmediately();
    }

    private void SetPlayerScriptsEnabled(bool enabledValue)
    {
        if (playerScriptsToDisable == null)
            return;

        foreach (MonoBehaviour playerScript in playerScriptsToDisable)
        {
            if (playerScript != null)
                playerScript.enabled = enabledValue;
        }
    }

    private void OnDisable()
    {
        if (!openingFinished)
            EndOpening();
    }
}