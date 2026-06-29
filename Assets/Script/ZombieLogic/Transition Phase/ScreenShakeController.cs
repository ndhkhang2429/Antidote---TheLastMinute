using System.Collections;
using UnityEngine;

/// <summary>
/// Screen Shake System - Rung màn hình theo các pattern khác nhau
/// Dùng cho cutscene transformations và combat impact
/// </summary>
public class ScreenShakeController : MonoBehaviour
{
    private static ScreenShakeController _instance;
    public static ScreenShakeController Instance => _instance;

    [Header("== Shake Settings ==")]
    [SerializeField] private Camera mainCamera;
    private Vector3 _originalCameraPos;
    private float _shakeTimer = 0f;
    private float _shakeDuration = 0f;
    private float _shakeIntensity = 0f;
    private float _shakeFrequency = 5f;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
            Destroy(gameObject);

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        if (mainCamera != null)
            _originalCameraPos = mainCamera.transform.localPosition;
    }

    private void LateUpdate()
    {
        if (_shakeTimer > 0)
        {
            _shakeTimer -= Time.deltaTime;

            // Perlin noise based shake for smooth organic motion
            float shakeX = Mathf.PerlinNoise(Time.time * _shakeFrequency, 0f) - 0.5f;
            float shakeY = Mathf.PerlinNoise(Time.time * _shakeFrequency, 1f) - 0.5f;
            float shakeZ = Mathf.PerlinNoise(Time.time * _shakeFrequency, 2f) - 0.5f;

            Vector3 shakeOffset = new Vector3(shakeX, shakeY, shakeZ) * _shakeIntensity;
            mainCamera.transform.localPosition = _originalCameraPos + shakeOffset;
        }
        else
        {
            // Return to original position smoothly
            mainCamera.transform.localPosition = Vector3.Lerp(
                mainCamera.transform.localPosition,
                _originalCameraPos,
                Time.deltaTime * 5f
            );
        }
    }

    /// <summary>
    /// Trigger screen shake
    /// </summary>
    /// <param name="duration">Bao lâu shake diễn ra (seconds)</param>
    /// <param name="intensity">Cường độ rung (world units)</param>
    /// <param name="frequency">Tần số rung (Hz) - cao = rung nhanh</param>
    public void Shake(float duration, float intensity, float frequency = 5f)
    {
        _shakeDuration = duration;
        _shakeTimer = duration;
        _shakeIntensity = intensity;
        _shakeFrequency = frequency;
    }

    /// <summary>
    /// Trigger soft shake (dread building)
    /// </summary>
    public void ShakeSoft()
    {
        Shake(1.5f, 0.15f, 3f);
    }

    /// <summary>
    /// Trigger medium shake (transformation moment)
    /// </summary>
    public void ShakeMedium()
    {
        Shake(0.8f, 0.5f, 8f);
    }

    /// <summary>
    /// Trigger hard shake (roar/impact)
    /// </summary>
    public void ShakeHard()
    {
        Shake(1.0f, 1.2f, 12f);
    }

    /// <summary>
    /// Stop shake immediately
    /// </summary>
    public void StopShake()
    {
        _shakeTimer = 0f;
    }
}