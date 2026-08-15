using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Điều phối toàn bộ phần mở trận boss:
/// khóa Player -> fade -> teleport -> Timeline -> mở Player/UI/boss.
/// </summary>
public class BossEncounterController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Các script cần tắt trong cutscene: ThirdPersonController, PlayerAttack,...")]
    [SerializeField] private MonoBehaviour[] playerScriptsToDisable;
    [Tooltip("Các object cần ẩn trong cutscene, ví dụ FPS Hands.")]
    [SerializeField] private GameObject[] playerObjectsToHide;

    [Header("Teleport")]
    [SerializeField] private Transform bossRoomSpawnPoint;

    [Header("Boss")]
    [SerializeField] private MutatedBossZombie boss;
    [SerializeField] private BossHealthUI bossHealthUI;

    [Header("Intro Cutscene (Optional)")]
    [SerializeField] private PlayableDirector introDirector;
    [Tooltip("Thời gian dự phòng. Hết thời gian này Player vẫn được mở khóa.")]
    [SerializeField] private float cutsceneSafetyTimeout = 15f;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.65f;
    [SerializeField] private float blackScreenHold = 0.2f;

    private bool[] _scriptPreviousStates;
    private bool[] _objectPreviousStates;
    private bool _encounterStarted;
    private bool _sequenceRunning;
    private bool _directorStopped;

    public bool EncounterStarted => _encounterStarted;

    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTransform = playerObject.transform;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }

        boss?.PauseEncounter();
        bossHealthUI?.Hide();
    }

    /// <summary>Được AE_Door gọi. Trả false nếu thiếu reference bắt buộc.</summary>
    public bool StartBossEncounter()
    {
        if (_encounterStarted || _sequenceRunning) return false;

        if (playerTransform == null || bossRoomSpawnPoint == null || boss == null)
        {
            Debug.LogError(
                "[BossEncounter] Thiếu Player Transform, Boss Room Spawn Point hoặc Boss.",
                this);
            return false;
        }

        _encounterStarted = true;
        StartCoroutine(EncounterSequence());
        return true;
    }

    private IEnumerator EncounterSequence()
    {
        _sequenceRunning = true;
        LockPlayer();

        yield return FadeTo(1f);
        yield return new WaitForSecondsRealtime(blackScreenHold);

        TeleportPlayer();
        yield return null;

        yield return FadeTo(0f);

        if (introDirector != null && introDirector.playableAsset != null)
            yield return PlayIntroSafely();

        // Chỉ dùng dự phòng nếu Timeline không phát Signal.
        if (_sequenceRunning)
            FinishEncounterIntro();
    }

    private IEnumerator PlayIntroSafely()
    {
        _directorStopped = false;
        introDirector.stopped -= HandleDirectorStopped;
        introDirector.stopped += HandleDirectorStopped;
        introDirector.time = 0d;
        introDirector.Play();

        float timeout = cutsceneSafetyTimeout;
        double duration = introDirector.duration;
        if (!double.IsNaN(duration) && !double.IsInfinity(duration) && duration > 0d)
            timeout = Mathf.Max(timeout, (float)duration + 2f);

        float elapsed = 0f;
        while (!_directorStopped && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        introDirector.stopped -= HandleDirectorStopped;

        if (!_directorStopped)
        {
            Debug.LogWarning(
                "[BossEncounter] Timeline không kết thúc đúng hạn. Đã tự mở khóa Player.",
                this);
            introDirector.Stop();
        }
    }

    private void HandleDirectorStopped(PlayableDirector director)
    {
        _directorStopped = true;
    }

    private void FinishEncounterIntro()
    {
        if (!_sequenceRunning)
            return;

        UnlockPlayer();

        bossHealthUI?.ShowBoss(boss);
        boss.BeginEncounter();

        _sequenceRunning = false;
    }

    private void TeleportPlayer()
    {
        CharacterController controller = playerTransform.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;

        if (controllerWasEnabled)
            controller.enabled = false;

        playerTransform.SetPositionAndRotation(
            bossRoomSpawnPoint.position,
            bossRoomSpawnPoint.rotation);

        Physics.SyncTransforms();

        if (controllerWasEnabled)
            controller.enabled = true;
    }

    private void LockPlayer()
    {
        if (playerScriptsToDisable == null)
            playerScriptsToDisable = new MonoBehaviour[0];
        if (playerObjectsToHide == null)
            playerObjectsToHide = new GameObject[0];

        _scriptPreviousStates = new bool[playerScriptsToDisable.Length];
        for (int i = 0; i < playerScriptsToDisable.Length; i++)
        {
            MonoBehaviour script = playerScriptsToDisable[i];
            if (script == null || script == this) continue;

            _scriptPreviousStates[i] = script.enabled;
            script.enabled = false;
        }

        _objectPreviousStates = new bool[playerObjectsToHide.Length];
        for (int i = 0; i < playerObjectsToHide.Length; i++)
        {
            GameObject target = playerObjectsToHide[i];
            if (target == null) continue;

            _objectPreviousStates[i] = target.activeSelf;
            target.SetActive(false);
        }
    }

    private void UnlockPlayer()
    {
        if (playerScriptsToDisable == null)
            playerScriptsToDisable = new MonoBehaviour[0];
        if (playerObjectsToHide == null)
            playerObjectsToHide = new GameObject[0];

        if (_scriptPreviousStates != null)
        {
            for (int i = 0; i < playerScriptsToDisable.Length; i++)
            {
                MonoBehaviour script = playerScriptsToDisable[i];
                if (script != null && script != this)
                    script.enabled = _scriptPreviousStates[i];
            }
        }

        if (_objectPreviousStates != null)
        {
            for (int i = 0; i < playerObjectsToHide.Length; i++)
            {
                GameObject target = playerObjectsToHide[i];
                if (target != null)
                    target.SetActive(_objectPreviousStates[i]);
            }
        }
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true;
        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.01f;
    }

    private void OnDisable()
    {
        if (!_sequenceRunning) return;

        if (introDirector != null)
            introDirector.stopped -= HandleDirectorStopped;

        UnlockPlayer();
        _sequenceRunning = false;
    }
    public void FinishIntroFromTimeline()
    {
        if (!_sequenceRunning)
            return;

        FinishEncounterIntro();
    }
}