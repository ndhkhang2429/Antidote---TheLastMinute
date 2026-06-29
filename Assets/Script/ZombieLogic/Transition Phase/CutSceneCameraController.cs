using System.Collections;
using UnityEngine;

/// <summary>
/// Cutscene Camera Controller - Chuyên xử lý camera animations
/// Hỗ trợ: Orbit, Dolly Zoom, Follow, Shake, Position Lerp
/// </summary>
public class CutsceneCameraController : MonoBehaviour
{
    private static CutsceneCameraController _instance;
    public static CutsceneCameraController Instance => _instance;

    [Header("== Main Camera Reference ==")]
    public Camera mainCamera;

    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private float _originalFOV;

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
        {
            _originalPosition = mainCamera.transform.position;
            _originalRotation = mainCamera.transform.rotation;
            _originalFOV = mainCamera.fieldOfView;
        }
    }

    // ============ ORBIT MOVEMENT ============

    /// <summary>
    /// Quay quanh target (orbit)
    /// </summary>
    /// <param name="target">Điểm quay quanh</param>
    /// <param name="radius">Khoảng cách từ target</param>
    /// <param name="startAngle">Góc bắt đầu (độ)</param>
    /// <param name="endAngle">Góc kết thúc (độ)</param>
    /// <param name="duration">Thời gian quay</param>
    /// <param name="height">Độ cao của camera so với target</param>
    public IEnumerator OrbitAround(Transform target, float radius, float startAngle, float endAngle, float duration, float height = 0f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);

            // Tính vị trí orbit
            float x = target.position.x + Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius;
            float z = target.position.z + Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius;
            Vector3 newPos = new Vector3(x, target.position.y + height, z);

            mainCamera.transform.position = newPos;
            mainCamera.transform.LookAt(target.position + Vector3.up * height * 0.5f);

            yield return null;
        }
    }

    // ============ DOLLY ZOOM (ZOOM & TRUCK) ============

    /// <summary>
    /// Dolly Zoom effect - camera moves back while zooming in (hoặc ngược lại)
    /// Tạo cảm giác bất an, distortion
    /// </summary>
    /// <param name="target">Điểm focus</param>
    /// <param name="duration">Thời gian effect</param>
    /// <param name="moveDistance">Khoảng cách di chuyển (âm = lùi lại)</param>
    /// <param name="fovChange">Thay đổi FOV (âm = zoom in)</param>
    public IEnumerator DollyZoom(Transform target, float duration, float moveDistance, float fovChange)
    {
        Vector3 startPos = mainCamera.transform.position;
        float startFOV = mainCamera.fieldOfView;
        float endFOV = startFOV + fovChange;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Di chuyển camera
            Vector3 direction = (mainCamera.transform.position - target.position).normalized;
            Vector3 newPos = target.position + direction * (Vector3.Distance(startPos, target.position) + moveDistance * t);
            mainCamera.transform.position = newPos;

            // Thay đổi FOV
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, t);

            // Luôn nhìn vào target
            mainCamera.transform.LookAt(target.position);

            yield return null;
        }
    }

    // ============ SMOOTH POSITION MOVEMENT ============

    /// <summary>
    /// Di chuyển camera từ vị trí này đến vị trí khác
    /// </summary>
    public IEnumerator MoveTo(Vector3 targetPosition, Vector3 lookAtTarget, float duration, AnimationCurve easeCurve = null)
    {
        Vector3 startPos = mainCamera.transform.position;
        Vector3 startLookAt = mainCamera.transform.forward;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (easeCurve != null)
                t = easeCurve.Evaluate(t);

            mainCamera.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            mainCamera.transform.LookAt(lookAtTarget);

            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.transform.LookAt(lookAtTarget);
    }

    // ============ FOV TRANSITIONS ============

    /// <summary>
    /// Thay đổi FOV (zoom in/out)
    /// </summary>
    public IEnumerator ChangeFOV(float targetFOV, float duration, AnimationCurve easeCurve = null)
    {
        float startFOV = mainCamera.fieldOfView;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (easeCurve != null)
                t = easeCurve.Evaluate(t);

            mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
            yield return null;
        }

        mainCamera.fieldOfView = targetFOV;
    }

    // ============ CLOSE-UP SHOT ============

    /// <summary>
    /// Zoom vào close-up một phần của body (ví dụ: đầu, vai, ngực)
    /// </summary>
    public IEnumerator CloseUpShot(Transform target, Vector3 bodyPartOffset, float distance, float duration)
    {
        Vector3 targetPos = target.position + bodyPartOffset;
        Vector3 cameraPos = targetPos - target.forward * distance;

        yield return StartCoroutine(MoveTo(cameraPos, targetPos, duration));
    }

    // ============ SIDE PROFILE SHOT ============

    /// <summary>
    /// Quay sang góc side profile của target
    /// </summary>
    public IEnumerator SideProfileShot(Transform target, float distance, float height, float duration, bool rightSide = true)
    {
        Vector3 sideDirection = rightSide ? target.right : -target.right;
        Vector3 targetCameraPos = target.position + sideDirection * distance + Vector3.up * height;

        yield return StartCoroutine(MoveTo(targetCameraPos, target.position + Vector3.up * (height * 0.5f), duration));
    }

    // ============ DRAMATIC UPWARD ANGLE ============

    /// <summary>
    /// Camera nhìn từ dưới lên (dramatic, powerful)
    /// </summary>
    public IEnumerator DramaticLowAngle(Transform target, float distance, float upAngle, float duration)
    {
        Vector3 direction = (mainCamera.transform.position - target.position).normalized;
        direction.y = 0;
        Vector3 targetCameraPos = target.position + direction * distance + Vector3.up * distance * upAngle;

        yield return StartCoroutine(MoveTo(targetCameraPos, target.position + Vector3.up, duration));
    }

    // ============ LOOK-AT FOCUS CHANGE ============

    /// <summary>
    /// Thay đổi focus điểm nhìn (gaze shift)
    /// </summary>
    public IEnumerator GazeShift(Vector3 fromTarget, Vector3 toTarget, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 currentTarget = Vector3.Lerp(fromTarget, toTarget, t);
            mainCamera.transform.LookAt(currentTarget);

            yield return null;
        }

        mainCamera.transform.LookAt(toTarget);
    }

    // ============ COMPLEX ORBITAL MOVEMENT ============

    /// <summary>
    /// Quay orbit với dynamic radius (tighter/looser)
    /// </summary>
    public IEnumerator DynamicOrbit(Transform target, float startRadius, float endRadius,
        float startAngle, float endAngle, float duration, float height = 0f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float currentRadius = Mathf.Lerp(startRadius, endRadius, t);
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);

            float x = target.position.x + Mathf.Cos(currentAngle * Mathf.Deg2Rad) * currentRadius;
            float z = target.position.z + Mathf.Sin(currentAngle * Mathf.Deg2Rad) * currentRadius;
            Vector3 newPos = new Vector3(x, target.position.y + height, z);

            mainCamera.transform.position = newPos;
            mainCamera.transform.LookAt(target.position + Vector3.up * height * 0.5f);

            yield return null;
        }
    }

    // ============ RESET CAMERA ============

    /// <summary>
    /// Trả camera về vị trí/rotation ban đầu
    /// </summary>
    public IEnumerator ResetCamera(float duration)
    {
        yield return StartCoroutine(MoveTo(_originalPosition, _originalPosition + Vector3.forward, duration));
        mainCamera.fieldOfView = _originalFOV;
        mainCamera.transform.rotation = _originalRotation;
    }

    /// <summary>
    /// Reset instantly (không smooth)
    /// </summary>
    public void ResetCameraImmediate()
    {
        mainCamera.transform.position = _originalPosition;
        mainCamera.transform.rotation = _originalRotation;
        mainCamera.fieldOfView = _originalFOV;
    }

    // ============ SHAKE INTEGRATION ============

    /// <summary>
    /// Kết hợp camera movement với screen shake
    /// </summary>
    public IEnumerator OrbitWithShake(Transform target, float radius, float startAngle, float endAngle,
        float duration, float shakeIntensity = 0.2f)
    {
        ScreenShakeController.Instance.Shake(duration, shakeIntensity, 5f);
        yield return StartCoroutine(OrbitAround(target, radius, startAngle, endAngle, duration));
    }
}