using UnityEngine;

public class ProZombieExplosion : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject spurtPrefab; // Kéo VFX_Blood_Spurt vào đây
    public GameObject chunksPrefab; // Kéo VFX_Blood_Chunks vào đây
    public GameObject[] meatGibs;  // Các mảnh nội tạng Quake 3

    [Header("Materials (Để ngẫu nhiên hóa)")]
    public Material[] directionalMaterials; // Kéo 3 cái Mat_Directional vào đây
    public Material[] radialMaterials;      // Kéo 3 cái Mat_Radial vào đây

    public void ExecuteExplosion()
    {
        Vector3 pos = transform.position + Vector3.up * 1f;

        // 1. Sinh ra tia máu và đổi Material ngẫu nhiên
        GameObject spurt = Instantiate(spurtPrefab, pos, Quaternion.identity);
        RandomizeMaterial(spurt, directionalMaterials);

        // 2. Sinh ra cục máu đặc và đổi Material ngẫu nhiên
        GameObject chunks = Instantiate(chunksPrefab, pos, Quaternion.identity);
        RandomizeMaterial(chunks, radialMaterials);

        // 3. Sinh ra thịt văng (Gibs)
        foreach (GameObject gib in meatGibs)
        {
            GameObject g = Instantiate(gib, pos + Random.insideUnitSphere * 0.5f, Random.rotation);
            g.GetComponent<Rigidbody>().AddExplosionForce(20f, pos, 5f, 1f, ForceMode.Impulse);
            Destroy(g, 5f);
        }

        // 4. Xóa Zombie
        Destroy(gameObject);
    }

    void RandomizeMaterial(GameObject psObj, Material[] mats)
    {
        if (mats.Length == 0) return;

        // Lấy ParticleSystemRenderer để đổi Material
        var renderer = psObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = mats[Random.Range(0, mats.Length)];

        Destroy(psObj, 3f);
    }
}