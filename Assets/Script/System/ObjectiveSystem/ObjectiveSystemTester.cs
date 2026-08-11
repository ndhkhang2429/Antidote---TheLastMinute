using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectiveSystemTester : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null ||
            ObjectiveManager.Instance == null)
        {
            return;
        }

        // Thêm một nhiệm vụ.
        if (Keyboard.current.f11Key.wasPressedThisFrame)
        {
            ObjectiveManager.Instance.AddObjective(
                "restore_power",
                "Restore power to the hospital"
            );
        }

        // Cập nhật nội dung nhiệm vụ.
        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            ObjectiveManager.Instance.UpdateObjective(
                "restore_power",
                "Find the main electrical room"
            );
        }

        // Hoàn thành nhiệm vụ.
        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            ObjectiveManager.Instance.CompleteObjective(
                "restore_power"
            );
        }

        // Thêm ba nhiệm vụ cùng lúc.
        if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            AddThreeObjectives();
        }
    }

    private void AddThreeObjectives()
    {
        ObjectiveManager.Instance.AddObjective(
            "clue_1",
            "Find the reception security record"
        );

        ObjectiveManager.Instance.AddObjective(
            "clue_2",
            "Find the isolation report"
        );

        ObjectiveManager.Instance.AddObjective(
            "clue_3",
            "Find the chief guard's duty log"
        );
    }
}