using UnityEngine;
using UnityEngine.UI;

public class GameOverBackgroundSetter : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite fallbackSprite;

    private void Start()
    {
        if (DeathScreenshotHolder.LastFrame != null)
        {
            Sprite deathSprite = Sprite.Create(
                DeathScreenshotHolder.LastFrame,
                new Rect(0, 0, DeathScreenshotHolder.LastFrame.width, DeathScreenshotHolder.LastFrame.height),
                new Vector2(0.5f, 0.5f)
            );
            backgroundImage.sprite = deathSprite;
        }
        else if (fallbackSprite != null)
        {
            backgroundImage.sprite = fallbackSprite;
        }
    }
}