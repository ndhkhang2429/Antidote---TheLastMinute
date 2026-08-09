using UnityEngine;

public class ItemHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    [ColorUsage(true, true)]
    [SerializeField] private Color highlightColor = Color.white;

    [SerializeField, Range(0f, 10f)]
    private float emissionIntensity = 2.5f;

    private Renderer[] _renderers;
    private Material[][] _materials;
    private Color[][] _defaultEmissionColors;
    private bool[][] _hasEmissionProperties;

    private void Awake()
    {
        // Lấy toàn bộ Renderer của object và tất cả object con.
        _renderers = GetComponentsInChildren<Renderer>(true);

        _materials = new Material[_renderers.Length][];
        _defaultEmissionColors = new Color[_renderers.Length][];
        _hasEmissionProperties = new bool[_renderers.Length][];

        for (int rendererIndex = 0;
             rendererIndex < _renderers.Length;
             rendererIndex++)
        {
            // materials tạo các material instance riêng cho item này,
            // tránh làm đổi màu tất cả item dùng chung material.
            _materials[rendererIndex] =
                _renderers[rendererIndex].materials;

            int materialCount = _materials[rendererIndex].Length;

            _defaultEmissionColors[rendererIndex] =
                new Color[materialCount];

            _hasEmissionProperties[rendererIndex] =
                new bool[materialCount];

            for (int materialIndex = 0;
                 materialIndex < materialCount;
                 materialIndex++)
            {
                Material material =
                    _materials[rendererIndex][materialIndex];

                if (material == null ||
                    !material.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                _hasEmissionProperties[rendererIndex][materialIndex] = true;

                _defaultEmissionColors[rendererIndex][materialIndex] =
                    material.GetColor("_EmissionColor");
            }
        }
    }

    public void ToggleHighlight(bool isOn)
    {
        if (_materials == null)
            return;

        Color finalHighlightColor =
            highlightColor * emissionIntensity;

        for (int rendererIndex = 0;
             rendererIndex < _materials.Length;
             rendererIndex++)
        {
            for (int materialIndex = 0;
                 materialIndex < _materials[rendererIndex].Length;
                 materialIndex++)
            {
                if (!_hasEmissionProperties[rendererIndex][materialIndex])
                    continue;

                Material material =
                    _materials[rendererIndex][materialIndex];

                if (material == null)
                    continue;

                if (isOn)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor(
                        "_EmissionColor",
                        finalHighlightColor
                    );
                }
                else
                {
                    Color defaultColor =
                        _defaultEmissionColors
                        [rendererIndex]
                        [materialIndex];

                    material.SetColor(
                        "_EmissionColor",
                        defaultColor
                    );

                    if (defaultColor.maxColorComponent <= 0.001f)
                        material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    private void OnDisable()
    {
        ToggleHighlight(false);
    }
}