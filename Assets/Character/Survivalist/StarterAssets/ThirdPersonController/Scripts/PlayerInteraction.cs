using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Cài đặt Tương tác")]
    public float interactionRadius = 2.0f; // Bán kính vòng tròn quét đồ
    public LayerMask itemLayer; // Nhớ tạo Layer tên là "Item" và gán cho các vật phẩm

    [Header("Thành phần kết nối")]
    public Animator animator;
    public Transform weaponSlot; // Gắn cái Transform bàn tay vào đây

    private GameObject nearestItem = null; // Lưu món đồ đang đứng gần nhất
    private GameObject currentItemInHand = null; // Món đồ đang cầm trên tay

    void Update()
    {
        FindNearestItem();

        // Nếu tìm thấy đồ và người chơi bấm F
        if (nearestItem != null && Input.GetKeyDown(KeyCode.F))
        {
            PerformPickup();
        }
    }

    // Hàm này quét các vật phẩm xung quanh và tìm món gần nhất
    void FindNearestItem()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, itemLayer);

        if (hitColliders.Length > 0)
        {
            float shortestDistance = Mathf.Infinity;
            GameObject closestObj = null;

            // Lọc ra món đồ gần nhân vật nhất trong bán kính
            foreach (Collider col in hitColliders)
            {
                float distanceToItem = Vector3.Distance(transform.position, col.transform.position);
                if (distanceToItem < shortestDistance)
                {
                    shortestDistance = distanceToItem;
                    closestObj = col.gameObject;
                }
            }

            nearestItem = closestObj;

            // TODO: Chỗ này sau này bạn có thể gọi UI hiện chữ "Bấm F để nhặt: [Tên đồ]"
        }
        else
        {
            nearestItem = null;
            // TODO: Tắt UI nhặt đồ
        }
    }

    void PerformPickup()
    {
        // Kích hoạt animation cúi nhặt (tham số bạn đã setup ở bước trước)
        animator.SetTrigger("PickUp");
    }

    // HÀM NÀY ĐƯỢC GỌI TỪ ANIMATION EVENT (Khi tay vung ra chạm đất)
    public void EquipItem()
    {
        if (nearestItem != null)
        {
            // Vứt đồ cũ nếu đang cầm
            if (currentItemInHand != null)
            {
                // Logic vứt đồ (bật lại Rigidbody, nhả parent...)
                currentItemInHand.transform.SetParent(null);
                if (currentItemInHand.GetComponent<Rigidbody>()) currentItemInHand.GetComponent<Rigidbody>().isKinematic = false;
            }

            
            currentItemInHand = nearestItem;

            // Xử lý món đồ mới nhặt: Tắt vật lý, gắn vào tay
            if (currentItemInHand.GetComponent<Rigidbody>()) currentItemInHand.GetComponent<Rigidbody>().isKinematic = true;
            if (currentItemInHand.GetComponent<Collider>()) currentItemInHand.GetComponent<Collider>().enabled = false;

            currentItemInHand.transform.SetParent(weaponSlot);
            currentItemInHand.transform.localPosition = Vector3.zero;
            currentItemInHand.transform.localRotation = Quaternion.identity;
            animator.SetBool("IsArmed", true);
            // Xóa bộ nhớ tạm để không nhặt lại chính nó
            nearestItem = null;
        }
    }

    // Vẽ một vòng tròn đỏ trong Editor để bạn dễ hình dung bán kính quét đồ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}