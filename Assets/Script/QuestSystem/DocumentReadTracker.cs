using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton theo dõi những DocumentDataSO đã được đọc ít nhất 1 lần trong phiên chơi.
/// Khi đọc lần đầu: bắn onFirstReadEvent (nếu có) + báo ngầm cho QuestManager
/// (KHÔNG có UI nào thông báo cho player — đúng tinh thần "tự nhận ra" bạn muốn).
/// Đặt object này 1 lần trong scene, ngang hàng QuestManager.
/// </summary>
public class DocumentReadTracker : MonoBehaviour
{
    public static DocumentReadTracker Instance { get; private set; }

    private readonly HashSet<DocumentDataSO> readDocuments = new HashSet<DocumentDataSO>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool HasRead(DocumentDataSO doc) => doc != null && readDocuments.Contains(doc);

    // Gọi từ DocumentReaderUI mỗi khi player mở đọc 1 tài liệu
    public void MarkRead(DocumentDataSO doc)
    {
        if (doc == null || readDocuments.Contains(doc)) return; // chỉ trigger lần đầu
        readDocuments.Add(doc);

        if (doc.onFirstReadEvent != null)
            doc.onFirstReadEvent.Raise();

        if (!string.IsNullOrEmpty(doc.documentID))
            QuestManager.Instance?.ReportEvent(QuestCompletionType.ReadDocument, doc.documentID);
    }
}
