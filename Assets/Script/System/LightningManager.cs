using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    [Header("Settings")]
    public bool startsWithPowerOff = true;

    [Header("Transition")]
    public float fadeDuration = 1.5f;

    // Lưu trạng thái ban đầu
    private List<Light> managedLights = new List<Light>();
    private List<RendererEmissionData> managedEmissives = new List<RendererEmissionData>();
    private bool isPowerOn = false;

    // Struct lưu thông tin emission gốc
    private struct RendererEmissionData
    {
        public Material mat;
        public Color originalEmissionColor;
    }

    void Start()
    {
        CollectAllLightsAndEmissives();

        if (startsWithPowerOff)
            SetPower(false, instant: true);
    }

    void CollectAllLightsAndEmissives()
    {
        // Thu thập tất cả Light, bỏ qua EmergencyLight
        Light[] allLights = FindObjectsOfType<Light>();
        foreach (Light light in allLights)
        {
            if (!light.CompareTag("EmergencyLight"))
                managedLights.Add(light);
        }

        // Thu thập tất cả Renderer có Emission, bỏ qua EmergencyLight
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        foreach (Renderer rend in allRenderers)
        {
            if (rend.CompareTag("EmergencyLight")) continue;

            foreach (Material mat in rend.materials)
            {
                if (mat.IsKeywordEnabled("_EMISSION"))
                {
                    managedEmissives.Add(new RendererEmissionData
                    {
                        mat = mat,
                        originalEmissionColor = mat.GetColor("_EmissionColor")
                    });
                }
            }
        }

        Debug.Log($"[LightingManager] Tìm thấy {managedLights.Count} đèn, {managedEmissives.Count} emissive materials.");
    }

    // Hàm này gọi khi gạt cần điện
    public void TogglePower()
    {
        SetPower(!isPowerOn);
    }

    // Hoặc gọi trực tiếp
    public void SetPower(bool on, bool instant = false)
    {
        isPowerOn = on;

        if (instant)
        {
            ApplyPowerInstant(on);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FadePower(on));
        }
    }

    void ApplyPowerInstant(bool on)
    {
        foreach (Light light in managedLights)
            light.enabled = on;

        foreach (var data in managedEmissives)
        {
            if (on)
            {
                data.mat.EnableKeyword("_EMISSION");
                data.mat.SetColor("_EmissionColor", data.originalEmissionColor);
            }
            else
            {
                data.mat.SetColor("_EmissionColor", Color.black);
                data.mat.DisableKeyword("_EMISSION");
            }
        }
    }

    IEnumerator FadePower(bool on)
    {
        float elapsed = 0f;

        // Lưu intensity ban đầu của từng đèn
        float[] startIntensities = new float[managedLights.Count];
        for (int i = 0; i < managedLights.Count; i++)
        {
            managedLights[i].enabled = true;
            startIntensities[i] = managedLights[i].intensity;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float factor = on ? t : (1f - t);

            // Fade light intensity
            for (int i = 0; i < managedLights.Count; i++)
            {
                managedLights[i].intensity = startIntensities[i] * factor;
            }

            // Fade emissive
            foreach (var data in managedEmissives)
            {
                data.mat.EnableKeyword("_EMISSION");
                data.mat.SetColor("_EmissionColor", data.originalEmissionColor * factor);
            }

            yield return null;
        }

        // Kết thúc fade
        if (!on)
        {
            foreach (Light light in managedLights)
                light.enabled = false;
        }
    }
}