using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class UIButtonAudioManager : MonoBehaviour
{
    public static UIButtonAudioManager Instance
    {
        get;
        private set;
    }

    [Header("Button Click Audio")]
    [SerializeField] private AudioClip clickClip;

    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 0.55f;

    [SerializeField]
    private Vector2 pitchRange =
        new Vector2(0.98f, 1.02f);

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RegisterAllButtons();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        RegisterAllButtons();
    }

    private void RegisterAllButtons()
    {
        Button[] buttons =
            FindObjectsOfType<Button>(true);

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            // Tránh đăng ký trùng khi load hoặc quét lại scene.
            button.onClick.RemoveListener(
                PlayClick
            );

            button.onClick.AddListener(
                PlayClick
            );
        }
    }

    public void PlayClick()
    {
        if (_audioSource == null ||
            clickClip == null)
        {
            return;
        }

        _audioSource.pitch = Random.Range(
            pitchRange.x,
            pitchRange.y
        );

        _audioSource.PlayOneShot(
            clickClip,
            clickVolume
        );
    }
}