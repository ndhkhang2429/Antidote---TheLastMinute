using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_6000_0_OR_NEWER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BFX_DemoTest : MonoBehaviour
{
    public bool InfiniteDecal;
    public Light DirLight;
    public bool isVR = true;
    public GameObject BloodAttach;
    public GameObject[] BloodFX;

    public Vector3 direction;

    int effectIdx;
    int activeBloods;

    Transform GetNearestObject(Transform hit, Vector3 hitPos)
    {
        var closestPos = 100f;
        Transform closestBone = null;
        var childs = hit.GetComponentsInChildren<Transform>();

        foreach (var child in childs)
        {
            var dist = Vector3.Distance(child.position, hitPos);
            if (dist < closestPos)
            {
                closestPos = dist;
                closestBone = child;
            }
        }

        var distRoot = Vector3.Distance(hit.position, hitPos);
        if (distRoot < closestPos)
        {
            closestPos = distRoot;
            closestBone = hit;
        }

        return closestBone;
    }

    void Update()
    {
        if (!TryGetLeftMouseClickPosition(out var mousePosition)) return;

        var cam = Camera.main;
        if (cam == null) return;

        var ray = cam.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out var hit))
        {
            float angle = Mathf.Atan2(hit.normal.x, hit.normal.z) * Mathf.Rad2Deg + 180;

            if (effectIdx == BloodFX.Length) effectIdx = 0;

            var instance = Instantiate(BloodFX[effectIdx], hit.point, Quaternion.Euler(0, angle + 90, 0));
            effectIdx++;
            activeBloods++;

            var settings = instance.GetComponent<BFX_BloodSettings>();
            if (settings != null && DirLight != null) settings.LightIntensityMultiplier = DirLight.intensity;

            var nearestBone = GetNearestObject(hit.transform.root, hit.point);
            if (nearestBone != null)
            {
                var attachBloodInstance = Instantiate(BloodAttach);
                var bloodT = attachBloodInstance.transform;

                bloodT.position = hit.point;
                bloodT.localRotation = Quaternion.identity;
                bloodT.localScale = Vector3.one * Random.Range(0.75f, 1.2f);
                bloodT.LookAt(hit.point + hit.normal, direction);
                bloodT.Rotate(90, 0, 0);
                bloodT.parent = nearestBone;
            }
        }
    }

    static bool TryGetLeftMouseClickPosition(out Vector2 mousePosition)
    {
#if UNITY_6000_0_OR_NEWER && ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            mousePosition = default;
            return false;
        }

        mousePosition = mouse.position.ReadValue();
        return true;
#else
        if (!Input.GetMouseButtonDown(0))
        {
            mousePosition = default;
            return false;
        }

        mousePosition = Input.mousePosition;
        return true;
#endif
    }

    public float CalculateAngle(Vector3 from, Vector3 to)
    {
        return Quaternion.FromToRotation(Vector3.up, to - from).eulerAngles.z;
    }
}