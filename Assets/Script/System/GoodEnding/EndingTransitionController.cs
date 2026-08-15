using System.Collections;
using UnityEngine;

public class EndingTransitionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EndingFadeController fadeController;
    [SerializeField] private Transform player;
    [SerializeField] private Transform rooftopSpawnPoint;

    [Header("Boss")]
    [Tooltip("Kéo HealthSystem của boss vào đây.")]
    [SerializeField] private HealthSystem bossHealthSystem;

    [Tooltip(
        "Thời gian chờ sau khi boss chết để animation chết " +
        "được phát trước khi Fade Out."
    )]
    [SerializeField] private float bossDeathDelay = 2f;

    [Header("Player Lock During Fade")]
    [Tooltip(
        "Các script điều khiển cần khóa trong lúc Fade và Teleport, " +
        "ví dụ FirstPersonController, PlayerAttack, PlayerInteraction."
    )]
    [SerializeField]
    private MonoBehaviour[] playerScriptsToDisable;

    [Header("Transition Settings")]
    [SerializeField] private float fadeOutDuration = 1.2f;
    [SerializeField] private float blackScreenHoldTime = 0.5f;
    [SerializeField] private float fadeInDuration = 1.2f;

    [Header("Boss Music (Optional)")]
    [Tooltip(
        "AudioSource phát nhạc boss. Có thể để trống nếu hệ thống khác " +
        "đã tự Fade Out nhạc."
    )]
    [SerializeField] private AudioSource bossMusicSource;

    [SerializeField] private float bossMusicFadeDuration = 2f;

    [Header("Debug")]
    [Tooltip("Cho phép nhấn F7 để test chuyển lên rooftop.")]
    [SerializeField] private bool enableF7Cheat = true;

    private bool[] _previousScriptStates;

    private bool _isTransitioning;
    private bool _bossDeathReceived;

    public bool IsTransitioning => _isTransitioning;

    // ─────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────

    private void OnEnable()
    {
        SubscribeToBossDeath();
    }

    private void Start()
    {
        /*
         * Gọi lại để dự phòng trường hợp Boss HealthSystem
         * được gán hoặc kích hoạt sau OnEnable.
         */
        SubscribeToBossDeath();
    }

    private void OnDisable()
    {
        UnsubscribeFromBossDeath();

        /*
         * Tránh để Player bị khóa nếu object này bị tắt
         * giữa quá trình chuyển cảnh.
         */
        if (_isTransitioning)
        {
            StopAllCoroutines();
            UnlockPlayer();
            _isTransitioning = false;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromBossDeath();
    }

    private void Update()
    {
        if (!enableF7Cheat)
            return;

        if (Input.GetKeyDown(KeyCode.F7))
        {
            Debug.Log(
                "[EndingTransition] F7 pressed. " +
                "Starting rooftop transition."
            );

            /*
             * Cheat bỏ qua thời gian chờ animation chết.
             */
            StartRooftopTransition();
        }
    }

    // ─────────────────────────────────────────────────────
    // Boss death
    // ─────────────────────────────────────────────────────

    private void SubscribeToBossDeath()
    {
        if (bossHealthSystem == null)
            return;

        /*
         * Trừ trước rồi cộng lại để tránh đăng ký trùng.
         */
        bossHealthSystem.OnDeath -= HandleBossDeath;
        bossHealthSystem.OnDeath += HandleBossDeath;
    }

    private void UnsubscribeFromBossDeath()
    {
        if (bossHealthSystem != null)
        {
            bossHealthSystem.OnDeath -= HandleBossDeath;
        }
    }

    private void HandleBossDeath()
    {
        if (_bossDeathReceived || _isTransitioning)
            return;

        _bossDeathReceived = true;

        Debug.Log(
            "[EndingTransition] Boss defeated. " +
            "Waiting for death animation."
        );

        StartCoroutine(
            BossDeathTransitionRoutine()
        );
    }

    private IEnumerator BossDeathTransitionRoutine()
    {
        /*
         * Cho boss đủ thời gian phát animation chết.
         */
        float delay = Mathf.Max(0f, bossDeathDelay);

        if (bossMusicSource != null &&
            bossMusicSource.isPlaying)
        {
            StartCoroutine(
                FadeOutBossMusic(
                    bossMusicFadeDuration
                )
            );
        }

        yield return new WaitForSecondsRealtime(delay);

        StartRooftopTransition();
    }

    // ─────────────────────────────────────────────────────
    // Public transition
    // ─────────────────────────────────────────────────────

    public void StartRooftopTransition()
    {
        if (_isTransitioning)
        {
            Debug.LogWarning(
                "[EndingTransition] Transition already running."
            );

            return;
        }

        if (!ValidateReferences())
            return;

        _isTransitioning = true;

        StartCoroutine(
            RooftopTransitionRoutine()
        );
    }

    private bool ValidateReferences()
    {
        if (fadeController == null)
        {
            Debug.LogError(
                "[EndingTransition] FadeController is NULL!",
                this
            );

            return false;
        }

        if (player == null)
        {
            Debug.LogError(
                "[EndingTransition] Player is NULL!",
                this
            );

            return false;
        }

        if (rooftopSpawnPoint == null)
        {
            Debug.LogError(
                "[EndingTransition] RooftopSpawnPoint is NULL!",
                this
            );

            return false;
        }

        return true;
    }

    // ─────────────────────────────────────────────────────
    // Transition sequence
    // ─────────────────────────────────────────────────────

    private IEnumerator RooftopTransitionRoutine()
    {
        Debug.Log(
            "[EndingTransition] Rooftop transition started."
        );

        /*
         * Chỉ khóa Player khi màn hình bắt đầu Fade.
         * Trong khoảng bossDeathDelay, Player vẫn có thể
         * quan sát animation cuối của boss.
         */
        LockPlayer();

        // 1. Fade sang đen
        fadeController.FadeOut(fadeOutDuration);

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, fadeOutDuration) + 0.1f
        );

        // 2. Teleport khi màn hình đã đen
        TeleportPlayerToRooftop();

        // Xóa prompt tương tác còn sót lại
        InteractionUIManager.Instance?.HidePrompt();

        Art_Equilibrium.AE_Door.ClearAllDoorPrompts();

        // Chờ ngắn trên màn hình đen
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, blackScreenHoldTime)
        );

        // 3. Fade sáng trên rooftop
        fadeController.FadeIn(fadeInDuration);

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, fadeInDuration) + 0.1f
        );

        // 4. Trả quyền điều khiển
        UnlockPlayer();

        _isTransitioning = false;

        Debug.Log(
            "[EndingTransition] Rooftop transition complete."
        );
    }

    // ─────────────────────────────────────────────────────
    // Teleport
    // ─────────────────────────────────────────────────────

    private void TeleportPlayerToRooftop()
    {
        CharacterController characterController =
            player.GetComponent<CharacterController>();

        bool controllerWasEnabled =
            characterController != null &&
            characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        player.SetPositionAndRotation(
            rooftopSpawnPoint.position,
            rooftopSpawnPoint.rotation
        );

        Physics.SyncTransforms();

        if (controllerWasEnabled)
            characterController.enabled = true;

        Debug.Log(
            "[EndingTransition] Player teleported to rooftop: " +
            player.position
        );
    }

    // ─────────────────────────────────────────────────────
    // Player lock
    // ─────────────────────────────────────────────────────

    private void LockPlayer()
    {
        if (playerScriptsToDisable == null)
        {
            playerScriptsToDisable =
                new MonoBehaviour[0];
        }

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
    }

    private void UnlockPlayer()
    {
        if (playerScriptsToDisable == null ||
            _previousScriptStates == null)
        {
            return;
        }

        int count = Mathf.Min(
            playerScriptsToDisable.Length,
            _previousScriptStates.Length
        );

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

    // ─────────────────────────────────────────────────────
    // Boss music
    // ─────────────────────────────────────────────────────

    private IEnumerator FadeOutBossMusic(
        float duration)
    {
        if (bossMusicSource == null)
            yield break;

        float startVolume =
            bossMusicSource.volume;

        float fadeDuration =
            Mathf.Max(0.01f, duration);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );

            bossMusicSource.volume =
                Mathf.Lerp(
                    startVolume,
                    0f,
                    progress
                );

            yield return null;
        }

        bossMusicSource.volume = 0f;
        bossMusicSource.Stop();

        /*
         * Khôi phục volume để AudioSource vẫn dùng được
         * nếu Scene được reset hoặc chơi lại trong Editor.
         */
        bossMusicSource.volume = startVolume;
    }
}