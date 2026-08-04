using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class OpeningCutsceneManager : MonoBehaviour
{
    [Header("Slideshow")]
    [SerializeField] private DeadRoofIntroSlideshow introSlideshow;

    [Header("Wake Up Cutscene")]
    [SerializeField] private PlayableDirector wakeUpDirector;
    [SerializeField] private GameObject wakeUpFadeCanvas;

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

    private bool openingFinished;

    private IEnumerator Start()
    {
        BeginOpening();

        // Phần 1: slideshow.
        if (introSlideshow != null)
        {
            yield return StartCoroutine(
                introSlideshow.PlaySlideshow()
            );
        }
        else
        {
            Debug.LogError(
                "OpeningCutsceneManager: Chưa gán Intro Slideshow."
            );
        }

        // Phần 2: cảnh mở mắt, radio và subtitle.
        yield return StartCoroutine(
            PlayWakeUpCutscene()
        );

        // Phần 3: trả gameplay.
        EndOpening();
    }

    private void BeginOpening()
    {
        openingFinished = false;

        SetPlayerScriptsEnabled(false);

        if (playerArmature != null)
        {
            playerArmature.SetActive(false);
        }

        if (gameplayUI != null)
        {
            gameplayUI.SetActive(false);
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.enabled = false;
        }

        if (wakeUpCamera != null)
        {
            wakeUpCamera.enabled = false;
        }

        if (zombieSpawnRoot != null)
        {
            zombieSpawnRoot.SetActive(false);
        }

        // Không để Canvas đen che slideshow.
        if (wakeUpFadeCanvas != null)
        {
            wakeUpFadeCanvas.SetActive(false);
        }

        if (wakeUpDirector != null)
        {
            wakeUpDirector.Stop();
            wakeUpDirector.time = 0d;

            // Không để Timeline giữ mãi trạng thái Playing ở frame cuối.
            wakeUpDirector.extrapolationMode =
                DirectorWrapMode.None;

            wakeUpDirector.Evaluate();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator PlayWakeUpCutscene()
    {
        if (wakeUpDirector == null)
        {
            Debug.LogError(
                "OpeningCutsceneManager: Chưa gán Wake Up Director."
            );

            yield break;
        }

        if (wakeUpFadeCanvas != null)
        {
            wakeUpFadeCanvas.SetActive(true);
        }

        if (wakeUpCamera != null)
        {
            wakeUpCamera.enabled = true;
        }

        wakeUpDirector.Stop();
        wakeUpDirector.time = 0d;
        wakeUpDirector.extrapolationMode =
            DirectorWrapMode.None;
        wakeUpDirector.Evaluate();

        double timelineDuration =
            wakeUpDirector.duration;

        if (timelineDuration <= 0d ||
            double.IsInfinity(timelineDuration) ||
            double.IsNaN(timelineDuration))
        {
            Debug.LogError(
                $"WakeUpTimeline có duration không hợp lệ: " +
                $"{timelineDuration}"
            );

            yield break;
        }

        wakeUpDirector.Play();

        float elapsedRealtime = 0f;

        float safetyTimeout =
            (float)timelineDuration +
            safetyTimeoutExtra;

        while (true)
        {
            bool reachedEnd =
                wakeUpDirector.time >=
                timelineDuration - finishTolerance;

            bool stoppedNaturally =
                wakeUpDirector.state !=
                PlayState.Playing;

            if (reachedEnd || stoppedNaturally)
            {
                break;
            }

            elapsedRealtime +=
                Time.unscaledDeltaTime;

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

        wakeUpDirector.Stop();

        Debug.Log(
            $"WakeUpTimeline kết thúc. " +
            $"Time: {wakeUpDirector.time:F3}, " +
            $"Duration: {timelineDuration:F3}"
        );
    }

    private void EndOpening()
    {
        if (openingFinished)
        {
            return;
        }

        openingFinished = true;

        if (wakeUpDirector != null)
        {
            wakeUpDirector.Stop();
        }

        if (wakeUpCamera != null)
        {
            wakeUpCamera.enabled = false;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.enabled = true;
        }

        if (wakeUpFadeCanvas != null)
        {
            wakeUpFadeCanvas.SetActive(false);
        }

        SetPlayerScriptsEnabled(true);

        if (playerArmature != null)
        {
            playerArmature.SetActive(true);
        }

        if (gameplayUI != null)
        {
            gameplayUI.SetActive(true);
        }

        if (zombieSpawnRoot != null)
        {
            zombieSpawnRoot.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log(
            "Opening cutscene hoàn tất. " +
            "Camera và quyền điều khiển đã được trả cho player."
        );
    }

    private void SetPlayerScriptsEnabled(bool enabledValue)
    {
        if (playerScriptsToDisable == null)
        {
            return;
        }

        foreach (
            MonoBehaviour playerScript
            in playerScriptsToDisable)
        {
            if (playerScript != null)
            {
                playerScript.enabled =
                    enabledValue;
            }
        }
    }

    private void OnDisable()
    {
        if (!openingFinished)
        {
            EndOpening();
        }
    }
}