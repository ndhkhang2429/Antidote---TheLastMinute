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

    [Header("Player Lock")]
    [Tooltip("ThirdPersonController, PlayerAttack và các script input cần khóa.")]
    [SerializeField] private MonoBehaviour[] playerScriptsToDisable;
    [Tooltip("Ví dụ FPS Hands cần ẩn trong cutscene.")]
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
        if (_running || boss == null) return false;

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
                _boss?.ApplyPhase2Visuals();
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

    private void FinishTransition()
    {
        if (!_running) return;

        if (!_visualsApplied)
        {
            _visualsApplied = true;
            _boss?.ApplyPhase2Visuals();
        }

        _boss?.CompletePhase2Transition();
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
            if (script == null || script == this) continue;

            _previousScriptStates[i] = script.enabled;
            script.enabled = false;
        }

        _previousObjectStates = new bool[playerObjectsToHide.Length];
        for (int i = 0; i < playerObjectsToHide.Length; i++)
        {
            GameObject target = playerObjectsToHide[i];
            if (target == null) continue;

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
        if (!_running) return;

        StopAllCoroutines();

        if (phase2Director != null)
            phase2Director.stopped -= HandleDirectorStopped;

        FinishTransition();
    }
}