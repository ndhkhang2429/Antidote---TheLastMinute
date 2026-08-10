using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Chạy Timeline chuyển Phase 1 -> Phase 2 và luôn mở khóa Player khi kết thúc/lỗi.
/// </summary>
public class BossPhaseTransitionController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector phase2Director;
    [SerializeField] private float materialSwapTime = 1.8f;
    [SerializeField] private float safetyTimeout = 8f;

    [Header("Phase 2 Camera Orbit")]
    [Tooltip("Pivot cha của Phase2FocusCamera. Pivot sẽ được đưa tới vị trí hiện tại của boss trước khi Timeline chạy.")]
    [SerializeField] private Transform phase2CameraOrbit;
    [Tooltip("Bật nếu pivot cũng phải lấy Y của boss. Thông thường nên bật.")]
    [SerializeField] private bool snapOrbitYToBoss = true;
    [Tooltip("Độ cao cộng thêm cho pivot so với chân boss. Camera con vẫn giữ Local Y riêng.")]
    [SerializeField] private float orbitPivotYOffset = 0f;

    [Header("Player Lock")]
    [Tooltip("FirstPersonController, PlayerAttack, PlayerInteraction và các script input cần khóa.")]
    [SerializeField] private MonoBehaviour[] playerScriptsToDisable;
    [Tooltip("FPS Hands, HUD và các object cần ẩn trong cutscene.")]
    [SerializeField] private GameObject[] playerObjectsToHide;

    private MutatedBossZombie _boss;
    private bool[] _previousScriptStates;
    private bool[] _previousObjectStates;
    private bool _running;
    private bool _directorStopped;
    private bool _visualsApplied;

    public bool IsRunning => _running;

    /// <summary>Được MutatedBossZombie gọi khi HP chạm ngưỡng Phase 2.</summary>
    public bool PlayTransition(MutatedBossZombie boss)
    {
        if (_running || boss == null)
            return false;

        if (phase2Director == null || phase2Director.playableAsset == null)
        {
            Debug.LogWarning(
                "[BossPhaseTransition] Chưa gán PlayableDirector/Timeline. Dùng fallback.",
                this);
            return false;
        }

        _boss = boss;
        _running = true;
        StartCoroutine(TransitionSequence());
        return true;
    }

    private IEnumerator TransitionSequence()
    {
        LockPlayer();
        SnapCameraOrbitToBoss();

        _directorStopped = false;
        _visualsApplied = false;
        phase2Director.stopped -= HandleDirectorStopped;
        phase2Director.stopped += HandleDirectorStopped;
        phase2Director.time = 0d;
        phase2Director.Play();

        float timeout = safetyTimeout;
        double duration = phase2Director.duration;
        if (!double.IsNaN(duration) && !double.IsInfinity(duration) && duration > 0d)
            timeout = Mathf.Max(timeout, (float)duration + 2f);

        float elapsed = 0f;
        while (!_directorStopped && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;

            if (!_visualsApplied && elapsed >= materialSwapTime)
            {
                _visualsApplied = true;
                if (_boss != null)
                    _boss.ApplyPhase2Visuals();
            }

            yield return null;
        }

        phase2Director.stopped -= HandleDirectorStopped;

        if (!_directorStopped)
        {
            Debug.LogWarning(
                "[BossPhaseTransition] Timeline quá thời gian. Tự kết thúc để mở khóa Player.",
                this);
            phase2Director.Stop();
        }

        FinishTransition();
    }

    private void SnapCameraOrbitToBoss()
    {
        if (phase2CameraOrbit == null)
        {
            Debug.LogWarning(
                "[BossPhaseTransition] Chưa gán Phase 2 Camera Orbit; camera sẽ dùng vị trí đặt sẵn trong Scene.",
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
            orbitPosition.y = bossPosition.y + orbitPivotYOffset;

        phase2CameraOrbit.position = orbitPosition;
    }

    private void FinishTransition()
    {
        if (!_running)
            return;

        if (!_visualsApplied)
        {
            _visualsApplied = true;
            if (_boss != null)
                _boss.ApplyPhase2Visuals();
        }

        if (_boss != null)
            _boss.CompletePhase2Transition();

        UnlockPlayer();

        _boss = null;
        _running = false;
    }

    private void HandleDirectorStopped(PlayableDirector director)
    {
        _directorStopped = true;
    }

    private void LockPlayer()
    {
        if (playerScriptsToDisable == null)
            playerScriptsToDisable = new MonoBehaviour[0];
        if (playerObjectsToHide == null)
            playerObjectsToHide = new GameObject[0];

        _previousScriptStates = new bool[playerScriptsToDisable.Length];
        for (int i = 0; i < playerScriptsToDisable.Length; i++)
        {
            MonoBehaviour script = playerScriptsToDisable[i];
            if (script == null || script == this)
                continue;

            _previousScriptStates[i] = script.enabled;
            script.enabled = false;
        }

        _previousObjectStates = new bool[playerObjectsToHide.Length];
        for (int i = 0; i < playerObjectsToHide.Length; i++)
        {
            GameObject target = playerObjectsToHide[i];
            if (target == null)
                continue;

            _previousObjectStates[i] = target.activeSelf;
            target.SetActive(false);
        }
    }

    private void UnlockPlayer()
    {
        if (playerScriptsToDisable != null && _previousScriptStates != null)
        {
            for (int i = 0; i < playerScriptsToDisable.Length; i++)
            {
                MonoBehaviour script = playerScriptsToDisable[i];
                if (script != null && script != this)
                    script.enabled = _previousScriptStates[i];
            }
        }

        if (playerObjectsToHide != null && _previousObjectStates != null)
        {
            for (int i = 0; i < playerObjectsToHide.Length; i++)
            {
                GameObject target = playerObjectsToHide[i];
                if (target != null)
                    target.SetActive(_previousObjectStates[i]);
            }
        }
    }

    private void OnDisable()
    {
        if (!_running)
            return;

        StopAllCoroutines();

        if (phase2Director != null)
            phase2Director.stopped -= HandleDirectorStopped;

        FinishTransition();
    }
}