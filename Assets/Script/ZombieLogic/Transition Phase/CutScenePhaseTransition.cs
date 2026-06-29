using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CutscenePhaseTransition : MonoBehaviour
{
    private MutatedBossZombie _bossZombie;
    private Transform _bossTransform;
    private Animator _bossAnimator;
    private NavMeshAgent _bossAgent;
    private SkinnedMeshRenderer _bossSkinMesh;
    private HealthSystem _bossHealth;
    private Camera _cutsceneCamera;

    [Header("== BOSS SETUP ==")]
    [SerializeField] private GameObject cutsceneCamera;

    [Header("== CUTSCENE CAMERA ==")]
    [SerializeField] private float orbitRadius = 5f;

    [Header("== VFX PREFABS ==")]
    [SerializeField] private GameObject phase2RoarVfxPrefab;
    [SerializeField] private GameObject groundCrackVfxPrefab;

    [Header("== TRANSFORMATION SETTINGS ==")]
    [SerializeField] private Material materialV3;
    [SerializeField] private float bossScaleMultiplier = 1.3f;

    private bool _cutsceneActive = false;
    private Vector3 _bossOriginalScale;

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

        // === SETUP ===
        if (cutsceneCamera == null)
        {
            Debug.LogError("[Cutscene] CutsceneCamera not assigned!");
            _cutsceneActive = false;
            yield break;
        }

        cutsceneCamera.SetActive(true);
        _cutsceneCamera = cutsceneCamera.GetComponent<Camera>();

        if (_cutsceneCamera == null)
        {
            Debug.LogError("[Cutscene] CutsceneCamera has NO Camera component!");
            _cutsceneActive = false;
            yield break;
        }

        Debug.Log("[Cutscene] CutsceneCamera enabled! Starting cutscene...");

        _bossAgent.enabled = false;
        _bossAnimator.speed = 1f;

        // === ACT 1: ORBIT AROUND BOSS (3 seconds) ===
        yield return StartCoroutine(ActOne_Orbit());

        // === ACT 2: STAND UP ===
        yield return StartCoroutine(ActTwo_StandUp());

        // === ACT 3: ROAR + TRANSFORM ===
        yield return StartCoroutine(ActThree_Transform());

        // === RETURN ===
        yield return StartCoroutine(ReturnToGameplay());

        _cutsceneActive = false;
    }

    private IEnumerator ActOne_Orbit()
    {
        _bossAnimator.SetTrigger("DieTrigger");

        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Orbit around boss
            float angle = t * 90f; // 0 → 90 degrees
            float x = _bossTransform.position.x + Mathf.Cos(angle * Mathf.Deg2Rad) * orbitRadius;
            float z = _bossTransform.position.z + Mathf.Sin(angle * Mathf.Deg2Rad) * orbitRadius;
            Vector3 camPos = new Vector3(x, _bossTransform.position.y + 2f, z);

            _cutsceneCamera.transform.position = camPos;
            _cutsceneCamera.transform.LookAt(_bossTransform.position + Vector3.up);

            Debug.Log("[Cutscene] Camera orbiting: angle=" + angle.ToString("F1"));

            yield return null;
        }
    }

    private IEnumerator ActTwo_StandUp()
    {
        _bossAnimator.SetTrigger("StandUpTrigger");
        yield return new WaitForSeconds(2f);
    }

    private IEnumerator ActThree_Transform()
    {
        _bossAnimator.SetTrigger("RoarTransition");

        // VFX
        if (phase2RoarVfxPrefab != null)
        {
            Vector3 vfxPos = _bossTransform.position + Vector3.up * 1.5f;
            GameObject roarVfx = Instantiate(phase2RoarVfxPrefab, vfxPos, Quaternion.identity);
            Destroy(roarVfx, 5f);
        }

        // Material swap
        yield return new WaitForSeconds(0.5f);
        if (_bossSkinMesh != null && materialV3 != null)
        {
            _bossSkinMesh.sharedMaterial = materialV3;
            Debug.Log("[Cutscene] Material swapped!");
        }

        // Scale up
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

        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator ReturnToGameplay()
    {
        // Disable cutscene camera
        if (cutsceneCamera != null)
            cutsceneCamera.SetActive(false);

        Debug.Log("[Cutscene] Cutscene ended, returning to gameplay!");

        // Re-enable boss combat
        if (!_bossAgent.enabled)
            _bossAgent.enabled = true;

        if (_bossAgent.isActiveAndEnabled && _bossAgent.isOnNavMesh)
        {
            _bossAgent.Warp(_bossTransform.position);
            _bossAgent.isStopped = false;
        }

        _bossAnimator.speed = 1.25f;
        _bossZombie.ResetToNormalCombatState();

        yield return null;
    }

    public bool IsCutsceneActive => _cutsceneActive;
}