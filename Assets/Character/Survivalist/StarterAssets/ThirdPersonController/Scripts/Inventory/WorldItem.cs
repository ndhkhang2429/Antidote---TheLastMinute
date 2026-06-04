using UnityEngine;

public class WorldItem : MonoBehaviour
{
    public ItemDataSO itemData;
    public int quantity = 1;

    [Header("Interact")]
    public float pickupRadius = 2f;
    public KeyCode pickupKey = KeyCode.F;

    Transform _player;

    void Start() => _player = GameObject.FindWithTag("Player")?.transform;

    void Update()
    {
        if (_player == null) return;
        if (Vector3.Distance(transform.position, _player.position) > pickupRadius) return;
        if (!Input.GetKeyDown(pickupKey)) return;

        bool picked = InventorySystem.Instance.PickupItem(itemData, quantity);
        if (picked) Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}