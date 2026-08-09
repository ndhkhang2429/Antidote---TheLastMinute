using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    [Header("Settings")]
    public bool startsWithPowerOff = true;

    [Header("Transition")]
    [Min(0.01f)]
    public float fadeDuration = 1.5f;

    [Header("Exceptions")]
    [Tooltip("Kéo các Renderer không bị ảnh hưởng bởi nguồn điện vào đây.")]
    public List<Renderer> ignoreRenderers =
        new List<Renderer>();

    public static LightingManager Instance
    {
        get;
        private set;
    }

    public bool IsPowerOn => isPowerOn;

    private readonly List<ManagedLightData>
        managedLights =
            new List<ManagedLightData>();

    private readonly List<RendererEmissionData>
        managedEmissives =
            new List<RendererEmissionData>();

    private bool isPowerOn;

    private struct ManagedLightData
    {
        public Light light;
        public float originalIntensity;
    }

    private struct RendererEmissionData
    {
        public Material material;
        public Color originalEmissionColor;
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CollectAllLightsAndEmissives();

        if (startsWithPowerOff)
        {
            SetPower(false, instant: true);
        }
        else
        {
            SetPower(true, instant: true);
        }
    }

    private void CollectAllLightsAndEmissives()
    {
        managedLights.Clear();
        managedEmissives.Clear();

        Light[] allLights =
            FindObjectsOfType<Light>();

        foreach (Light lightComponent in allLights)
        {
            if (lightComponent == null ||
                lightComponent.CompareTag(
                    "EmergencyLight"
                ))
            {
                continue;
            }

            managedLights.Add(
                new ManagedLightData
                {
                    light = lightComponent,
                    originalIntensity =
                        lightComponent.intensity
                }
            );
        }

        Renderer[] allRenderers =
            FindObjectsOfType<Renderer>();

        foreach (Renderer rend in allRenderers)
        {
            if (rend == null ||
                rend.CompareTag("EmergencyLight") ||
                ignoreRenderers.Contains(rend))
            {
                continue;
            }

            foreach (Material material in
                     rend.materials)
            {
                if (material == null ||
                    !material.HasProperty(
                        "_EmissionColor"
                    ))
                {
                    continue;
                }

                if (!material.IsKeywordEnabled(
                        "_EMISSION"
                    ))
                {
                    continue;
                }

                managedEmissives.Add(
                    new RendererEmissionData
                    {
                        material = material,
                        originalEmissionColor =
                            material.GetColor(
                                "_EmissionColor"
                            )
                    }
                );
            }
        }

        Debug.Log(
            $"[LightingManager] Tìm thấy " +
            $"{managedLights.Count} đèn và " +
            $"{managedEmissives.Count} emissive materials."
        );
    }

    public void TogglePower()
    {
        SetPower(!isPowerOn);
    }

    public void SetPower(
        bool on,
        bool instant = false)
    {
        isPowerOn = on;

        StopAllCoroutines();

        if (instant)
        {
            ApplyPowerInstant(on);
        }
        else
        {
            StartCoroutine(
                FadePower(on)
            );
        }
    }

    private void ApplyPowerInstant(bool on)
    {
        foreach (ManagedLightData data in
                 managedLights)
        {
            if (data.light == null)
            {
                continue;
            }

            data.light.intensity =
                on ? data.originalIntensity : 0f;

            data.light.enabled = on;
        }

        foreach (RendererEmissionData data in
                 managedEmissives)
        {
            if (data.material == null)
            {
                continue;
            }

            if (on)
            {
                data.material.EnableKeyword(
                    "_EMISSION"
                );

                data.material.SetColor(
                    "_EmissionColor",
                    data.originalEmissionColor
                );
            }
            else
            {
                data.material.SetColor(
                    "_EmissionColor",
                    Color.black
                );

                data.material.DisableKeyword(
                    "_EMISSION"
                );
            }
        }
    }

    private IEnumerator FadePower(bool on)
    {
        float safeDuration =
            Mathf.Max(0.01f, fadeDuration);

        float elapsed = 0f;

        float[] startingIntensities =
            new float[managedLights.Count];

        Color[] startingEmissionColors =
            new Color[managedEmissives.Count];

        for (int i = 0;
             i < managedLights.Count;
             i++)
        {
            ManagedLightData data =
                managedLights[i];

            if (data.light == null)
            {
                continue;
            }

            startingIntensities[i] =
                data.light.intensity;

            // Phải bật component trước khi fade sáng.
            data.light.enabled = true;
        }

        for (int i = 0;
             i < managedEmissives.Count;
             i++)
        {
            RendererEmissionData data =
                managedEmissives[i];

            if (data.material == null)
            {
                continue;
            }

            startingEmissionColors[i] =
                data.material.GetColor(
                    "_EmissionColor"
                );

            data.material.EnableKeyword(
                "_EMISSION"
            );
        }

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / safeDuration
            );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            for (int i = 0;
                 i < managedLights.Count;
                 i++)
            {
                ManagedLightData data =
                    managedLights[i];

                if (data.light == null)
                {
                    continue;
                }

                float targetIntensity =
                    on
                        ? data.originalIntensity
                        : 0f;

                data.light.intensity =
                    Mathf.Lerp(
                        startingIntensities[i],
                        targetIntensity,
                        smoothProgress
                    );
            }

            for (int i = 0;
                 i < managedEmissives.Count;
                 i++)
            {
                RendererEmissionData data =
                    managedEmissives[i];

                if (data.material == null)
                {
                    continue;
                }

                Color targetColor =
                    on
                        ? data.originalEmissionColor
                        : Color.black;

                data.material.SetColor(
                    "_EmissionColor",
                    Color.Lerp(
                        startingEmissionColors[i],
                        targetColor,
                        smoothProgress
                    )
                );
            }

            yield return null;
        }

        // Ép chính xác trạng thái cuối.
        for (int i = 0;
             i < managedLights.Count;
             i++)
        {
            ManagedLightData data =
                managedLights[i];

            if (data.light == null)
            {
                continue;
            }

            data.light.intensity =
                on
                    ? data.originalIntensity
                    : 0f;

            data.light.enabled = on;
        }

        foreach (RendererEmissionData data in
                 managedEmissives)
        {
            if (data.material == null)
            {
                continue;
            }

            if (on)
            {
                data.material.EnableKeyword(
                    "_EMISSION"
                );

                data.material.SetColor(
                    "_EmissionColor",
                    data.originalEmissionColor
                );
            }
            else
            {
                data.material.SetColor(
                    "_EmissionColor",
                    Color.black
                );

                data.material.DisableKeyword(
                    "_EMISSION"
                );
            }
        }
    }
}