using UnityEngine;
using System.Collections;

/// <summary>
/// Cutscene Effects Tuner - Điều chỉnh effects realtime trong Play Mode
/// Cho phép tweak values mà không cần recompile
/// Debug tool để test effects nhanh
/// </summary>
public class CutsceneEffectsTuner : MonoBehaviour
{
    [Header("== SCREEN SHAKE TUNING ==")]
    [Range(0.1f, 3f)]
    [SerializeField] private float shakeIntensity = 0.5f;
    [Range(2f, 15f)]
    [SerializeField] private float shakeFrequency = 5f;
    [SerializeField] private bool testShakeSoft = false;
    [SerializeField] private bool testShakeMedium = false;
    [SerializeField] private bool testShakeHard = false;

    [Header("== VIGNETTE TUNING ==")]
    [Range(0f, 1f)]
    [SerializeField] private float vignetteIntensity = 0.3f;
    [SerializeField] private bool setVignette = false;

    [Header("== BLOOM TUNING ==")]
    [Range(0f, 3f)]
    [SerializeField] private float bloomIntensity = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float bloomThreshold = 0.9f;
    [SerializeField] private bool setBloom = false;

    [Header("== FILM GRAIN TUNING ==")]
    [Range(0f, 1f)]
    [SerializeField] private float grainIntensity = 0.2f;
    [SerializeField] private bool setGrain = false;

    [Header("== CHROMATIC ABERRATION ==")]
    [Range(0f, 1f)]
    [SerializeField] private float aberrationIntensity = 0.3f;
    [SerializeField] private bool setAberration = false;

    [Header("== CAMERA TUNING ==")]
    [SerializeField] private Transform targetLookAt;
    [Range(0.1f, 20f)]
    [SerializeField] private float orbitRadius = 5f;
    [Range(0f, 360f)]
    [SerializeField] private float orbitAngle = 45f;
    [Range(0f, 5f)]
    [SerializeField] private float orbitHeight = 2f;
    [SerializeField] private bool updateOrbitCamera = false;

    [Header("== CUTSCENE SIMULATION ==")]
    [SerializeField] private float simulationSpeed = 1f; // 0.5 = slow-mo, 2 = fast
    [SerializeField] private bool simulateCutsceneSequence = false;
    private bool _isSimulating = false;

    [Header("== DEBUG INFO ==")]
    [SerializeField] private bool showDebugUI = true;
    private GUIStyle _debugStyle;

    private void OnGUI()
    {
        if (!showDebugUI) return;

        if (_debugStyle == null)
        {
            _debugStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }

        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Label("=== CUTSCENE EFFECTS TUNER ===", _debugStyle);

        if (ScreenShakeController.Instance != null)
        {
            GUILayout.Label($"Shake Active: {ScreenShakeController.Instance.GetType().Name}");
        }

        if (ScreenEffectsController.Instance != null)
        {
            GUILayout.Label($"Effects Ready: Yes");
        }
        else
        {
            GUILayout.Label("⚠️ Effects NOT FOUND - Create ScreenEffectsController", _debugStyle);
        }

        GUILayout.Label($"Simulation Speed: {simulationSpeed}x");

        GUILayout.EndArea();
    }

    private void Update()
    {
        // Time scale control
        Time.timeScale = simulationSpeed;

        // Screen Shake Tests
        if (testShakeSoft && ScreenShakeController.Instance != null)
        {
            ScreenShakeController.Instance.ShakeSoft();
            testShakeSoft = false;
        }

        if (testShakeMedium && ScreenShakeController.Instance != null)
        {
            ScreenShakeController.Instance.ShakeMedium();
            testShakeMedium = false;
        }

        if (testShakeHard && ScreenShakeController.Instance != null)
        {
            ScreenShakeController.Instance.ShakeHard();
            testShakeHard = false;
        }

        // Direct shake with slider values
        if (Input.GetKeyDown(KeyCode.F1) && ScreenShakeController.Instance != null)
        {
            ScreenShakeController.Instance.Shake(1f, shakeIntensity, shakeFrequency);
        }

        // Vignette
        if (setVignette && ScreenEffectsController.Instance != null)
        {
            ScreenEffectsController.Instance.SetVignette(vignetteIntensity);
        }

        // Bloom
        if (setBloom && ScreenEffectsController.Instance != null)
        {
            ScreenEffectsController.Instance.SetBloom(bloomIntensity, bloomThreshold);
        }

        // Film Grain
        if (setGrain && ScreenEffectsController.Instance != null)
        {
            ScreenEffectsController.Instance.SetFilmGrain(grainIntensity);
        }

        // Chromatic Aberration
        if (setAberration && ScreenEffectsController.Instance != null)
        {
            ScreenEffectsController.Instance.SetChromaticAberration(aberrationIntensity);
        }

        // Update orbit camera
        if (updateOrbitCamera && targetLookAt != null && CutsceneCameraController.Instance != null)
        {
            UpdateOrbitCamera();
        }

        // Simulate full cutscene
        if (simulateCutsceneSequence && !_isSimulating)
        {
            StartCoroutine(SimulateCutsceneEffects());
        }
    }

    private void UpdateOrbitCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // Calculate orbit position
        float x = targetLookAt.position.x + Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        float z = targetLookAt.position.z + Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        Vector3 newPos = new Vector3(x, targetLookAt.position.y + orbitHeight, z);

        mainCam.transform.position = newPos;
        mainCam.transform.LookAt(targetLookAt.position + Vector3.up * (orbitHeight * 0.5f));
    }

    private IEnumerator SimulateCutsceneEffects()
    {
        _isSimulating = true;

        // Simulate Act 1
        Debug.Log("[Tuner] Simulating Act 1 - Dread Building");
        for (int i = 0; i < 50; i++)
        {
            float t = i / 50f;
            ScreenEffectsController.Instance.SetVignette(t * 0.25f);
            ScreenEffectsController.Instance.SetFilmGrain(t * 0.15f);
            yield return new WaitForSeconds(0.02f);
        }

        yield return new WaitForSeconds(1f);

        // Simulate Act 2
        Debug.Log("[Tuner] Simulating Act 2 - Awakening");
        ScreenShakeController.Instance.Shake(1f, 0.3f, 5f);
        yield return new WaitForSeconds(1.5f);

        // Simulate Act 3
        Debug.Log("[Tuner] Simulating Act 3 - Transformation");
        yield return StartCoroutine(ScreenEffectsController.Instance.ChaosEffect(2f));
        ScreenShakeController.Instance.Shake(1.5f, 0.8f, 12f);
        yield return new WaitForSeconds(1.5f);

        // Simulate Act 4
        Debug.Log("[Tuner] Simulating Act 4 - Dominance");
        for (int i = 0; i < 30; i++)
        {
            float t = i / 30f;
            ScreenEffectsController.Instance.SetVignette((1 - t) * 0.35f);
            yield return new WaitForSeconds(0.02f);
        }

        ScreenEffectsController.Instance.ResetAllEffects();
        _isSimulating = false;
        simulateCutsceneSequence = false;

        Debug.Log("[Tuner] Simulation complete!");
    }

    /// <summary>
    /// Reset all effects (keyboard shortcut: R)
    /// </summary>
    private void ResetEffectsShortcut()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ScreenEffectsController.Instance.ResetAllEffects();
            ScreenShakeController.Instance.StopShake();
            Debug.Log("[Tuner] All effects reset!");
        }
    }

    // ============ KEYBOARD SHORTCUTS ============

    public void PrintShortcuts()
    {
        Debug.Log(
            "=== CUTSCENE EFFECTS TUNER SHORTCUTS ===\n" +
            "F1: Test shake with slider values\n" +
            "R: Reset all effects\n" +
            "Check Inspector for more tuning options"
        );
    }
}