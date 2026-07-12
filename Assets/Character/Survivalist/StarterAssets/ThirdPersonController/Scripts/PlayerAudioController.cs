using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioSource breathSource;
    [SerializeField] private PlayerStamina playerStamina; // kéo Player object vào

    [Header("Footstep - Surface Clips")]
    [SerializeField] private AudioClip[] footstepConcrete;
    [SerializeField] private AudioClip[] footstepMetal;
    [SerializeField] private AudioClip[] footstepGlass;

    [Header("Footstep Settings")]
    [SerializeField] private float walkVolume = 0.5f;
    [SerializeField] private float runVolume = 0.8f;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.05f;

    [Header("Footstep Timing (FPS - không dùng Animation Event)")]
    [SerializeField] private float walkStepInterval = 0.5f; // thời gian giữa 2 bước khi đi bộ
    [SerializeField] private float runStepInterval = 0.32f;  // thời gian giữa 2 bước khi chạy

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float raycastDistance = 0.5f;

    [Header("Breathing Clips")]
    [SerializeField] private AudioClip breathRun;
    [SerializeField] private AudioClip breathExhausted;

    private bool isMoving = false;
    private bool isRunning = false;
    private float stepTimer = 0f;

    void Update()
    {
        HandleFootstepTimer();
        UpdateBreathing();
    }

    // ================= FOOTSTEP =================

    // Gọi từ FirstPersonController mỗi frame trong Move(), thay cho Animation Event
    public void SetMovementState(bool moving, bool running)
    {
        isMoving = moving;
        isRunning = running;
    }

    private void HandleFootstepTimer()
    {
        if (!isMoving)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = isRunning ? runStepInterval : walkStepInterval;
        }
    }

    private void PlayFootstep()
    {
        AudioClip[] clipSet = GetClipSetBySurface();
        if (clipSet == null || clipSet.Length == 0 || footstepSource == null) return;

        AudioClip clip = clipSet[Random.Range(0, clipSet.Length)];
        footstepSource.pitch = Random.Range(pitchMin, pitchMax);
        footstepSource.PlayOneShot(clip, isRunning ? runVolume : walkVolume);
    }

    private AudioClip[] GetClipSetBySurface()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down,
            out RaycastHit hit, raycastDistance, groundMask))
        {
            if (hit.collider.CompareTag("Surface_Metal")) return footstepMetal;
            if (hit.collider.CompareTag("Surface_Glass")) return footstepGlass;
        }
        return footstepConcrete;
    }

    // ================= BREATHING =================

    private void UpdateBreathing()
    {
        if (playerStamina == null || breathSource == null) return;

        AudioClip target = null;

        if (playerStamina.isExhausted)
            target = breathExhausted;
        else if (isRunning)
            target = breathRun;
        // else: đứng yên/đi bộ bình thường -> không có tiếng thở (im lặng, đúng chất horror)

        if (target == null)
        {
            if (breathSource.isPlaying) breathSource.Stop();
            return;
        }

        if (breathSource.clip == target && breathSource.isPlaying) return;

        breathSource.clip = target;
        breathSource.loop = true;
        breathSource.Play();
    }
}