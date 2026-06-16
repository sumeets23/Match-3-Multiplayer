using UnityEngine;
using UnityEngine.SceneManagement;

using Udar.SceneManager; // Update this if your SceneField type is in a different namespace

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Scene References (SceneField)")]
    [SerializeField] private SceneField mainMenuScene;
    [SerializeField] private SceneField lobby;
    public SceneField mainGameScene;

    [Header("Settings")] 
    [SerializeField] private bool dontDestroyOnLoad = true;

    private void Awake()
    {
        // Ensure singleton survives scene loads.
        // NOTE: DontDestroyOnLoad only works on ROOT GameObjects.
        // If this component is on a child object, Unity will not persist it correctly.
        if (transform.parent != null)
        {
            Debug.LogWarning($"[SceneController] '{name}' is not a root GameObject. Moving to root for DontDestroyOnLoad to work.");
            transform.SetParent(null);
        }

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SceneController] Duplicate detected. Destroying '{name}'. Keeping '{Instance.name}'.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[SceneController] Marked DontDestroyOnLoad: '{name}'");
        }
        else
        {
            Debug.Log($"[SceneController] dontDestroyOnLoad is false for '{name}'");
        }
    }

    public void LoadMainMenu()
    {
        if (mainMenuScene == null || string.IsNullOrEmpty(mainMenuScene.Name))
        {
            Debug.LogError("SceneManagere: Main Menu SceneField is not assigned.");
            return;
        }

        if (!IsSceneInBuild(mainMenuScene.Name))
        {
            Debug.LogError($"SceneManagere: Scene '{mainMenuScene.Name}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(mainMenuScene.Name);
    }

    public void LoadLobby()
    {
        if (lobby == null || string.IsNullOrEmpty(lobby.Name))
        {
            Debug.LogError("SceneManagere: Lobby SceneField is not assigned.");
            return;
        }

        if (!IsSceneInBuild(lobby.Name))
        {
            Debug.LogError($"SceneManagere: Scene '{lobby.Name}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(lobby.Name);
    }

    public void LoadMainGame()
    {
        if (mainGameScene == null || string.IsNullOrEmpty(mainGameScene.Name))
        {
            Debug.LogError("SceneManagere: Main Game SceneField is not assigned.");
            return;
        }

        if (!IsSceneInBuild(mainGameScene.Name))
        {
            Debug.LogError($"SceneManagere: Scene '{mainGameScene.Name}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(mainGameScene.Name);
    }

    private bool IsSceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }
}
