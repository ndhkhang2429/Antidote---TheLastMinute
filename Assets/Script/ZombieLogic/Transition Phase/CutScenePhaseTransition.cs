using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// CUTSCENE PHASE TRANSITION - Main orchestrator
/// Quản lý toàn bộ fase chuyển đổi từ Phase 1 → Phase 2 của Boss Zombie
/// 
/// Thiết kế: Gồm 4 ACT, mỗi ACT có camera, VFX, audio, timing riêng
/// Tích hợp: ScreenShakeController, ScreenEffectsController
/// </summary>
public class CutscenePhaseTransition : MonoBehaviour
{
    private MutatedBossZombie _bossZombie;
    private Transform _bossTransform;
    private Animator _bossAnimator;
    private NavMeshAgent _bossAgent;
    private SkinnedMeshRenderer _bossSkinMesh;
    private HealthSystem _bossHealth;

    [Header("== BOSS SETUP ==")]
    [SerializeField] private GameObject cutsceneCamera;

    [Header("== CUTSCENE CAMERA ==")]
    [SerializeField] private float orbitRadius = 5f;
    [SerializeField] private float closeUpDistance = 1.5f;
    [SerializeField] private float dramaticDistance = 6f;

    [Header("== TIMING (Giây) ==")]
    [SerializeField] private float actOneStart = 0f;
    [SerializeField] private float actTwoDuration = 2.5f;
    [SerializeField] private float actThreeDuration = 4f;
    [SerializeField] private float actFourDuration = 2f;

    [Header("== VFX PREFABS ==")]
    [SerializeField] private GameObject phase2RoarVfxPrefab;
    [SerializeField] private GameObject groundCrackVfxPrefab;

    [Header("== TRANSFORMATION SETTINGS ==")]
    [SerializeField] private Material materialV3;
    [SerializeField] private float bossScaleMultiplier = 1.3f;
    [SerializeField] private float postCutsceneAnimSpeed = 1.25f;

    [Header("== AUDIO (Optional) ==")]
    [SerializeField] private AudioClip roarAudio;
    [SerializeField] private AudioClip transformationAmbience;

    // State tracking
    private bool _cutsceneActive = false;
    private Vector3 _bossOriginalScale;
    private Camera _cutsceneMainCamera;

    public void Initialize(MutatedBossZombie boss, Transform bossTransform, Animator animator,
        NavMeshAgent agent, SkinnedMeshRenderer skinMesh, HealthSystem health)
    {
        _bossZombie = boss;
        _bossTransform = bossTransform;
        _bossAnimator = animator;
        _bossAgent = agent;
        _bossSkinMesh = skinMesh;
        _bossHealth = health;
        _bossOriginalScale = bossTransform.localScale;
    }

    public void StartPhaseTransitionCutscene()
    {
        if (_cutsceneActive) return;
        StartCoroutine(CutsceneSequence());
    }

    private IEnumerator CutsceneSequence()
    {
        _cutsceneActive = true;

        // Activate cutscene camera & get reference
        if (cutsceneCamera != null)
        {
            cutsceneCamera.SetActive(true);
            _cutsceneMainCamera = cutsceneCamera.GetComponent<Camera>();
            if (_cutsceneMainCamera == null)
            {
                Debug.LogError("[Cutscene] CutsceneCamera has no Camera component!");
                _cutsceneActive = false;
                yield break;
            }
        }
        else
        {
            Debug.LogError("[Cutscene] CutsceneCamera GameObject not assigned!");
            _cutsceneActive = false;
            yield break;
        }

        // Lock combat
        _bossAgent.enabled = false;
        _bossAnimator.speed = 1f;

        // === ACT 1: FALSE VICTORY (0-2.5s) ===
        yield return StartCoroutine(ActOne());

        // === ACT 2: THE AWAKENING (2.5-5s) ===
        yield return StartCoroutine(ActTwo());

        // === ACT 3: THE METAMORPHOSIS (5-9s) ===
        yield return StartCoroutine(ActThree());

        // === ACT 4: DOMINANCE ESTABLISHED (9-11s) ===
        yield return StartCoroutine(ActFour());

        // === RETURN TO GAMEPLAY ===
        yield return StartCoroutine(ReturnToGameplay());

        _cutsceneActive = false;
    }

    // ============ ACT 1: FALSE VICTORY (0-2.5s) ============
    // Boss collapses, camera orbits slowly, dread builds

    private IEnumerator ActOne()
    {
        // Boss collapses
        _bossAnimator.SetTrigger("DieTrigger");

        // Slow camera drift (orbit around boss)
        float orbitStart = 0f;
        float orbitEnd = 45f;
        float duration = 2.5f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Smooth orbit around boss
            float angle = Mathf.Lerp(orbitStart, orbitEnd, t);
            float x = _bossTransform.position.x + Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius;
            float z = _bossTransform.position.z + Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius;
            Vector3 camPos = new Vector3(x, _bossTransform.position.y + 2f, z);

            _cutsceneMainCamera.transform.position = camPos;
            _cutsceneMainCamera.transform.LookAt(_bossTransform.position + Vector3.up);

            // Screen effects: subtle dread building
            float dreadIntensity = t * 0.15f;
            ScreenEffectsController.Instance.SetVignette(dreadIntensity);
            ScreenEffectsController.Instance.SetFilmGrain(dreadIntensity * 0.8f);

            yield return null;
        }
    }

    // ============ ACT 2: THE AWAKENING (2.5-5s) ============
    // Boss wakes up, stands up, rotates (animation handles rotation)

    private IEnumerator ActTwo()
    {
        // Boss starts moving - stand up trigger
        _bossAnimator.SetTrigger("StandUpTrigger");

        // Camera: zoom to shoulder close-up
        Vector3 shoulderOffset = _bossTransform.position + _bossTransform.right * 0.8f + Vector3.up * 1.5f;
        Vector3 camCloseUp = shoulderOffset - _bossTransform.forward * 1.5f;

        // Quick zoom to close-up
        float zoomDuration = 0.5f;
        float zoomElapsed = 0f;

        while (zoomElapsed < zoomDuration)
        {
            zoomElapsed += Time.deltaTime;
            float t = zoomElapsed / zoomDuration;

            _cutsceneMainCamera.transform.position = Vector3.Lerp(_cutsceneMainCamera.transform.position, camCloseUp, t);
            _cutsceneMainCamera.transform.LookAt(shoulderOffset);

            yield return null;
        }

        // Shake slightly as boss moves
        ScreenShakeController.Instance.Shake(1f, 0.3f, 5f);

        // Dread effects increase
        float vignetteStart = 0.15f;
        float vignetteEnd = 0.35f;
        float vignetteDuration = 1.5f;
        float vignetteElapsed = 0f;

        while (vignetteElapsed < vignetteDuration)
        {
            vignetteElapsed += Time.deltaTime;
            float t = vignetteElapsed / vignetteDuration;

            float vignetteValue = Mathf.Lerp(vignetteStart, vignetteEnd, t);
            ScreenEffectsController.Instance.SetVignette(vignetteValue);

            yield return null;
        }

        // Camera moves to dramatic side profile (animation handles boss rotation)
        float cameraDuration = 1.0f;
        float cameraElapsed = 0f;

        while (cameraElapsed < cameraDuration)
        {
            cameraElapsed += Time.deltaTime;
            float t = cameraElapsed / cameraDuration;

            // Camera moves to side profile while boss animation plays
            Vector3 sidePos = _bossTransform.position + _bossTransform.right * 3f + Vector3.up * 2f;
            _cutsceneMainCamera.transform.position = Vector3.Lerp(_cutsceneMainCamera.transform.position, sidePos, t);
            _cutsceneMainCamera.transform.LookAt(_bossTransform.position + Vector3.up * 1.2f);

            // Effects intensify
            ScreenEffectsController.Instance.SetFilmGrain(0.25f + (t * 0.15f));

            yield return null;
        }

        // Eyes glow moment
        yield return new WaitForSeconds(0.5f);

        // Quick shake as boss looks at player
        ScreenShakeController.Instance.Shake(0.3f, 0.4f, 8f);
        ScreenEffectsController.Instance.SetBloom(1f);

        yield return new WaitForSeconds(0.5f);
    }

    // ============ ACT 3: THE METAMORPHOSIS (5-9s) ============
    // Boss roars, material swaps, scales up, VFX bursts

    private IEnumerator ActThree()
    {
        // Roar trigger
        _bossAnimator.SetTrigger("RoarTransition");

        // VFX BURST - roar VFX appears
        if (phase2RoarVfxPrefab != null)
        {
            Vector3 vfxPos = _bossTransform.position + Vector3.up * 1.5f;
            GameObject roarVfx = Instantiate(phase2RoarVfxPrefab, vfxPos, Quaternion.identity);
            Destroy(roarVfx, 5f);
        }

        // CHAOS PHASE - Heavy screen effects (2 seconds)
        yield return StartCoroutine(ChaosEffect(2f));

        // Camera: Pull back + dramatic angle
        float dollyDuration = 2f;
        float dollyElapsed = 0f;
        Vector3 dollyStartPos = _cutsceneMainCamera.transform.position;
        Vector3 dollyEndPos = _bossTransform.position - _bossTransform.forward * 6f + Vector3.up * 3f;

        while (dollyElapsed < dollyDuration)
        {
            dollyElapsed += Time.deltaTime;
            float t = dollyElapsed / dollyDuration;

            _cutsceneMainCamera.transform.position = Vector3.Lerp(dollyStartPos, dollyEndPos, t);
            _cutsceneMainCamera.transform.LookAt(_bossTransform.position + Vector3.up * 1.5f);

            yield return null;
        }

        // Screen shake during transformation
        ScreenShakeController.Instance.Shake(1.5f, 0.8f, 12f);

        // MATERIAL SWAP after 0.3s (hidden by VFX chaos)
        yield return new WaitForSeconds(0.3f);
        if (_bossSkinMesh != null && materialV3 != null)
        {
            _bossSkinMesh.sharedMaterial = materialV3;
            Debug.Log("[Cutscene] Material swapped to V3 (mutated)!");
        }

        // SCALE UP - Boss grows bigger
        Vector3 targetScale = _bossOriginalScale * bossScaleMultiplier;
        float scaleDuration = 1.5f;
        float scaleElapsed = 0f;

        while (scaleElapsed < scaleDuration)
        {
            scaleElapsed += Time.deltaTime;
            float t = scaleElapsed / scaleDuration;
            _bossTransform.localScale = Vector3.Lerp(_bossOriginalScale, targetScale, t);
            yield return null;
        }

        _bossTransform.localScale = targetScale;

        // VFX: Ground crack appears
        if (groundCrackVfxPrefab != null)
        {
            Vector3 crackPos = _bossTransform.position + Vector3.up * 0.1f;
            Quaternion crackRot = Quaternion.Euler(-90, 0, 0);
            GameObject crack = Instantiate(groundCrackVfxPrefab, crackPos, crackRot);
            Destroy(crack, 10f);
        }

        // Final roar moment - hard shake
        yield return new WaitForSeconds(0.5f);
        ScreenShakeController.Instance.Shake(0.8f, 1f, 10f);
    }

    // ============ ACT 4: DOMINANCE ESTABLISHED (9-11s) ============
    // Boss takes powerful stance, effects fade

    private IEnumerator ActFour()
    {
        float duration = 2f;

        // Boss takes dominant stance
        _bossAnimator.SetFloat("Speed", 0f); // Idle

        // Camera: Slow pan to powerful side profile
        Vector3 powerPos = _bossTransform.position + _bossTransform.right * 4f + Vector3.up * 1.5f;
        float panDuration = 1f;
        float panElapsed = 0f;
        Vector3 panStartPos = _cutsceneMainCamera.transform.position;

        while (panElapsed < panDuration)
        {
            panElapsed += Time.deltaTime;
            float t = panElapsed / panDuration;

            _cutsceneMainCamera.transform.position = Vector3.Lerp(panStartPos, powerPos, t);
            _cutsceneMainCamera.transform.LookAt(_bossTransform.position + Vector3.up * 1.2f);

            yield return null;
        }

        // Effects fade out
        float vignetteStart = 0.35f;
        float vignetteEnd = 0f;
        float vignetteFadeDuration = 0.8f;
        float vignetteElapsed = 0f;

        while (vignetteElapsed < vignetteFadeDuration)
        {
            vignetteElapsed += Time.deltaTime;
            float t = vignetteElapsed / vignetteFadeDuration;

            float vignetteValue = Mathf.Lerp(vignetteStart, vignetteEnd, t);
            ScreenEffectsController.Instance.SetVignette(vignetteValue);

            yield return null;
        }

        ScreenEffectsController.Instance.SetFilmGrain(0f);
        ScreenEffectsController.Instance.SetBloom(0.3f); // Subtle glow on transformed boss

        yield return new WaitForSeconds(duration);
    }

    // ============ RETURN TO GAMEPLAY ============
    // Cleanup, restore control, boss enters Phase 2

    private IEnumerator ReturnToGameplay()
    {
        // Fade out cutscene camera
        if (cutsceneCamera != null)
            cutsceneCamera.SetActive(false);

        // Reset all screen effects
        ScreenEffectsController.Instance.ResetAllEffects();

        // Re-enable NavMeshAgent
        if (!_bossAgent.enabled)
            _bossAgent.enabled = true;

        if (_bossAgent.isActiveAndEnabled && _bossAgent.isOnNavMesh)
        {
            _bossAgent.Warp(_bossTransform.position);
            _bossAgent.isStopped = false;
        }

        // Set phase 2 animation speed (faster, more aggressive)
        _bossAnimator.speed = postCutsceneAnimSpeed;

        // Callback to boss to enter Phase 2 combat
        _bossZombie.ResetToNormalCombatState();

        yield return null;
    }

    // ============ HELPER METHODS ============

    private IEnumerator ChaosEffect(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Heavy effects during chaos
            ScreenEffectsController.Instance.SetVignette(0.5f + (Mathf.Sin(elapsed * 10f) * 0.2f));
            ScreenEffectsController.Instance.SetFilmGrain(0.5f + (Mathf.Sin(elapsed * 8f) * 0.3f));
            ScreenEffectsController.Instance.SetBloom(1f + (Mathf.Sin(elapsed * 6f) * 0.5f));

            yield return null;
        }
    }

    // ============ UTILITY ============

    public bool IsCutsceneActive => _cutsceneActive;
}