using UnityEngine;

[CreateAssetMenu(fileName = "QuestChain_", menuName = "DeadRoof/Quest/Quest Chain")]
public class QuestChainSO : ScriptableObject
{
    [Header("Chuỗi nhiệm vụ theo thứ tự (VD: Tầng Trệt)")]
    public string chainName;              // "Ground Floor"
    public QuestStepSO[] steps;           // Thứ tự trong mảng = thứ tự thực hiện
}
