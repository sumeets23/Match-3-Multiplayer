using UnityEngine;

/// <summary>
/// Centralized audio manager for playing UI/button sounds.
/// Attach this to a GameObject in your initial scene and assign AudioClips in the inspector.
/// </summary>
public class SoundsManager : MonoBehaviour
{
    public static SoundsManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("AudioSource dedicated for UI sounds (button clicks, hovers, etc.)")]
    [SerializeField] private AudioSource uiAudioSource;

    [Header("UI Clips")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] private AudioClip positiveClip;
    [SerializeField] private AudioClip negativeClip;

    [Header("Settings")] 
    [Range(0f, 1f)]
    [SerializeField] private float uiVolume = 1f;
    [SerializeField] private bool dontDestroyOnLoad = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        // Ensure we have an AudioSource for UI if not assigned
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Plays the configured button click sound.
    /// </summary>
    public void PlayButtonClick()
    {
        PlayUi(buttonClickClip);
    }

    /// <summary>
    /// Plays the configured button hover sound.
    /// </summary>
    public void PlayButtonHover()
    {
        PlayUi(buttonHoverClip);
    }

    /// <summary>
    /// Plays a generic positive feedback sound (e.g., success).
    /// </summary>
    public void PlayPositive()
    {
        PlayUi(positiveClip);
    }

    /// <summary>
    /// Plays a generic negative feedback sound (e.g., error).
    /// </summary>
    public void PlayNegative()
    {
        PlayUi(negativeClip);
    }

    /// <summary>
    /// Generic UI play helper.
    /// </summary>
    private void PlayUi(AudioClip clip)
    {
        if (clip == null || uiAudioSource == null) return;
        uiAudioSource.volume = uiVolume;
        uiAudioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Allows external systems (e.g., audio settings menu) to update UI volume at runtime.
    /// </summary>
    public void SetUiVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Utility that can be wired to Unity UI Button onClick without code:
    /// Select the SoundsManager in the scene -> OnClick -> SoundsManager.PlayButtonClick.
    /// </summary>
    [ContextMenu("Test Button Click")] 
    private void TestClick()
    {
        PlayButtonClick();
    }
}
