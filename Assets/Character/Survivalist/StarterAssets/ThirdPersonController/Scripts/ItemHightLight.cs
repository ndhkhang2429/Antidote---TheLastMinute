using UnityEngine;

public class ItemHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color highlightColor = new Color(0.2f, 0.2f, 0.2f);

    private Material _mat;
    private Color _defaultEmission = Color.black;
    private bool _hasEmissionProperty = false;

    private void Awake()
    {
        Renderer render = GetComponent<Renderer>();
        if (render != null)
        {
            _mat = render.material;
            if (_mat.HasProperty("_EmissionColor"))
            {
                _hasEmissionProperty = true;
                _mat.EnableKeyword("_EMISSION");
                _defaultEmission = _mat.GetColor("_EmissionColor");
            }
        }
    }

    public void ToggleHighlight(bool isOn)
    {
        if (!_hasEmissionProperty || _mat == null) return;

        if (isOn)
        {
            _mat.SetColor("_EmissionColor", highlightColor);
        }
        else
        {
            _mat.SetColor("_EmissionColor", _defaultEmission);
        }
    }
}