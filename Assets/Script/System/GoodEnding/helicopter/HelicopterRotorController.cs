using UnityEngine;

public class HelicopterRotorController : MonoBehaviour
{
    [Header("Rotors")]
    [SerializeField] private Transform topRotor;
    [SerializeField] private Transform backRotor;

    [Header("Rotor Speed")]
    [SerializeField] private float topRotorSpeed = 1600f;
    [SerializeField] private float backRotorSpeed = 2200f;

    [Header("Audio")]
    [SerializeField] private AudioSource helicopterAudio;

    private bool rotorRunning = false;

    private void Update()
    {
        if (!rotorRunning)
            return;

        if (topRotor != null)
        {
            topRotor.Rotate(
                Vector3.up,
                topRotorSpeed * Time.deltaTime,
                Space.Self
            );
        }

        if (backRotor != null)
        {
            backRotor.Rotate(
                Vector3.right,
                backRotorSpeed * Time.deltaTime,
                Space.Self
            );
        }
    }

    public void StartRotor()
    {
        rotorRunning = true;

        if (helicopterAudio != null && !helicopterAudio.isPlaying)
        {
            helicopterAudio.Play();
        }
    }

    public void StopRotor()
    {
        rotorRunning = false;

        if (helicopterAudio != null)
        {
            helicopterAudio.Stop();
        }
    }
}