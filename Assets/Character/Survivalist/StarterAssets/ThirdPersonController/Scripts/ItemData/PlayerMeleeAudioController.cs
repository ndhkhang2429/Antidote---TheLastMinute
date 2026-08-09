using UnityEngine;

public class PlayerMeleeAudioController : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    public void PlaySwing(WeaponDataSO data)
    {
        if (data == null)
            return;

        PlayRandom(
            data.meleeSwingClips,
            data.meleeSwingVolume,
            data.meleePitchRange
        );
    }

    public void PlayHitFlesh(WeaponDataSO data)
    {
        if (data == null)
            return;

        PlayRandom(
            data.meleeHitFleshClips,
            data.meleeHitFleshVolume,
            data.meleePitchRange
        );
    }

    private void PlayRandom(
        AudioClip[] clips,
        float volume,
        Vector2 pitchRange)
    {
        if (audioSource == null ||
            clips == null ||
            clips.Length == 0)
        {
            return;
        }

        AudioClip clip =
            clips[Random.Range(0, clips.Length)];

        audioSource.pitch = Random.Range(
            pitchRange.x,
            pitchRange.y
        );

        audioSource.PlayOneShot(clip, volume);
    }
}