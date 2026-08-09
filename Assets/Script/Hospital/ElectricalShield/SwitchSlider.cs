using System.Collections;
using UnityEngine;

public class SwitchSlider : MonoBehaviour
{
    [Header("Switch Settings")]
    [SerializeField] private bool isOn = false;

    [Tooltip(
        "Local position offset applied when the switch is ON.\n" +
        "Example: (0, 0, 0.03) moves the switch 3 cm " +
        "along the local Z axis."
    )]
    [SerializeField]
    private Vector3 onOffset =
        new Vector3(0f, 0f, 0.03f);

    [Min(0.01f)]
    [SerializeField] private float slideDuration = 0.15f;

    [Header("References")]
    [SerializeField] private PanelInteractZone panelZone;
    [SerializeField] private FusePanelManager fusePanelManager;

    [Header("Switch Audio")]
    [Tooltip(
        "A shared Audio Source can be used by all switches " +
        "inside the electrical cabinet."
    )]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip switchOnClip;
    [SerializeField] private AudioClip switchOffClip;

    [Range(0f, 1f)]
    [SerializeField] private float switchVolume = 0.65f;

    [SerializeField]
    private Vector2 pitchRange =
        new Vector2(0.97f, 1.03f);

    private Vector3 _originPosition;
    private bool _isAnimating;

    public bool IsOn => isOn;

    private void Start()
    {
        _originPosition = transform.localPosition;

        transform.localPosition = isOn
            ? _originPosition + onOffset
            : _originPosition;
    }

    private void OnMouseDown()
    {
        if (panelZone == null ||
            !panelZone.IsInPanelMode)
        {
            return;
        }

        Toggle();
    }

    public void Toggle()
    {
        if (_isAnimating)
        {
            return;
        }

        isOn = !isOn;

        Vector3 targetPosition = isOn
            ? _originPosition + onOffset
            : _originPosition;

        PlaySwitchSound();

        StartCoroutine(
            AnimateSlide(targetPosition)
        );

        fusePanelManager?.UpdatePanelState();

        Debug.Log(
            $"[Switch {gameObject.name}] " +
            $"{(isOn ? "ON" : "OFF")}"
        );
    }

    private IEnumerator AnimateSlide(
        Vector3 targetPosition)
    {
        _isAnimating = true;

        Vector3 startPosition =
            transform.localPosition;

        float elapsed = 0f;
        float safeDuration =
            Mathf.Max(0.01f, slideDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / safeDuration
            );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            transform.localPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smoothProgress
                );

            yield return null;
        }

        transform.localPosition =
            targetPosition;

        _isAnimating = false;
    }

    private void PlaySwitchSound()
    {
        if (audioSource == null)
        {
            return;
        }

        AudioClip selectedClip =
            isOn ? switchOnClip : switchOffClip;

        if (selectedClip == null)
        {
            return;
        }

        audioSource.pitch = Random.Range(
            pitchRange.x,
            pitchRange.y
        );

        audioSource.PlayOneShot(
            selectedClip,
            switchVolume
        );
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin =
            transform.localPosition;

        Vector3 previewPosition =
            origin + onOffset;

        Gizmos.color = Color.green;

        Gizmos.DrawSphere(
            transform.parent != null
                ? transform.parent.TransformPoint(
                    previewPosition
                )
                : transform.position + onOffset,
            0.005f
        );
    }
}