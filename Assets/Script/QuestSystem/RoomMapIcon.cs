using UnityEngine;
using UnityEngine.UI;

public class RoomMapIcon : MonoBehaviour
{
    public string roomID;

    [SerializeField] private Image iconImage;
    [SerializeField] private Color undiscoveredColor = new Color(1, 1, 1, 0.25f);
    [SerializeField] private Color discoveredColor = Color.white;

    private void Start() => Refresh(false);

    public void SetDiscovered(bool value) => Refresh(value);

    private void Refresh(bool isDiscovered)
    {
        if (iconImage == null) return;
        iconImage.color = isDiscovered ? discoveredColor : undiscoveredColor;
    }
}
