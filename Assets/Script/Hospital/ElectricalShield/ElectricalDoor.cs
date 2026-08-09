using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ElectricalDoor : MonoBehaviour, IQuestRequirement
{
    [Header("Door Settings")]
    [SerializeField] private Transform hingeTransform;

    [Tooltip(
        "The axis around which the door rotates. " +
        "Change this to X, Y, or Z depending on the model."
    )]
    [SerializeField]
    private Vector3 rotationAxis =
        Vector3.forward;

    [SerializeField] private float openAngle = -150f;

    [Min(0.01f)]
    [SerializeField] private float openSpeed = 2f;

    [Header("Door State")]
    [SerializeField] private bool _isOpen = false;

    [Header("Objects Inside Cabinet")]
    [Tooltip(
        "Assign all colliders belonging to fuses, switches, " +
        "or interactable objects inside the cabinet."
    )]
    [SerializeField] private Collider[] insideColliders;

    [Header("Door Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    [Range(0f, 1f)]
    [SerializeField] private float openVolume = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float closeVolume = 0.7f;

    [SerializeField]
    private Vector2 pitchRange =
        new Vector2(0.97f, 1.03f);

    private Coroutine currentAnimation;
    private bool _isAnimating;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (hingeTransform == null)
        {
            hingeTransform = transform;
        }

        float startAngle =
            _isOpen ? openAngle : 0f;

        hingeTransform.localRotation =
            Quaternion.Euler(
                rotationAxis.normalized * startAngle
            );

        ToggleInsideColliders(_isOpen);
    }

    // This door does not require an inventory item.
    public ItemDataSO GetRequiredItem()
    {
        return null;
    }

    public bool IsCompleted()
    {
        return _isOpen;
    }

    public string GetPrompt()
    {
        if (_isAnimating)
        {
            return null;
        }

        return _isOpen
            ? "[F] Close electrical cabinet"
            : "[F] Open electrical cabinet";
    }

    public bool TryUseItem(InventorySystem inventory)
    {
        if (_isAnimating)
        {
            return false;
        }

        _isOpen = !_isOpen;

        ToggleInsideColliders(_isOpen);

        if (_isOpen)
        {
            PlayDoorSound(openClip, openVolume);
        }
        else
        {
            PlayDoorSound(closeClip, closeVolume);
        }

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        float targetAngle =
            _isOpen ? openAngle : 0f;

        currentAnimation =
            StartCoroutine(AnimateDoor(targetAngle));

        Debug.Log(
            _isOpen
                ? "[ElectricalDoor] Cabinet opened."
                : "[ElectricalDoor] Cabinet closed."
        );

        return true;
    }

    private void ToggleInsideColliders(bool state)
    {
        if (insideColliders == null)
        {
            return;
        }

        foreach (Collider col in insideColliders)
        {
            if (col != null)
            {
                col.enabled = state;
            }
        }
    }

    private IEnumerator AnimateDoor(float targetAngle)
    {
        _isAnimating = true;

        Quaternion startRotation =
            hingeTransform.localRotation;

        Quaternion targetRotation =
            Quaternion.Euler(
                rotationAxis.normalized * targetAngle
            );

        float progress = 0f;
        float safeSpeed = Mathf.Max(0.01f, openSpeed);

        while (progress < 1f)
        {
            progress += Time.deltaTime * safeSpeed;

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(progress)
                );

            hingeTransform.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    smoothProgress
                );

            yield return null;
        }

        hingeTransform.localRotation =
            targetRotation;

        _isAnimating = false;
        currentAnimation = null;
    }

    private void PlayDoorSound(
        AudioClip clip,
        float volume)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.pitch = Random.Range(
            pitchRange.x,
            pitchRange.y
        );

        audioSource.PlayOneShot(
            clip,
            volume
        );
    }
}