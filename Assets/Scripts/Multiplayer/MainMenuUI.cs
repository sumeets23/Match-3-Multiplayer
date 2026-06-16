using Fusion;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button joinButton; // assign manually in inspector


    [Header("Audio References")]
    public AudioSource musicSource;    // Your music AudioSource
    public AudioClip buttonSound;
    public AudioSource sfxAudioSource;    // Your music AudioSource
    public AudioSettingsSO settingsSO; // ScriptableObject to store volume
    public Slider volumeSlider;




    private void Start()
    {
        if (playerNameInput != null && !string.IsNullOrEmpty(NetworkManagerFusion.Instance?.LocalPlayerName))
            playerNameInput.text = NetworkManagerFusion.Instance.LocalPlayerName;

        if (errorMessageText != null)
            errorMessageText.gameObject.SetActive(false);

        if (settingsSO != null)
            SetVolume(settingsSO.musicVolume);

        // Sync slider value
        if (volumeSlider != null)
        {
            volumeSlider.value = settingsSO.musicVolume;
            volumeSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }


    private void OnDisable()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    public void OnSliderChanged(float value)
    {
        SetVolume(value);
        settingsSO.musicVolume = value; // Save to ScriptableObject

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(settingsSO); // So value persists in Editor
#endif
    }

    private void SetVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = value; // Directly set volume 0–1
    }


    // ---------------- CREATE ----------------
    public async void OnCreateButton()
    {
        if (NetworkManagerFusion.Instance == null)
        {
            ShowErrorMessage("Please wait for network initialization");
            return;
        }

        string nameToUse = string.IsNullOrWhiteSpace(playerNameInput.text)
            ? "Player" + UnityEngine.Random.Range(1000, 9999)
            : playerNameInput.text;

        NetworkManagerFusion.Instance.SetLocalPlayerName(nameToUse);

        string roomCode = GenerateRoomCode();
        bool success = await NetworkManagerFusion.Instance.CreateRoomAsync(roomCode);

        if (success)
            SceneController.Instance.LoadLobby();
        else
            ShowErrorMessage("Failed to create room");
    }

    // ---------------- JOIN ----------------
    public async void OnJoinButton()
    {
        if (joinButton != null) joinButton.interactable = false;

        string trimmedCode = roomCodeInput.text?.Trim().ToUpper() ?? "";

        if (string.IsNullOrEmpty(trimmedCode))
        {
            ShowErrorMessage("Please enter a room code.");
            joinButton.interactable = true;
            return;
        }

        try
        {
            string playerName = string.IsNullOrWhiteSpace(playerNameInput?.text)
                ? "Player" + UnityEngine.Random.Range(1000, 9999)
                : playerNameInput.text;

            NetworkManagerFusion.Instance.SetLocalPlayerName(playerName);

            bool success = await NetworkManagerFusion.Instance.JoinRoomAsync(trimmedCode);

            if (success)
            {
                SceneController.Instance.LoadLobby();
            }
            else
            {
                ShowErrorMessage("❌ Room not found. Please check the code.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Join failed: {ex.Message}");
            ShowErrorMessage("❌ Connection error. Try again.");
            await NetworkManagerFusion.Instance.ShutdownRunnerAsync(true);
        }
        finally
        {
            if (joinButton != null) joinButton.interactable = true;
        }
    }

    public void LeaveRoomAndReturnToMainMenu()
    {
        // On Android/IL2CPP, async/await continuations can be unreliable if objects get disabled/destroyed
        // during scene changes. Do a best-effort shutdown, but don't block menu navigation.
        StartCoroutine(LeaveThenLoadMainMenuRoutine());
    }

    private IEnumerator LeaveThenLoadMainMenuRoutine()
    {
        Debug.Log("Leaving current Fusion room (best-effort) and returning to Main Menu...");

        if (joinButton != null)
            joinButton.interactable = false;

        // Start shutdown task (if possible)
        Task shutdownTask = null;
        if (NetworkManagerFusion.Instance != null)
            shutdownTask = NetworkManagerFusion.Instance.ShutdownRunnerAsync(true);

        // Load menu immediately (keeps UI responsive on mobile)
        if (SceneController.Instance != null)
            SceneController.Instance.LoadMainMenu();
        else
            SceneManager.LoadScene("Main Menu");

        // Finish shutdown in background
        if (shutdownTask != null)
        {
            while (!shutdownTask.IsCompleted)
                yield return null;

            if (shutdownTask.IsFaulted)
                Debug.LogWarning($"[MainMenuUI] Shutdown task faulted: {shutdownTask.Exception}");
        }

        if (joinButton != null)
            joinButton.interactable = true;
    }


    // ---------------- HELPERS ----------------
    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789";
        char[] code = new char[5];
        for (int i = 0; i < code.Length; i++)
            code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        return new string(code);
    }

    private void ShowErrorMessage(string message)
    {
        if (errorMessageText == null) return;

        StopAllCoroutines(); // reset hide timer
        errorMessageText.text = message;
        errorMessageText.gameObject.SetActive(true);
        StartCoroutine(HideErrorAfterDelay());
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(2.5f);
        if (errorMessageText != null)
            errorMessageText.gameObject.SetActive(false);
    }

    public void PlayButtonSound()
    {
        if (sfxAudioSource != null)
        {

            sfxAudioSource.clip = buttonSound;

            sfxAudioSource.Play();
        }

    }

    public void ExitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }
}
