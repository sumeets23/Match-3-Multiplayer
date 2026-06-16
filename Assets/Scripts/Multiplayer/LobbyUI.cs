using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private Button startGameButton; // Host-only button



    [Header("Match settings (host only)")]
    [SerializeField] private TMP_InputField matchDurationInput;

    [Header("Timer UI")]
    [Tooltip("Timer GameObject will be enabled only for host")]
    [SerializeField] private GameObject timerObject;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        var room = NetworkManagerFusion.Instance?.CurrentRoomCode ?? "No Room";
        SetRoomName(room);

        var runner = NetworkManagerFusion.Instance?.GetRunner();
        bool isHost = runner != null && runner.IsServer;

        // Host-only UI elements
        if (startGameButton != null)
            startGameButton.gameObject.SetActive(isHost);

        if (matchDurationInput != null)
            matchDurationInput.interactable = isHost;

        if (timerObject != null)
            timerObject.SetActive(isHost); // ✅ Host sees timer, clients don't

        // Show current players
        RefreshPlayerList();

     

        // Load match duration setting (only visible to host)
        if (isHost && matchDurationInput != null && NetworkManagerFusion.Instance != null)
        {
            matchDurationInput.text = NetworkManagerFusion.Instance.GetMatchDuration().ToString();
        }
    }

    // Called by PlayerNetwork spawn/despawn to refresh the current list
    public void RefreshPlayerList()
    {
        if (playerListParent == null || playerListItemPrefab == null) return;

        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        var players = FindObjectsOfType<PlayerNetwork>();
        List<string> names = new List<string>();

        foreach (var pn in players)
        {
            string name;
            if (pn.Object != null && pn.Object.IsValid)
            {
                name = pn.DisplayName.ToString();
            }
            else
            {
                name = "(Connecting...)";
                Debug.LogWarning($"[LobbyUI] Found PlayerNetwork not yet spawned - using placeholder name.");
            }

            if (string.IsNullOrEmpty(name)) name = "Player";
            names.Add(name);
        }

        names = names.OrderBy(n => n).ToList();

        foreach (var n in names)
        {
            var item = Instantiate(playerListItemPrefab, playerListParent);
            var text = item.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = n;
        }
    }

    public void SetRoomName(string roomName)
    {
        if (roomNameText != null)
            roomNameText.text = $"Room: {roomName}";
    }

    // Called from inspector button — only host should see this button.
    public void OnStartGameClicked()
    {
        var runner = NetworkManagerFusion.Instance?.GetRunner();
        if (runner == null)
        {
            Debug.LogWarning("No runner available when starting game.");
            return;
        }

        if (!runner.IsServer)
        {
            Debug.LogWarning("Only the Host/Server can start the game.");
            return;
        }

        Debug.Log("Host requested to start game: loading game scene for all players...");

        var nm = NetworkManagerFusion.Instance;
        if (nm != null && matchDurationInput != null)
        {
            if (float.TryParse(matchDurationInput.text, out var parsed) && parsed > 0)
            {
                nm.SetMatchDuration(parsed);
            }
            else
            {
                Debug.LogWarning("Invalid match duration entered in lobby - using default.");
            }
        }

        var targetScene = SceneRef.FromIndex(SceneController.Instance.mainGameScene.BuildIndex);
        runner.LoadScene(targetScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
