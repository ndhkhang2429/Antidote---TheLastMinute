using UnityEngine;

public class MatchCameraRotation : MonoBehaviour
{
    private Transform _mainCamera;

    void Start()
    {
        // Tự động tìm Main Camera trong Scene
        if (Camera.main != null)
        {
            _mainCamera = Camera.main.transform;
        }
    }

    // Dùng LateUpdate để xoay tay SAU KHI Camera đã xoay xong
    void LateUpdate()
    {
        if (_mainCamera != null)
        {
            // Ép cụm tay súng copy y hệt góc quay (lên/xuống, trái/phải) của Camera
            transform.rotation = _mainCamera.rotation;
        }
    }
}