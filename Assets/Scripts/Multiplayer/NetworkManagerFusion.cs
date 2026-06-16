// NetworkManagerFusion.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;

public class NetworkManagerFusion : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManagerFusion Instance { get; private set; }

    [Header("Prefabs / Settings")]
    [Tooltip("Assign the PlayerNetwork prefab (contains a NetworkObject + PlayerNetwork script)")]
    public NetworkObject playerPrefab;

    [Header("Match settings")]
    [Tooltip("Duration of a match in seconds (host only). This value is used to initialize the GameTimer on match start.")]
    [SerializeField] private float matchDuration = 60f;

   
    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _sceneManager;


    public string CurrentRoomCode { get; private set; } = "";
    public string LocalPlayerName { get; private set; } = "";

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    public void SetLocalPlayerName(string name)
    {
        LocalPlayerName = (name ?? "").Trim();
        if (LocalPlayerName.Length > 32) LocalPlayerName = LocalPlayerName.Substring(0, 32);
    }


    public void SetMatchDuration(float seconds)
    {
        // clamp to reasonable minimum
        matchDuration = Mathf.Max(1f, seconds);
        Debug.Log($"[NetworkManagerFusion] matchDuration set to {matchDuration}s by UI");
    }

    public float GetMatchDuration() => matchDuration;


    public NetworkRunner GetRunner() => _runner;


    public async Task<bool> CreateRoomAsync(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(LocalPlayerName))
        {
            Debug.LogError("LocalPlayerName must be set before CreateRoomAsync()");
            return false;
        }

        // kill any old runner
        if (_runner != null)
        {
            try { await _runner.Shutdown(); } catch { }
            _runner = null;
        }

        CurrentRoomCode = roomCode;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        //  persist scene manager
        _sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        //   make sure the Lobby scene index exists
        int lobbyIndex = 1;
        if (lobbyIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("Lobby scene index not in Build Settings!");
            return false;
        }

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomCode,
            Scene = SceneRef.FromIndex(lobbyIndex),
            SceneManager = _sceneManager
        };

        var result = await _runner.StartGame(startArgs);

        if (!result.Ok)
        {
            Debug.LogError($"CreateRoomAsync failed: {result.ShutdownReason}");
            return false;
        }

        Debug.Log($" Created and running room: {roomCode}");

     
        if (_runner.IsServer)
        {
            if (!_spawnedPlayers.ContainsKey(_runner.LocalPlayer))
            {
                if (playerPrefab == null)
                {
                    // Try to load a prefab fallback from Resources (helpful if inspector not assigned)
                    var fallback = Resources.Load<NetworkObject>("PlayerPrefab");
                    if (fallback != null)
                    {
                        playerPrefab = fallback;
                        Debug.LogWarning("[NetworkManagerFusion] playerPrefab was null - loaded fallback from Resources/PlayerPrefab");
                    }
                }

                if (playerPrefab != null)
                {
                    var no = _runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, _runner.LocalPlayer);
                    if (no != null) _spawnedPlayers[_runner.LocalPlayer] = no;
                    Debug.Log($"[NetworkManagerFusion] Spawned host player object for LocalPlayer: {no}");
                }
                else
                {
                    Debug.LogWarning("[NetworkManagerFusion] playerPrefab is not assigned - host player was not spawned. Assign a PlayerNetwork prefab in the inspector or add a Resources/PlayerPrefab.");
                }
            }
        }
        return true;
    }



    // Join an existing session by roomCode (client)
    public async Task<bool> JoinRoomAsync(string roomCode)
    {
        if (_runner == null)
        {
            Debug.Log("Creating new Fusion runner...");
            var go = new GameObject("NetworkRunner");
            _runner = go.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }

        Debug.Log($"🔹 Attempting to join room: {roomCode}");
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = roomCode,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log($" Joined room: {roomCode}");
            return true;
        }
        else
        {
            Debug.LogWarning($" Join failed: {result.ShutdownReason}");

            if (result.ShutdownReason == ShutdownReason.GameNotFound)
            {
                Debug.Log("Room not found on server.");
            }

            // Cleanup runner so next attempt works cleanly
            await ShutdownRunnerAsync(true);
            return false;
        }
    }


    // Leave the current session (shutdown runner)
    public void LeaveSession()
    {
        if (_runner != null)
        {
            try
            {
                _runner.Shutdown(true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LeaveSession shutdown exception: {e.Message}");
            }
            _runner = null;
            CurrentRoomCode = "";
        }

        // clear host-side dictionary
        _spawnedPlayers.Clear();

        // destroy scene manager if present
        if (_sceneManager != null)
        {
            try { Destroy(_sceneManager); } catch { }
            _sceneManager = null;
        }
    }
    public async Task ShutdownRunnerAsync(bool destroyRunner = false)
    {
        // Guard against shutdown being called while/after runner is already being destroyed.
        var runner = _runner;
        if (runner == null)
            return;

        try
        {
            await runner.Shutdown();
        }
        catch (Exception e)
        {
            // Fusion can throw during disconnect sequences; we still want to continue cleanup.
            Debug.LogWarning($"[NetworkManagerFusion] Runner shutdown exception: {e}");
        }

        if (destroyRunner)
        {
            try
            {
                if (runner != null)
                    Destroy(runner.gameObject);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetworkManagerFusion] Destroy runner exception: {e}");
            }
        }

        // Cleanup related state
        _runner = null;
        CurrentRoomCode = "";
        _spawnedPlayers.Clear();

        if (_sceneManager != null)
        {
            try { Destroy(_sceneManager); } catch { }
            _sceneManager = null;
        }
    }


    // -------------------------------------------------------------------------
    // INetworkRunnerCallbacks implementation (all required callbacks implemented)
    // Most are not used for this simple lobby example; bodies are empty or log basic info.
    // -------------------------------------------------------------------------
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined: {player}");

        if (runner.IsServer)
        {
           
            if (_spawnedPlayers.ContainsKey(player))
            {
                Debug.Log($"[NetworkManagerFusion] OnPlayerJoined: player {player} already has a spawned object - skipping duplicate spawn.");
                LobbyUI.Instance?.RefreshPlayerList();
                return;
            }
         
            if (playerPrefab == null)
            {
                Debug.LogWarning("PlayerPrefab not set in NetworkManagerFusion inspector. Trying Resources/PlayerPrefab fallback.");
                var fallback = Resources.Load<NetworkObject>("PlayerPrefab");
                if (fallback != null)
                {
                    playerPrefab = fallback;
                    Debug.LogWarning("Loaded PlayerPrefab from Resources/PlayerPrefab as fallback.");
                }
                else
                {
                    Debug.LogError("PlayerPrefab not set and no Resources/PlayerPrefab found. Cannot spawn player object.");
                    return;
                }
            }

            var spawnPos = Vector3.zero;
            var spawnRot = Quaternion.identity;

           
            var no = runner.Spawn(playerPrefab, spawnPos, spawnRot, player);

            if (no != null)
            {
                _spawnedPlayers[player] = no;
                Debug.Log($"Spawned PlayerNetwork for player {player} (host).");
            }
            else
            {
                Debug.LogError("Failed to spawn PlayerNetwork prefab for player " + player);
            }
        }

     
        LobbyUI.Instance?.RefreshPlayerList();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerLeft: {player}");

       
        if (runner.IsServer)
        {
            if (_spawnedPlayers.TryGetValue(player, out var no))
            {
                runner.Despawn(no);
                _spawnedPlayers.Remove(player);
            }
        }

        LobbyUI.Instance?.RefreshPlayerList();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner shutdown: {shutdownReason}");
        // cleanup
        _spawnedPlayers.Clear();
        CurrentRoomCode = "";
        _runner = null;
    }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Debug.LogWarning($"ConnectFailed: {reason}"); }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { Debug.Log($"Disconnected: {reason}"); }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
        LobbyUI.Instance?.RefreshPlayerList();
        
   
        if (runner.IsServer)
        {
            
            try
            {
                foreach (var playerRef in runner.ActivePlayers)
                {
                    var playerObj = runner.GetPlayerObject(playerRef);
                    if (playerObj == null)
                    {
                        // Try inspector prefab then Resources fallback
                        if (playerPrefab == null)
                        {
                            var fallback = Resources.Load<NetworkObject>("PlayerPrefab");
                            if (fallback != null)
                            {
                                playerPrefab = fallback;
                                Debug.LogWarning("[NetworkManagerFusion] playerPrefab was null - loaded fallback from Resources/PlayerPrefab during scene load");
                            }
                        }

                        if (playerPrefab != null)
                        {
                            var spawnPos = Vector3.zero;
                            var spawnRot = Quaternion.identity;
                            var no = runner.Spawn(playerPrefab, spawnPos, spawnRot, playerRef);
                            if (no != null)
                            {
                                _spawnedPlayers[playerRef] = no;
                                Debug.Log($"[NetworkManagerFusion] Spawned PlayerNetwork for player {playerRef} after scene load. Obj: {no.name}");
                            }
                            else
                            {
                                Debug.LogError($"[NetworkManagerFusion] Failed to spawn PlayerNetwork for player {playerRef} after scene load.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[NetworkManagerFusion] playerPrefab is not assigned and no Resources/PlayerPrefab found - cannot spawn player objects after scene load.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetworkManagerFusion] Exception while spawning players after scene load: {e.Message}");
            }

            var timer = FindObjectOfType<GameTimerManager>();
            if (timer != null)
            {
                Debug.Log($"[NetworkManagerFusion] Initializing match timer: {matchDuration}s (server)");
                timer.InitializeTimer(matchDuration);
            }
        }
    }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    
    public static string GenerateRoomCode(int length = 5)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; 
        var arr = new char[length];
        var rng = new System.Random();
        for (int i = 0; i < length; i++) arr[i] = chars[rng.Next(chars.Length)];
        return new string(arr);
    }
}
