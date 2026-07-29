using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private FirstPersonController fpsController;
    [SerializeField] private PlayerEquipmentManager equipmentManager;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Death Fall")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip deathGruntSound;
    [SerializeField] private AudioClip fallThudSound;
    [SerializeField] private float fallDuration = 0.6f;
    [SerializeField] private float fallCameraDropHeight = 0.9f;
    [SerializeField] private float fallTiltAngle = 75f;
    [SerializeField] private float holdAfterFall = 1f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private string gameOverSceneName = "GameOver";

    private bool isDead = false;

    private void OnEnable() => healthSystem.OnDeath += HandleDeath;
    private void OnDisable() => healthSystem.OnDeath -= HandleDeath;

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        fpsController.enabled = false;
        equipmentManager.enabled = false;

        if (deathGruntSound != null)
            deathAudioSource.PlayOneShot(deathGruntSound);

        yield return StartCoroutine(FallCamera());

        yield return new WaitForSeconds(holdAfterFall);

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = 0f;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        SceneManager.LoadScene(gameOverSceneName);
    }

    private IEnumerator FallCamera()
    {
        Vector3 startPos = cameraTransform.localPosition;
        Vector3 endPos = startPos + new Vector3(0f, -fallCameraDropHeight, 0f);

        Quaternion startRot = cameraTransform.localRotation;
        float side = Random.value > 0.5f ? 1f : -1f;
        Quaternion endRot = startRot * Quaternion.Euler(-15f, 0f, fallTiltAngle * side);

        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float p = t / fallDuration;
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            cameraTransform.localPosition = Vector3.Lerp(startPos, endPos, eased);
            cameraTransform.localRotation = Quaternion.Slerp(startRot, endRot, eased);
            yield return null;
        }

        cameraTransform.localPosition = endPos;
        cameraTransform.localRotation = endRot;

        if (fallThudSound != null)
            deathAudioSource.PlayOneShot(fallThudSound);

        yield return StartCoroutine(CameraShake(0.15f, 0.05f));
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        Vector3 basePos = cameraTransform.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float damper = 1f - (t / duration);
            cameraTransform.localPosition = basePos + Random.insideUnitSphere * magnitude * damper;
            yield return null;
        }
        cameraTransform.localPosition = basePos;
    }
}