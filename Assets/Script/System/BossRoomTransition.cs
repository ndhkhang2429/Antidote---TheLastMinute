using UnityEngine;
using UnityEngine.Playables;

public class BossRoomTransition : MonoBehaviour
{
    [Header("Cutscene Settings")]
    public PlayableDirector timelineDirector; // Kéo BossCutsceneDirector vào đây
    public Transform bossRoomSpawnPoint;      // Kéo BossRoomSpawnPoint vào đây

    [Header("Player Settings")]
    public Transform player;                  // Kéo nhân vật của bạn vào đây
    public MonoBehaviour playerMovementScript; // Kéo script di chuyển của nhân vật vào đây

    private bool hasTransitioned = false;

    // Hàm này sẽ được gọi khi bạn bấm nút mở cửa ở tầng 2
    public void StartTransitionSequence()
    {
        if (hasTransitioned) return;
        hasTransitioned = true;

        // 1. Khóa di chuyển để người chơi không chạy lung tung khi đang xem phim
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        // 2. Dịch chuyển (Teleport) người chơi
        TeleportPlayer();

        // 3. Chạy Timeline Cutscene
        if (timelineDirector != null)
        {
            timelineDirector.Play();

            // Lắng nghe sự kiện phim chạy xong để mở khóa di chuyển
            timelineDirector.stopped += OnCutsceneFinished;
        }
    }

    private void TeleportPlayer()
    {
        // Tắt CharacterController (nếu có) trước khi dịch chuyển để tránh lỗi vật lý của Unity kéo nhân vật về chỗ cũ
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Đưa nhân vật đến đúng vị trí và hướng nhìn của Spawn Point
        player.position = bossRoomSpawnPoint.position;
        player.rotation = bossRoomSpawnPoint.rotation;

        // Bật lại CharacterController
        if (cc != null) cc.enabled = true;
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        // 4. Phim xong -> Trả lại quyền di chuyển cho người chơi
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        // Hủy lắng nghe để dọn dẹp bộ nhớ
        timelineDirector.stopped -= OnCutsceneFinished;

        Debug.Log("Boss Fight Bắt Đầu!");
    }
}