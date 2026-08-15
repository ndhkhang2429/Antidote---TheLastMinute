using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Điều khiển cutscene chuyển từ Phase 1 sang Phase 2.
///
/// Signal trên Timeline sẽ mở khóa gameplay trước,
/// trong khi âm thanh Sting vẫn có thể tiếp tục phát đến hết Timeline.
/// </summary>
public class BossPhaseTransitionController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector phase2Director;

    [Tooltip("Thời điểm đổi ngoại hình Phase 2 tính theo Timeline.")]
    [SerializeField] private double materialSwapTime = 1.8d;

    [Tooltip("Thời gian dự phòng nếu Timeline không kết thúc đúng cách.")]
    [SerializeField] private float safetyTimeout = 10f;

    [Header("Phase 2 Camera Orbit")]
    [Tooltip(
        "Pivot cha của Phase2FocusCamera. " +
        "Pivot sẽ được đưa tới vị trí hiện tại của boss trước khi Timeline chạy.")]
    [SerializeField] private Transform phase2CameraOrbit;

    [Tooltip("Cho pivot lấy cả độ cao Y của boss.")]
    [SerializeField] private bool snapOrbitYToBoss = true;

    [Tooltip(
        "Độ cao cộng thêm cho pivot so với vị trí boss. " +
        "Camera con vẫn giữ Local Position riêng.")]
    [SerializeField] private float orbitPivotYOffset = 0f;

    [Header("Player Lock")]
    [Tooltip(
        "FirstPersonController, PlayerAttack, PlayerInteraction " +
        "và các script input cần khóa.")]
    [SerializeField] private MonoBehaviour[] playerScriptsToDisable;

    [Tooltip("FPS Hands và các object cần ẩn trong cutscene.")]
    [SerializeField] private GameObject[] playerObjectsToHide;

    private MutatedBossZombie _boss;

    private bool[] _previousScriptStates;
    private bool[] _previousObjectStates;

    // Timeline vẫn còn đang chạy, bao gồm cả phần Sting cuối.
    private bool _sequenceActive;

    // Gameplay đã được mở lại bởi Signal hoặc fallback.
    private bool _gameplayReleased;

    private bool _directorStopped;
    private bool _visualsApplied;

    /// <summary>
    /// True khi gameplay Phase 2 vẫn chưa được mở lại.
    /// </summary>
    public bool IsRunning => _sequenceActive && !_gameplayReleased;

    /// <summary>
    /// Được MutatedBossZombie gọi khi HP chạm ngưỡng Phase 2.
    /// </summary>
    public bool PlayTransition(MutatedBossZombie boss)
    {
        if (_sequenceActive || boss == null)
            return false;

        if (phase2Director == null ||
            phase2Director.playableAsset == null)
        {
            Debug.LogWarning(
                "[BossPhaseTransition] Chưa gán PlayableDirector/Timeline. " +
                "Sẽ dùng fallback của Boss.",
                this);

            return false;
        }

        _boss = boss;
        _sequenceActive = true;
        _gameplayReleased = false;
        _visualsApplied = false;
        _directorStopped = false;

        StartCoroutine(TransitionSequence());
        return true;
    }

    private IEnumerator TransitionSequence()
    {
        LockPlayer();
        SnapCameraOrbitToBoss();

        phase2Director.stopped -= HandleDirectorStopped;
        phase2Director.stopped += HandleDirectorStopped;

        phase2Director.time = 0d;
        phase2Director.Play();

        float timeout = safetyTimeout;
        double timelineDuration = phase2Director.duration;

        if (!double.IsNaN(timelineDuration) &&
            !double.IsInfinity(timelineDuration) &&
            timelineDuration > 0d)
        {
            timeout = Mathf.Max(
                timeout,
                (float)timelineDuration + 2f);
        }

        float elapsedRealtime = 0f;

        while (!_directorStopped && elapsedRealtime < timeout)
        {
            elapsedRealtime += Time.unscaledDeltaTime;

            if (!_visualsApplied &&
                phase2Director.time >= materialSwapTime)
            {
                ApplyPhase2Visuals();
            }

            yield return null;
        }

        phase2Director.stopped -= HandleDirectorStopped;

        if (!_directorStopped)
        {
            Debug.LogWarning(
                "[BossPhaseTransition] Timeline quá thời gian. " +
                "Đã tự kết thúc để tránh khóa Player.",
                this);

            phase2Director.Stop();
        }

        // Fallback: dùng khi Signal bị thiếu hoặc không được gọi.
        ReleaseGameplay();

        CleanupSequence();
    }

    /// <summary>
    /// Hàm public để Signal Timeline gọi khi camera Phase 2 kết thúc.
    /// Không dừng Timeline nên Sting vẫn tiếp tục phát.
    /// </summary>
    public void FinishPhase2FromTimeline()
    {
        if (!_sequenceActive || _gameplayReleased)
            return;

        ReleaseGameplay();
    }

    private void ReleaseGameplay()
    {
        if (_gameplayReleased)
            return;

        _gameplayReleased = true;

        // Bảo đảm ngoại hình Phase 2 đã được áp dụng.
        ApplyPhase2Visuals();

        // Hoàn tất trạng thái Phase 2 trước khi mở điều khiển.
        if (_boss != null)
            _boss.CompletePhase2Transition();

        UnlockPlayer();
    }

    private void ApplyPhase2Visuals()
    {
        if (_visualsApplied)
            return;

        _visualsApplied = true;

        if (_boss != null)
            _boss.ApplyPhase2Visuals();
    }

    private void CleanupSequence()
    {
        _boss = null;
        _sequenceActive = false;
        _directorStopped = false;
    }

    private void SnapCameraOrbitToBoss()
    {
        if (phase2CameraOrbit == null)
        {
            Debug.LogWarning(
                "[BossPhaseTransition] Chưa gán Phase 2 Camera Orbit. " +
                "Camera sẽ sử dụng vị trí đặt sẵn trong Scene.",
                this);

            return;
        }

        if (_boss == null)
            return;

        Vector3 bossPosition = _boss.transform.position;
        Vector3 orbitPosition = phase2CameraOrbit.position;

        orbitPosition.x = bossPosition.x;
        orbitPosition.z = bossPosition.z;

        if (snapOrbitYToBoss)
            orbitPosition.y =
                bossPosition.y + orbitPivotYOffset;

        phase2CameraOrbit.position = orbitPosition;
    }

    private void HandleDirectorStopped(
        PlayableDirector stoppedDirector)
    {
        if (stoppedDirector == phase2Director)
            _directorStopped = true;
    }

    private void LockPlayer()
    {
        if (playerScriptsToDisable == null)
            playerScriptsToDisable = new MonoBehaviour[0];

        if (playerObjectsToHide == null)
            playerObjectsToHide = new GameObject[0];

        _previousScriptStates =
            new bool[playerScriptsToDisable.Length];

        for (int i = 0;
             i < playerScriptsToDisable.Length;
             i++)
        {
            MonoBehaviour targetScript =
                playerScriptsToDisable[i];

            if (targetScript == null ||
                targetScript == this)
            {
                continue;
            }

            _previousScriptStates[i] =
                targetScript.enabled;

            targetScript.enabled = false;
        }

        _previousObjectStates =
            new bool[playerObjectsToHide.Length];

        for (int i = 0;
             i < playerObjectsToHide.Length;
             i++)
        {
            GameObject targetObject =
                playerObjectsToHide[i];

            if (targetObject == null)
                continue;

            _previousObjectStates[i] =
                targetObject.activeSelf;

            targetObject.SetActive(false);
        }
    }

    private void UnlockPlayer()
    {
        if (playerScriptsToDisable != null &&
            _previousScriptStates != null)
        {
            int count = Mathf.Min(
                playerScriptsToDisable.Length,
                _previousScriptStates.Length);

            for (int i = 0; i < count; i++)
            {
                MonoBehaviour targetScript =
                    playerScriptsToDisable[i];

                if (targetScript != null &&
                    targetScript != this)
                {
                    targetScript.enabled =
                        _previousScriptStates[i];
                }
            }
        }

        if (playerObjectsToHide != null &&
            _previousObjectStates != null)
        {
            int count = Mathf.Min(
                playerObjectsToHide.Length,
                _previousObjectStates.Length);

            for (int i = 0; i < count; i++)
            {
                GameObject targetObject =
                    playerObjectsToHide[i];

                if (targetObject != null)
                {
                    targetObject.SetActive(
                        _previousObjectStates[i]);
                }
            }
        }
    }

    private void OnDisable()
    {
        if (!_sequenceActive)
            return;

        StopAllCoroutines();

        if (phase2Director != null)
            phase2Director.stopped -= HandleDirectorStopped;

        // Luôn bảo đảm Player không bị khóa khi object bị tắt.
        ReleaseGameplay();
        CleanupSequence();
    }
}