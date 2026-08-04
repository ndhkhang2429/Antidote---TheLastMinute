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

    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayUI;

    private bool openingFinished;

    private IEnumerator Start()
    {
        BeginOpening();

        // Phần 1: chạy các ảnh giới thiệu.
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

        // Phần 2: chạy cảnh mở mắt.
        yield return StartCoroutine(PlayWakeUpCutscene());

        // Phần 3: trả lại gameplay.
        EndOpening();
    }

    private void BeginOpening()
    {
        openingFinished = false;

        SetPlayerScriptsEnabled(false);

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

        // Không cho màn hình đen che slideshow.
        if (wakeUpFadeCanvas != null)
        {
            wakeUpFadeCanvas.SetActive(false);
        }

        if (wakeUpDirector != null)
        {
            wakeUpDirector.Stop();
            wakeUpDirector.time = 0;
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

        // Bật Canvas đen ngay trước cảnh mở mắt.
        if (wakeUpFadeCanvas != null)
        {
            wakeUpFadeCanvas.SetActive(true);
        }

        if (wakeUpCamera != null)
        {
            wakeUpCamera.enabled = true;
        }

        wakeUpDirector.time = 0;
        wakeUpDirector.Evaluate();
        wakeUpDirector.Play();

        double timelineDuration = wakeUpDirector.duration;

        if (timelineDuration <= 0)
        {
            Debug.LogError(
                "WakeUpTimeline không có thời lượng hợp lệ."
            );

            yield break;
        }

        // Chờ Timeline chạy đến hết thời lượng.
        while (wakeUpDirector.time < timelineDuration)
        {
            yield return null;
        }

        wakeUpDirector.Stop();
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

        if (gameplayUI != null)
        {
            gameplayUI.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log(
            "Opening cutscene hoàn tất. Đã trả quyền điều khiển cho player."
        );
    }

    private void SetPlayerScriptsEnabled(bool enabledValue)
    {
        if (playerScriptsToDisable == null)
        {
            return;
        }

        foreach (MonoBehaviour playerScript in playerScriptsToDisable)
        {
            if (playerScript != null)
            {
                playerScript.enabled = enabledValue;
            }
        }
    }
}