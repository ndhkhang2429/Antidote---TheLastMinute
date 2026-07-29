using UnityEngine;

[DefaultExecutionOrder(1000)]
public class FPSViewmodelCameraFollower : MonoBehaviour
{
    [Header("Camera thật dùng để nhìn")]
    [SerializeField] private Camera mainCamera;

    [Header("Độ lệch của viewmodel")]
    [SerializeField] private Vector3 localPositionOffset;
    [SerializeField] private Vector3 localRotationOffset;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            return;
        }

        Transform cameraTransform = mainCamera.transform;

        transform.position =
            cameraTransform.TransformPoint(localPositionOffset);

        transform.rotation =
            cameraTransform.rotation *
            Quaternion.Euler(localRotationOffset);
    }
}