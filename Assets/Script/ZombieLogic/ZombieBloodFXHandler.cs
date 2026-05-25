using UnityEngine;

public class ZombieBloodFXHandler : MonoBehaviour
{
    [Header("Blood VFX Prefabs")]
    public GameObject[] BloodFXPrefabs;
    public GameObject AttachedBloodDecal;

    [Header("Settings")]
    public Light DirectionalLight;
    public Vector3 UpDirection = Vector3.up;
    public float DecalScaleMin = 0.75f;
    public float DecalScaleMax = 1.2f;

    private int _effectIndex = 0;

    // ── Dùng cho SÚNG (sau này) ──────────────────
    public void OnHit(RaycastHit hit)
    {
        SpawnBloodFX(hit.point, hit.normal, hit.transform);
    }

    // ── Dùng cho MELEE (hiện tại) ─────────────────
    public void OnHitMelee(Vector3 hitPoint, Vector3 hitNormal)
    {
        SpawnBloodFX(hitPoint, hitNormal, this.transform);
    }

    private void SpawnBloodFX(Vector3 hitPoint, Vector3 hitNormal, Transform hitTransform)
    {
        if (BloodFXPrefabs == null || BloodFXPrefabs.Length == 0) return;

        float angle = Mathf.Atan2(hitNormal.x, hitNormal.z) * Mathf.Rad2Deg + 180;
        var bloodInstance = Instantiate(
            BloodFXPrefabs[_effectIndex],
            hitPoint,
            Quaternion.Euler(0, angle + 90, 0)
        );
        _effectIndex = (_effectIndex + 1) % BloodFXPrefabs.Length;

        var settings = bloodInstance.GetComponent<BFX_BloodSettings>();
        if (settings != null && DirectionalLight != null)
            settings.LightIntensityMultiplier = DirectionalLight.intensity;

        if (AttachedBloodDecal == null) return;
        Transform nearestBone = GetNearestBone(hitTransform.root, hitPoint);
        if (nearestBone == null) return;

        var decalInstance = Instantiate(AttachedBloodDecal);
        var decalT = decalInstance.transform;
        decalT.position = hitPoint;
        decalT.localRotation = Quaternion.identity;
        decalT.localScale = Vector3.one * Random.Range(DecalScaleMin, DecalScaleMax);
        decalT.LookAt(hitPoint + hitNormal, UpDirection);
        decalT.Rotate(90, 0, 0);
        decalT.SetParent(nearestBone);
    }

    private Transform GetNearestBone(Transform root, Vector3 hitPos)
    {
        Transform closest = root;
        float closestDist = Vector3.Distance(root.position, hitPos);
        foreach (var child in root.GetComponentsInChildren<Transform>())
        {
            float dist = Vector3.Distance(child.position, hitPos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = child;
            }
        }
        return closest;
    }
}