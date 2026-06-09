using UnityEngine;

public class ElectricalBoxPuzzle : MonoBehaviour
{
    [Header("--- LIÊN KẾT HỆ THỐNG ---")]
    public LightingManager lightingManager; // Kéo object chứa LightingManager vào đây

    [Header("--- GIAI ĐOẠN 1: CẦU CHÌ ---")]
    public int totalFusesRequired = 2;
    private int currentFusesInstalled = 0;
    public GameObject[] missingFuseVisuals;

    [Header("--- GIAI ĐOẠN 2: CÔNG TẮC ---")]
    public bool[] currentSwitchStates;
    public bool[] correctSwitchStates;
    public GameObject[] knobTransforms;

    [Header("--- GIAI ĐOẠN 3: KÍCH HOẠT TỔNG ---")]
    public Transform mainSwitch;
    public Material lampMaterial;
    private bool isPatternCorrect = false;
    private bool isPowerOn = false;

    void Start()
    {
        foreach (GameObject fuse in missingFuseVisuals)
        {
            fuse.SetActive(false);
        }
        SetLampColor(Color.red);
        CheckSwitchPattern(); // Kiểm tra trạng thái ngay từ đầu
    }

    public void InstallFuse()
    {
        if (currentFusesInstalled < totalFusesRequired)
        {
            missingFuseVisuals[currentFusesInstalled].SetActive(true);
            currentFusesInstalled++;
            Debug.Log("Đã lắp cầu chì: " + currentFusesInstalled + "/" + totalFusesRequired);
        }
    }

    public void ToggleSwitch(int switchIndex)
    {
        // Cho phép gạt công tắc kể cả khi chưa có cầu chì (tạo cảm giác tự do tương tác)
        currentSwitchStates[switchIndex] = !currentSwitchStates[switchIndex];

        float targetAngle = currentSwitchStates[switchIndex] ? 30f : -30f;
        knobTransforms[switchIndex].transform.localRotation = Quaternion.Euler(targetAngle, 0, 0);

        CheckSwitchPattern();
    }

    void CheckSwitchPattern()
    {
        isPatternCorrect = true;
        for (int i = 0; i < currentSwitchStates.Length; i++)
        {
            if (currentSwitchStates[i] != correctSwitchStates[i])
            {
                isPatternCorrect = false;
                break;
            }
        }
    }

    // Hàm gọi khi người chơi tương tác với Cần gạt lớn
    public void PullMainSwitch()
    {
        if (isPowerOn) return; // Nếu đã bật điện rồi thì không cho gạt nữa

        // Xoay cần gạt xuống (Visual)
        mainSwitch.localRotation = Quaternion.Euler(45f, 0, 0);

        // Kiểm tra xem đã đủ Cầu Chì CHƯA
        if (currentFusesInstalled < totalFusesRequired)
        {
            Debug.Log("Thiếu cầu chì! Mạch không kín.");
            TriggerFailure();
            return; // Dừng lại, không chạy code bên dưới
        }

        // Kiểm tra xem Công Tắc đã đúng CHƯA
        if (isPatternCorrect)
        {
            TriggerSuccess();
        }
        else
        {
            Debug.Log("Sai thứ tự công tắc! Chập mạch.");
            TriggerFailure();
        }
    }

    void TriggerSuccess()
    {
        isPowerOn = true;
        SetLampColor(Color.green);
        Debug.Log("THÀNH CÔNG! Bệnh viện đã có điện.");

        // Gọi LightingManager để bật toàn bộ đèn (sử dụng hiệu ứng Fade)
        if (lightingManager != null)
        {
            lightingManager.SetPower(true, false);
        }
    }

    void TriggerFailure()
    {
        // Trả cần gạt về vị trí cũ sau 0.5 giây
        Invoke("ResetMainSwitch", 0.5f);

        // Gợi ý: Chèn dòng code phát Audio Source tiếng chập điện (spark) ở đây
    }

    void ResetMainSwitch()
    {
        mainSwitch.localRotation = Quaternion.Euler(0, 0, 0);
    }

    void SetLampColor(Color color)
    {
        if (lampMaterial != null)
        {
            lampMaterial.SetColor("_EmissionColor", color * 2f);
            DynamicGI.SetEmissive(GetComponent<Renderer>(), color);
        }
    }
}