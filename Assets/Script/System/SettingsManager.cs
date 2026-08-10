using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio Mixers")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private AudioMixer bossAudioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider vfxSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider brightnessSlider;

    [Header("Value Text")]
    [SerializeField] private TMP_Text vfxValueText;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text brightnessValueText;

    [Header("Brightness")]
    [SerializeField] private Volume globalVolume;

    [SerializeField] private float minExposure = -2f;
    [SerializeField] private float maxExposure = 2f;

    private ColorAdjustments colorAdjustments;

    private const string VFX_KEY = "VFXVolume";
    private const string BGM_KEY = "BGMVolume";
    private const string BRIGHTNESS_KEY = "Brightness";

    private void Start()
    {
        float savedVFX = PlayerPrefs.GetFloat(VFX_KEY, 1f);
        float savedBGM = PlayerPrefs.GetFloat(BGM_KEY, 1f);

        // Brightness mặc định = 50%
        float savedBrightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, 0.5f);

        if (globalVolume != null &&
            globalVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.Log("Color Adjustments found.");
        }
        else
        {
            Debug.LogWarning("Color Adjustments not found in Global Volume.");
        }

        if (vfxSlider != null)
            vfxSlider.value = savedVFX;

        if (bgmSlider != null)
            bgmSlider.value = savedBGM;

        if (brightnessSlider != null)
            brightnessSlider.value = savedBrightness;

        SetVFXVolume(savedVFX);
        SetBGMVolume(savedBGM);
        SetBrightness(savedBrightness);

        if (vfxSlider != null)
            vfxSlider.onValueChanged.AddListener(SetVFXVolume);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
    }

    // =====================================================
    // VFX / SFX
    // =====================================================

    public void SetVFXVolume(float value)
    {
        value = Mathf.Clamp01(value);

        float dB = LinearToDecibel(value);

        // MAIN MIXER
        if (mainAudioMixer != null)
        {
            mainAudioMixer.SetFloat("Main_SFX", dB);
            mainAudioMixer.SetFloat("Main_UI", dB);
        }

        // BOSS MIXER
        if (bossAudioMixer != null)
        {
            bossAudioMixer.SetFloat("Boss_Voice", dB);
            bossAudioMixer.SetFloat("Boss_Impact", dB);
            bossAudioMixer.SetFloat("Boss_SFX", dB);
        }

        PlayerPrefs.SetFloat(VFX_KEY, value);

        if (vfxValueText != null)
            vfxValueText.text =
                Mathf.RoundToInt(value * 100f) + "%";
    }

    // =====================================================
    // BGM / MUSIC
    // =====================================================

    public void SetBGMVolume(float value)
    {
        value = Mathf.Clamp01(value);

        float dB = LinearToDecibel(value);

        // MAIN MIXER
        if (mainAudioMixer != null)
        {
            mainAudioMixer.SetFloat("Main_Music", dB);
            mainAudioMixer.SetFloat("Main_Ambience", dB);
        }

        // BOSS MIXER
        if (bossAudioMixer != null)
        {
            bossAudioMixer.SetFloat("Boss_Music", dB);
            bossAudioMixer.SetFloat("Boss_Ambience", dB);
        }

        PlayerPrefs.SetFloat(BGM_KEY, value);

        if (bgmValueText != null)
            bgmValueText.text =
                Mathf.RoundToInt(value * 100f) + "%";
    }

    // =====================================================
    // BRIGHTNESS - tạm thời chỉ xử lý UI
    // Phần thực tế sẽ nối Global Volume sau.
    // =====================================================

    public void SetBrightness(float value)
    {
        value = Mathf.Clamp01(value);

        float exposure = Mathf.Lerp(minExposure, maxExposure, value);

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.Override(exposure);
        }

        PlayerPrefs.SetFloat(BRIGHTNESS_KEY, value);

        if (brightnessValueText != null)
        {
            brightnessValueText.text =
                Mathf.RoundToInt(value * 100f) + "%";
        }
    }

    // =====================================================

    private float LinearToDecibel(float value)
    {
        if (value <= 0.0001f)
            return -80f;

        return Mathf.Log10(value) * 20f;
    }

    private void OnDestroy()
    {
        if (vfxSlider != null)
            vfxSlider.onValueChanged.RemoveListener(SetVFXVolume);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(SetBrightness);
    }
}