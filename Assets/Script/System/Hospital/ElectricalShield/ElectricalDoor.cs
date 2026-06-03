using System.Collections;
using UnityEngine;

public class ElectricalDoor : MonoBehaviour
{
    [Header("Cài đặt Cửa")]
    public Transform hingeTransform;
    public float openAngle = -180f; // Theo như Inspector của bạn là -180
    public float openSpeed = 2f;

    [Header("Trạng thái")]
    public bool isLocked = true;
    public bool isOpen = false;

    private Coroutine currentAnimation;

    public void InteractWithDoor(bool playerHasKey)
    {
        // Dừng hoạt ảnh cũ nếu người chơi spam nút bấm liên tục
        if (currentAnimation != null) StopCoroutine(currentAnimation);

        if (!isOpen)
        {
            // --- NẾU CỬA ĐANG ĐÓNG -> TÌM CÁCH MỞ ---
            if (isLocked)
            {
                if (playerHasKey)
                {
                    Debug.Log("Có chìa khóa -> ĐÃ MỞ KHÓA TỦ ĐIỆN!");
                    isLocked = false;
                    isOpen = true;
                    currentAnimation = StartCoroutine(AnimateDoor(openAngle));
                }
                else
                {
                    Debug.Log("Cửa khóa! Yêu cầu chìa khóa.");
                }
            }
            else
            {
                isOpen = true;
                currentAnimation = StartCoroutine(AnimateDoor(openAngle));
            }
        }
        else
        {
            // --- NẾU CỬA ĐANG MỞ -> ĐÓNG LẠI ---
            isOpen = false;
            currentAnimation = StartCoroutine(AnimateDoor(0f)); // Trả góc xoay về 0
        }
    }

    IEnumerator AnimateDoor(float targetAngle)
    {
        Quaternion startRot = hingeTransform.localRotation;
        Quaternion endRot = Quaternion.Euler(0, 0, targetAngle); // Quay quanh trục Y
        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * openSpeed;
            hingeTransform.localRotation = Quaternion.Slerp(startRot, endRot, time);
            yield return null;
        }

        // Chốt góc chính xác khi kết thúc animation để tránh sai số
        hingeTransform.localRotation = endRot;
    }
}