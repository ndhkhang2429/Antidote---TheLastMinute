using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [Header("Refs")]
    public GameObject panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI weightText;
    public RectTransform rectTransform;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    void Update()
    {
        if (!panel.activeSelf) return;
        // Tooltip đi theo chuột
        Vector2 pos = Input.mousePosition;
        pos.x = Mathf.Min(pos.x + 12, Screen.width - rectTransform.sizeDelta.x - 4);
        pos.y = Mathf.Min(pos.y + 12, Screen.height - rectTransform.sizeDelta.y - 4);
        rectTransform.position = pos;
    }

    public static void Show(ItemDataSO item)
    {
        if (Instance == null || item == null) return;
        Instance.nameText.text = item.itemName;
        Instance.descText.text = item.description;
        Instance.weightText.text = item.weightPerUnit > 0
            ? $"Nặng {item.weightPerUnit} / đơn vị"
            : "Không chiếm sức chứa";
        Instance.panel.SetActive(true);
    }

    public static void Hide()
    {
        if (Instance != null) Instance.panel.SetActive(false);
    }
}