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
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private string gameOverSceneName = "GameOver";

    private bool isDead = false;

    private void OnEnable() => healthSystem.OnDeath += HandleDeath;
    private void OnDisable() => healthSystem.OnDeath -= HandleDeath;

    private void HandleDeath()
    {
        if (isDead) return; // tránh g?i 2 l?n n?u OnDeath fire nhi?u l?n
        isDead = true;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        fpsController.enabled = false;
        equipmentManager.enabled = false;

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
}