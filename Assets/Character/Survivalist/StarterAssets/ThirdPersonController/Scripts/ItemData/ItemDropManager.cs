using UnityEngine;

public class ItemDropManager : MonoBehaviour
{
    [Header("Drop Origin")]
    [SerializeField] private Transform _playerRoot;
    [SerializeField] private float _forwardDistance = 1.6f;
    [SerializeField] private float _rayStartHeight = 2.5f;
    [SerializeField] private float _rayDistance = 8f;
    [SerializeField] private float _groundClearance = 0.03f;

    [Header("Surface Detection")]
    [SerializeField] private LayerMask _dropSurfaceMask = ~0;

    [Header("Rules")]
    [SerializeField] private bool _allowQuestItemDrop;

    private void Awake()
    {
        if (_playerRoot == null)
            _playerRoot = transform;
    }

    public bool TryDropSlot(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty || slot.item == null)
            return false;

        ItemDataSO item = slot.item;
        int quantity = slot.quantity;

        if (!_allowQuestItemDrop && item.category == ItemCategory.QuestItem)
        {
            NotificationUI.Instance?.ShowNotification(
                "Quest items cannot be discarded."
            );
            return false;
        }

        if (item.worldPrefab == null)
        {
            NotificationUI.Instance?.ShowNotification(
                $"{item.itemName} has no world prefab."
            );
            Debug.LogError(
                $"[ItemDropManager] World Prefab is missing for {item.itemName}.",
                item
            );
            return false;
        }

        if (!TryFindDropSurface(out RaycastHit hit))
        {
            NotificationUI.Instance?.ShowNotification(
                "No safe drop surface found."
            );
            return false;
        }

        GameObject droppedObject = Instantiate(
            item.worldPrefab,
            hit.point,
            Quaternion.identity
        );

        WorldItem worldItem =
            droppedObject.GetComponentInChildren<WorldItem>(true);

        if (worldItem == null)
        {
            Debug.LogError(
                $"[ItemDropManager] {item.worldPrefab.name} has no WorldItem component.",
                droppedObject
            );
            Destroy(droppedObject);
            NotificationUI.Instance?.ShowNotification(
                "This item cannot be dropped yet."
            );
            return false;
        }

        worldItem.itemData = item;
        worldItem.quantity = Mathf.Max(1, quantity);

        PlaceObjectOnSurface(droppedObject, hit.point);
        EnableDropPhysics(droppedObject);

        // Chỉ xóa khỏi balo sau khi spawn thành công.
        slot.Clear();
        InventorySystem.Instance?.NotifyInventoryChanged();

        NotificationUI.Instance?.ShowNotification(
            $"Dropped {item.itemName} x{quantity}."
        );

        return true;
    }

    private bool TryFindDropSurface(out RaycastHit hit)
    {
        Vector3 horizontalForward =
            Vector3.ProjectOnPlane(_playerRoot.forward, Vector3.up).normalized;

        if (horizontalForward.sqrMagnitude < 0.01f)
            horizontalForward = transform.forward;

        Vector3 targetPoint =
            _playerRoot.position + horizontalForward * _forwardDistance;

        Vector3 rayOrigin =
            targetPoint + Vector3.up * _rayStartHeight;

        return Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out hit,
            _rayDistance,
            _dropSurfaceMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void PlaceObjectOnSurface(GameObject target, Vector3 surfacePoint)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);

        if (colliders.Length == 0)
        {
            target.transform.position = surfacePoint + Vector3.up * _groundClearance;
            return;
        }

        Bounds combinedBounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
            combinedBounds.Encapsulate(colliders[i].bounds);

        float lift = surfacePoint.y - combinedBounds.min.y + _groundClearance;
        target.transform.position += Vector3.up * lift;
    }

    private static void EnableDropPhysics(GameObject target)
    {
        Rigidbody body = target.GetComponentInChildren<Rigidbody>(true);
        if (body == null) return;

        body.isKinematic = false;
        body.useGravity = true;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.WakeUp();
    }
}